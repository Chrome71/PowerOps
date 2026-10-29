using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

using PowerOps.Core.Drift;
using PowerOps.Core.Output;

namespace PowerOps.Core.Deploy;

/// <summary>The import that a hook call would run: the zip and settings paths and the environment exactly as the call wrote them, and whether it stages and upgrades.</summary>
public sealed partial record ImportCall(string Zip, string Environment, string? Settings, bool StageAndUpgrade)
{
    /// <summary>The largest hook input the gate reads, in bytes.</summary>
    public const int MaxInputBytes = 1_048_576;

    private const int MaxName = 100;
    private const string Gap = @"(?:[\s'""]|\\\r?\n)*";

    private const string RouteModelApps = "model-apps' solution-settings.js import builds its own import request, so its options can't be checked";
    private const string RoutePowerPages = "power-pages' import-solution sends the zip in a request body, so its zip and environment can't be checked";
    private const string RoutePipeline = "pac pipeline deploy has no zip, and its target is a pipeline stage";
    private const string RouteTemplate = "create-site's template import imports a template solution that no plan holds, and overwrites unmanaged customizations";
    private const string RoutePackage = "pac package deploy imports a package that no plan holds";
    private const string RouteUpgrade = "pac solution upgrade applies a staged upgrade that no plan holds";
    private const string RoutePcf = "pac pcf push imports a solution that no plan holds";
    private const string RouteApplication = "pac application install installs an application that no plan holds";
    private const string RouteCode = "pac code push pushes an app that no plan holds";
    private const string RouteCatalog = "pac catalog install installs a catalog item that no plan holds";
    private const string RouteAppPackage = "pac app-management install-application-package installs an application package that no plan holds";

    private const string CantRead = "the command holds an import that the gate can't read";
    private const string Stray = "the command has a word the gate doesn't read";
    private const string PathHint = "must be a literal absolute path (no $, backtick, escape, ~ or wildcard)";
    private const string AnsiC = "the command has a $'...' word with an escape, which the gate can't read";

    private static readonly string[] Routes = [RouteModelApps, RoutePowerPages, RoutePipeline, RouteTemplate, RoutePackage, RouteUpgrade, RoutePcf, RouteApplication, RouteCode, RouteCatalog, RouteAppPackage];

    private static readonly Dictionary<string, string> Canonical = new(StringComparer.Ordinal)
    {
        ["--path"] = "--path",
        ["-p"] = "--path",
        ["--environment"] = "--environment",
        ["-env"] = "--environment",
        ["--settings-file"] = "--settings-file",
        ["--stage-and-upgrade"] = "--stage-and-upgrade",
        ["-up"] = "--stage-and-upgrade",
        ["--async"] = "--async",
        ["-a"] = "--async",
        ["--max-async-wait-time"] = "--max-async-wait-time",
        ["-wt"] = "--max-async-wait-time",
    };

    private static readonly string[] Switches = ["--stage-and-upgrade", "--async"];
    private static readonly string[] McpKeys = ["--path", "--environment", "--settings-file", "--stage-and-upgrade", "--async", "--max-async-wait-time"];

    private sealed record Word(string Value, string Raw, bool Literal, bool Redirect);

    private sealed record Segment(Word[] Words, int Start, int End);

    private sealed record Given(string Value, bool Literal);

    [GeneratedRegex($"solution{Gap}import|solution{Gap}upgrade|pipeline{Gap}deploy|package{Gap}deploy|solution-settings\\.js|import-template-solution|encode-solution-file|ImportSolution|StageAndUpgrade|DeployPackageAsync|RetryFailedDeploymentAsync|(?<!\\w)(?:pcf{Gap}push|application{Gap}install|code{Gap}push|catalog{Gap}install)|install-application-package|Import-CrmSolution", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Markers();

    // A $'...' word holding a backslash: its escapes (\x70, \160, \u0070) can spell any text.
    [GeneratedRegex(@"\$'[^']*\\")]
    private static partial Regex AnsiCEscape();

    [GeneratedRegex(@"(?<!\w)pac(?:\.exe)?(?!\w)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PacWord();

    // On Windows two slashes or backslashes in any mix (\\host, /\host, \\?\, \\.\) open another host or a device, which can send the user's NTLM credentials; on Unix // is / and /\x is a root file named \x, so nothing real is lost.
    public static bool IsNetworkPath(string value) => value.Length >= 2 && value[0] is '/' or '\\' && value[1] is '/' or '\\';

    /// <summary>A text for stderr: cleaned of control characters and cut to 100 characters.</summary>
    public static string Printable(string text)
    {
        var clean = TextReport.Clean(text);
        return clean.Length <= MaxName ? clean : clean[..(char.IsHighSurrogate(clean[MaxName - 1]) ? MaxName - 1 : MaxName)];
    }

    /// <summary>Reads a hook's JSON input (UTF-8). Pure: no file, process or network access.</summary>
    public static ImportRead Read(byte[] input)
    {
        try
        {
            return ReadInput(input);
        }
        catch (InvalidOperationException)
        {
            // GetString throws for a string that is not valid Unicode (a lone surrogate escape).
            return Blocked("the hook input is not JSON");
        }
    }

    private static ImportRead.Blocked Blocked(string line) => new([line], true);

    private static ImportRead ReadInput(byte[] input)
    {
        if (input.Length > MaxInputBytes)
        {
            return Blocked("the hook input is larger than 1 MB");
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(input, new JsonDocumentOptions { AllowDuplicateProperties = false });
        }
        catch (JsonException)
        {
            try
            {
                JsonDocument.Parse(input).Dispose();
            }
            catch (JsonException)
            {
                return Blocked("the hook input is not JSON");
            }

            return Blocked("a key appears twice in the hook input");
        }

        using (doc)
        {
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("tool_name", out var nameElement) || nameElement.ValueKind != JsonValueKind.String)
            {
                return Blocked("the hook input has no tool_name");
            }

            var tool = nameElement.GetString()!;
            var shell = tool is "Bash" or "PowerShell" or "Monitor";
            var mcp = tool.StartsWith("mcp__", StringComparison.Ordinal) && tool.EndsWith("__solution_import", StringComparison.Ordinal);
            if (!shell && !mcp)
            {
                return new ImportRead.NotImport();
            }

            if (!root.TryGetProperty("tool_input", out var toolInput) || toolInput.ValueKind != JsonValueKind.Object)
            {
                return Blocked("the hook input has no tool_input object");
            }

            if (!shell)
            {
                return ReadMcp(toolInput);
            }

            if (!toolInput.TryGetProperty("command", out var command))
            {
                // A Monitor watch with a ws object and no command runs no shell.
                return tool == "Monitor" ? new ImportRead.NotImport() : Blocked("the hook input has no command string");
            }

            return command.ValueKind == JsonValueKind.String
                ? ReadShell(command.GetString()!, tool == "PowerShell")
                : Blocked("the hook input has no command string");
        }
    }

    private static ImportRead Result(List<string> lines, ImportCall? call) =>
        lines.Count > 0
            ? new ImportRead.Blocked([.. lines.Distinct()], lines.Any(l => !Routes.Contains(l)))
            : call is null ? new ImportRead.NotImport() : new ImportRead.Check(call);

    // ---- MCP --------------------------------------------------------------------------------------------------

    private static ImportRead ReadMcp(JsonElement toolInput)
    {
        var lines = new List<string>();
        var given = new Dictionary<string, Given>(StringComparer.Ordinal);
        foreach (var property in toolInput.EnumerateObject())
        {
            var name = property.Name;
            if (name == "mcp_server")
            {
                continue;
            }

            if (!McpKeys.Contains(name))
            {
                lines.Add($"the gate doesn't read {Printable(name)}");
            }
            else if (property.Value.ValueKind != JsonValueKind.String)
            {
                lines.Add($"{name} must be a string");
            }
            else if (name == "--stage-and-upgrade" && property.Value.GetString() != "true")
            {
                lines.Add($"{name} must be \"true\"");
            }
            else if (name == "--async" && property.Value.GetString() is not ("true" or "false"))
            {
                lines.Add($"{name} must be \"true\" or \"false\"");
            }
            else if (name is "--path" or "--settings-file" && (property.Value.GetString()!.StartsWith('-') || !property.Value.GetString()!.All(c => char.IsLetterOrDigit(c) || c is '/' or '\\' or ':' or '.' or '_' or '-' or '~')))
            {
                // A free-form string could carry another pac flag, quotes or shell syntax if pac's server builds a command line from it.
                // ~ is for Windows short names (RUNNER~1); the path is fully qualified, so it can't lead.
                lines.Add($"{name} must hold only letters, digits and / \\ : . _ - ~, and not start with '-'");
            }
            else
            {
                given[name] = new Given(property.Value.GetString()!, true);
            }
        }

        return Result(lines, Finish(given, lines));
    }

    // ---- shell ------------------------------------------------------------------------------------------------

    private static ImportRead ReadShell(string command, bool powershell)
    {
        if (powershell)
        {
            return Markers().IsMatch(command) || PacWord().IsMatch(command)
                ? new ImportRead.Blocked(["imports are read only in Bash commands, not in PowerShell"], true)
                : new ImportRead.NotImport();
        }

        var segments = Tokenize(command, out var unclosed);
        var lines = new List<string>();
        if (AnsiCEscape().IsMatch(command))
        {
            lines.Add(AnsiC);
        }

        var recognized = new HashSet<Segment>();
        var imports = new List<Segment>();
        var rest = command.ToCharArray();
        var candidate = false;
        foreach (var segment in segments)
        {
            var words = segment.Words;
            var program = FileName(words[0].Value).ToLowerInvariant();
            if (program is "pac" or "pac.exe")
            {
                candidate = true;
                recognized.Add(segment);
                if (words.Take(3).Any(w => !w.Literal))
                {
                    lines.Add(CantRead);
                    continue;
                }

                switch (string.Join(' ', words.Skip(1).Take(2).Select(w => w.Value.ToLowerInvariant())))
                {
                    case "solution import":
                        imports.Add(segment);
                        break;
                    case "solution upgrade":
                        lines.Add(RouteUpgrade);
                        break;
                    case "package deploy":
                        lines.Add(RoutePackage);
                        break;
                    case "pipeline deploy":
                        lines.Add(RoutePipeline);
                        break;
                    case "pcf push":
                        lines.Add(RoutePcf);
                        break;
                    case "application install":
                        lines.Add(RouteApplication);
                        break;
                    case "code push":
                        lines.Add(RouteCode);
                        break;
                    case "catalog install":
                        lines.Add(RouteCatalog);
                        break;
                    case "app-management install-application-package":
                        lines.Add(RouteAppPackage);
                        break;
                    default:
                        // An option before the subcommand must not hide an import.
                        if (Markers().IsMatch(Join(segment)))
                        {
                            lines.Add(CantRead);
                        }

                        break;
                }
            }
            else if (program is "node" or "node.exe" && words.Length >= 2)
            {
                var verb = words.Length >= 3 ? words[2].Value : "";
                switch (FileName(words[1].Value).ToLowerInvariant())
                {
                    case "solution-settings.js" when verb == "import":
                        lines.Add(RouteModelApps);
                        break;
                    case "solution-settings.js" when verb == "template":
                        break;
                    case "encode-solution-file.js":
                        lines.Add(RoutePowerPages);
                        break;
                    case "import-template-solution.js":
                        lines.Add(RouteTemplate);
                        break;
                    default:
                        continue;
                }

                recognized.Add(segment);
            }
        }

        // Every marker must sit in a recognized segment: what is left after their text is removed must hold none.
        foreach (var segment in recognized)
        {
            Array.Fill(rest, ' ', segment.Start, segment.End - segment.Start);
        }

        if (Markers().IsMatch(new string(rest)) || segments.Where(s => !recognized.Contains(s)).Any(s => Markers().IsMatch(Join(s))))
        {
            lines.Add(CantRead);
        }

        if (unclosed && (candidate || lines.Contains(CantRead)))
        {
            lines.Add("the quotes are not closed");
        }

        if (imports.Count > 1)
        {
            lines.Add("the command holds more than one import");
            return Result(lines, null);
        }

        return Result(lines, imports.Count == 1 ? ReadCli(imports[0].Words, lines) : null);
    }

    private static string Join(Segment segment) => string.Join(' ', segment.Words.Select(w => w.Value));

    private static string FileName(string path) => path[(path.LastIndexOfAny(['/', '\\']) + 1)..];

    private static ImportCall? ReadCli(Word[] words, List<string> lines)
    {
        var given = new Dictionary<string, Given>(StringComparer.Ordinal);
        for (var i = 3; i < words.Length; i++)
        {
            var word = words[i];
            if (word.Redirect)
            {
                // 2>&1 and >&2 change only where pac writes.
                if (word.Raw is not ("2>&1" or ">&2"))
                {
                    lines.Add(Stray);
                }

                continue;
            }

            if (!Canonical.TryGetValue(word.Value, out var flag))
            {
                var name = word.Value.Split('=')[0];
                lines.Add(!word.Value.StartsWith('-') ? Stray
                    : word.Value.Contains('=') && Canonical.TryGetValue(name, out var known) ? $"give {known} and its value as separate words"
                    : $"the gate doesn't read {Printable(name)}");
                continue;
            }

            var value = new Given("", true);
            if (!Switches.Contains(flag))
            {
                if (i + 1 >= words.Length || words[i + 1].Redirect)
                {
                    lines.Add($"{flag} is missing");
                    continue;
                }

                value = new Given(words[i + 1].Value, words[i + 1].Literal);
                i++;
            }

            if (!given.TryAdd(flag, value))
            {
                lines.Add($"{flag} is given twice");
            }
        }

        return Finish(given, lines);
    }

    // The checks both forms share; null (with lines added) when the call is not readable.
    private static ImportCall? Finish(Dictionary<string, Given> given, List<string> lines)
    {
        var before = lines.Count;
        foreach (var flag in new[] { "--path", "--environment" }.Where(f => !given.ContainsKey(f)))
        {
            lines.Add($"{flag} is missing");
        }

        foreach (var flag in new[] { "--path", "--settings-file" }.Where(given.ContainsKey))
        {
            var (value, literal) = (given[flag].Value, given[flag].Literal);
            if (!literal || value.Contains('\0'))
            {
                lines.Add($"{flag} {PathHint}");
            }
            else if (IsNetworkPath(value))
            {
                lines.Add($"{flag} must not be a network or device path");
            }
            else if (!Path.IsPathFullyQualified(value))
            {
                lines.Add($"{flag} {PathHint}");
            }
        }

        if (given.TryGetValue("--environment", out var environment) && !(environment.Literal && IsEnvironment(environment.Value)))
        {
            lines.Add("--environment must be an environment id or the https URL of a Dataverse host");
        }

        if (given.TryGetValue("--max-async-wait-time", out var wait) && !(wait.Literal && wait.Value.Length > 0 && wait.Value.All(char.IsAsciiDigit)))
        {
            lines.Add("--max-async-wait-time must be a whole number of minutes");
        }

        return lines.Count > before
            ? null
            : new ImportCall(given["--path"].Value, given["--environment"].Value, given.TryGetValue("--settings-file", out var settings) ? settings.Value : null, given.ContainsKey("--stage-and-upgrade"));
    }

    // A GUID, or the https URL of a Dataverse host (the hosts that drift and deploy read). It goes to pac org who --environment as --target, so it can never start with '-'.
    private static bool IsEnvironment(string value)
    {
        if (!value.All(c => c is > ' ' and < '\u007f'))
        {
            return false;
        }

        try
        {
            return Guid.TryParseExact(value, "D", out _) || DriftParsers.IsDataverseHost(DriftParsers.EnvironmentUrl(value));
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }

    // ---- the POSIX word splitter ------------------------------------------------------------------------------

    private static List<Segment> Tokenize(string s, out bool unclosed)
    {
        var closedAll = true;
        var segments = new List<Segment>();
        var words = new List<Word>();
        var value = new StringBuilder();
        var (start, segmentStart, segmentEnd) = (-1, 0, 0);
        var (literal, redirect) = (true, false);

        void Begin(int at)
        {
            if (start < 0)
            {
                (start, literal, redirect) = (at, true, false);
                value.Clear();
            }
        }

        void EndWord(int at)
        {
            if (start < 0)
            {
                return;
            }

            if (words.Count == 0)
            {
                segmentStart = start;
            }

            words.Add(new Word(value.ToString(), s[start..at], literal, redirect));
            (segmentEnd, start) = (at, -1);
        }

        void EndSegment(int at)
        {
            EndWord(at);
            if (words.Count > 0)
            {
                segments.Add(new Segment([.. words], segmentStart, segmentEnd));
                words.Clear();
            }
        }

        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            switch (c)
            {
                case ' ' or '\t':
                    EndWord(i);
                    break;
                case '\r':
                    // A shell keeps a carriage return in the word, so the word is not what it looks like.
                    Begin(i);
                    value.Append(c);
                    literal = false;
                    break;
                case '\n' or ';' or '|' or '(' or ')':
                    EndSegment(i);
                    break;
                case '&' when (start >= 0 && s[i - 1] == '>') || (i + 1 < s.Length && s[i + 1] == '>'):
                    // Part of a redirection (2>&1, >&2, &>), not a separator.
                    Begin(i);
                    value.Append(c);
                    literal = false;
                    break;
                case '&':
                    EndSegment(i);
                    break;
                case '<' or '>':
                    Begin(i);
                    value.Append(c);
                    (literal, redirect) = (false, true);
                    break;
                case '\\':
                    // Only a backslash and a newline join lines; before \r\n the shell escapes the \r and the newline ends the command.
                    if (i + 1 < s.Length && s[i + 1] == '\n')
                    {
                        i++;
                    }
                    else
                    {
                        Begin(i);
                        literal = false;
                        value.Append(i + 1 < s.Length ? s[++i] : c);
                    }

                    break;
                case '\'':
                    Begin(i);
                    var close = s.IndexOf('\'', i + 1);
                    closedAll &= close >= 0;
                    var end = close >= 0 ? close : s.Length;
                    value.Append(s, i + 1, end - i - 1);
                    i = end;
                    break;
                case '"':
                    Begin(i);
                    var closed = false;
                    for (i++; i < s.Length && !closed; i++)
                    {
                        var d = s[i];
                        if (d == '"')
                        {
                            closed = true;
                        }
                        else if (d == '\\' && i + 1 < s.Length && s[i + 1] == '\n')
                        {
                            i++;
                        }
                        else if (d == '\\' && i + 1 < s.Length && s[i + 1] is '$' or '`' or '"' or '\\')
                        {
                            literal = false;
                            value.Append(s[++i]);
                        }
                        else
                        {
                            literal &= d is not ('$' or '`');
                            value.Append(d);
                        }
                    }

                    closedAll &= closed;
                    i--;
                    break;
                case '$' when i + 1 < s.Length && s[i + 1] is '\'' or '"':
                    // $'...' and $"..." are quotes to the shell, so $'pac' is pac; the word is still not literal.
                    Begin(i);
                    literal = false;
                    break;
                case '$' or '`' or '*' or '?' or '[' or '{':
                    Begin(i);
                    value.Append(c);
                    literal = false;
                    break;
                case '~':
                    var leading = start < 0;
                    Begin(i);
                    value.Append(c);
                    literal &= !leading;
                    break;
                default:
                    Begin(i);
                    value.Append(c);
                    break;
            }
        }

        EndSegment(s.Length);
        unclosed = !closedAll;
        return segments;
    }
}

/// <summary>What a hook call is: not an import, a call to block with its reasons, or an import to check against the plan.</summary>
public abstract record ImportRead
{
    private ImportRead()
    {
    }

    /// <summary>Not an import: the gate allows it without reading the plan.</summary>
    public sealed record NotImport : ImportRead;

    /// <summary>A call to block. <paramref name="Unreadable"/> is true when the call itself could not be read (the guidance says how to write a readable import), false for a route that is always blocked.</summary>
    public sealed record Blocked(IReadOnlyList<string> Lines, bool Unreadable) : ImportRead;

    /// <summary>A readable import of one zip into one environment.</summary>
    public sealed record Check(ImportCall Call) : ImportRead;
}