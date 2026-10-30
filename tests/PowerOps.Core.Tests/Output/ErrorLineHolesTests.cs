using System.Text.RegularExpressions;

using PowerOps.Tests.Shared;

namespace PowerOps.Core.Tests.Output;

// M33 S-1: an `error:` line that echoes a path, a name or a message from the input must pass it through Clean, or a line break or an escape in it forges a log line.
// Every other hole in such a line must be listed below with the reason it is safe, so a new unwrapped hole fails here.
public class ErrorLineHolesTests
{
    // shortcut: a text scan of single-line `$"error: ..."` literals (also `$"  error: ..."`), not symbol-based; a raw or multi-line literal is not seen. Upgrade to an analyzer if one slips by.
    private static readonly Regex Start = new(@"\$""\s*error: ", RegexOptions.CultureInvariant);

    private static readonly Regex CleanStart = new(@"^\{\s*(TextReport\.)?Clean\(", RegexOptions.CultureInvariant);

    // After the call: an optional alignment (",-10") and format (":N0"), then the closing brace.
    private static readonly Regex Suffix = new(@"^\s*(,\s*-?\d+\s*)?(:[^}]*)?\}$", RegexOptions.CultureInvariant);

    // "<file>: <hole text>". Only holes that are numbers, fixed text, or text another place in src has already cleaned.
    private static readonly HashSet<string> Allowed =
    [
        // Numbers.
        "src/PowerOps.Cli/DiffCommand.cs: {found.Count}",
        "src/PowerOps.Cli/DriftCommand.cs: {i + 1}",
        "src/PowerOps.Cli/DriftCommand.cs: {seen[name] + 1}",
        "src/PowerOps.Cli/DeployCommand.cs: {(int)deadline.TotalSeconds}",
        "src/PowerOps.Cli/McpCommand.cs: {code}",
        "src/PowerOps.Cli/McpCommand.cs: {i + 1}",
        "src/PowerOps.Cli/McpCommand.cs: {f.MinItems}",
        "src/PowerOps.Cli/McpCommand.cs: {f.MaxItems}",
        "src/PowerOps.Cli/McpCommand.cs: {MaxTextLength}",

        // Fixed text: a field or label name from the MCP tool's own table in code, a fixed word.
        "src/PowerOps.Cli/McpCommand.cs: {f.Name}",
        "src/PowerOps.Cli/McpCommand.cs: {label}",
        "src/PowerOps.Cli/McpCommand.cs: {(answer.Action == \"decline\" ? \"declined\" : \"cancelled\")}",
        "src/PowerOps.Cli/DeployCommand.cs: {line}",   // GateBlocked lines: each is fixed text or went through Clean or ImportCall.Printable where it was built.
        "src/PowerOps.Cli/SkillsCommand.cs: {r.Reason}",   // a SafeWrite reason constant

        // A list of usage problems, each fixed text or cleaned where it was added.
        "src/PowerOps.Cli/DeployCommand.cs: {p}",
        "src/PowerOps.Cli/DriftCommand.cs: {p}",

        // Not one Clean call, but every part that comes from input is cleaned inside the expression.
        "src/PowerOps.Cli/DiffCommand.cs: {string.Join(\", \", found.Select(TextReport.Clean))}",   // each joined path is cleaned by the method group
        "src/PowerOps.Cli/AnalyzeCommand.cs: {string.Join(\", \", paths.Select(p => $\"\\\"{TextReport.Clean(p)}\\\"\"))}",   // each joined path is cleaned; the quotes are fixed text
        "src/PowerOps.Cli/DeployCommand.cs: {string.Join(\" and \", g.Select(z => Clean(z.File)))}",   // each joined file is cleaned
        "src/PowerOps.Cli/DriftCommand.cs: {(ex is InvalidDataException ? TextReport.Clean(ex.Message) : \"the snapshot file could not be read\")}",   // one branch cleaned, the other fixed text

        // A SafeWrite refusal: the path in it is cleaned by SafeWrite.Refusal (see SafeWrite.cs).
        "src/PowerOps.Cli/AnalyzeCommand.cs: {refusal.Text}",
        "src/PowerOps.Cli/DiffCommand.cs: {refusal.Text}",
        "src/PowerOps.Cli/SettingsCommand.cs: {refusal.Text}",
        "src/PowerOps.Cli/TestCommand.cs: {refusal.Text}",
    ];

    private static IEnumerable<string> Holes(string line)
    {
        var m = Start.Match(line);
        var i = m.Index + m.Length;
        while (i < line.Length)
        {
            var c = line[i];
            if (c == '"')
            {
                yield break;
            }

            if (c == '{' && i + 1 < line.Length && line[i + 1] == '{')
            {
                i += 2;
                continue;
            }

            if (c != '{')
            {
                i++;
                continue;
            }

            var depth = 0;
            var start = i;
            var inString = false;
            for (; i < line.Length; i++)
            {
                if (line[i] == '"' && (i == 0 || line[i - 1] != '\\'))
                {
                    inString = !inString;
                }
                else if (!inString && line[i] == '{')
                {
                    depth++;
                }
                else if (!inString && line[i] == '}' && --depth == 0)
                {
                    break;
                }
            }

            yield return line[start..Math.Min(i + 1, line.Length)];
            i++;
        }
    }

    // A hole is cleaned only when the whole expression is one `Clean(...)` or `TextReport.Clean(...)` call: the call's closing parenthesis must be followed by nothing but an alignment or format suffix.
    // shortcut: parentheses are counted outside "..." strings only; a ")" in a char literal inside the call would confuse it. Upgrade to a syntax tree if one appears.
    private static bool CleanCall(string hole)
    {
        var start = CleanStart.Match(hole);
        if (!start.Success)
        {
            return false;
        }

        var depth = 1;
        var inString = false;
        for (var i = start.Length; i < hole.Length; i++)
        {
            if (hole[i] == '"' && hole[i - 1] != '\\')
            {
                inString = !inString;
            }
            else if (!inString && hole[i] == '(')
            {
                depth++;
            }
            else if (!inString && hole[i] == ')' && --depth == 0)
            {
                return Suffix.IsMatch(hole[(i + 1)..]);
            }
        }

        return false;
    }

    private static bool Wrapped(string file, string hole) => CleanCall(hole) || Allowed.Contains($"{file}: {hole}");

    [Fact]
    public void Every_hole_in_an_error_line_in_src_is_cleaned_or_listed()
    {
        var src = Path.Combine(Golden.RepoRoot, "src");
        var bad = Directory.EnumerateFiles(src, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Split(Path.DirectorySeparatorChar).Any(part => part is "obj" or "bin"))
            .SelectMany(f => File.ReadAllLines(f).Select((line, n) => (File: Path.GetRelativePath(Golden.RepoRoot, f).Replace('\\', '/'), Line: n + 1, Text: line)))
            .Where(l => Start.IsMatch(l.Text))
            .SelectMany(l => Holes(l.Text).Where(h => !Wrapped(l.File, h)).Select(h => $"{l.File}:{l.Line}: {h}"))
            .ToList();

        Assert.Equal("", string.Join("\n", bad));
    }

    [Fact]
    public void The_scan_sees_a_raw_hole_and_passes_a_cleaned_one()
    {
        Assert.Equal(["{path}"], Holes("error.WriteLine($\"error: path not found: {path}\");").ToArray());
        Assert.Equal(["{TextReport.Clean(path)}", "{x}"], Holes("$\"error: {TextReport.Clean(path)} and {x}\"").ToArray());
        Assert.Equal(["{(a ? \"b\" : c)}"], Holes("$\"error: {(a ? \"b\" : c)} {{literal}}\"").ToArray());
        Assert.True(Wrapped("a.cs", "{Clean(a.B)}"));
        Assert.False(Wrapped("a.cs", "{path}"));
    }

    [Theory]
    [InlineData("{Clean(x)}")]
    [InlineData("{TextReport.Clean(a + b)}")]
    [InlineData("{TextReport.Clean(Display(r.Path, baseDir))}")]
    [InlineData("{TextReport.Clean(\"(\" + x)}")]
    [InlineData("{TextReport.Clean(x),-12}")]
    [InlineData("{TextReport.Clean(x):N0}")]
    public void A_hole_that_is_one_clean_call_passes(string hole)
    {
        Assert.True(Wrapped("a.cs", hole));
    }

    [Theory]
    [InlineData("{TextReport.Clean(prefix) + rawPath}")]
    [InlineData("{rawPath + TextReport.Clean(x)}")]
    [InlineData("{Cleanup(rawPath)}")]
    [InlineData("{TextReport.Clean(a) ?? rawPath}")]
    [InlineData("{TextReport.Clean(a)}{rawPath}")]
    [InlineData("{TextReport.Clean(a) ? rawPath : b}")]
    [InlineData("{TextReport.Clean(a")]
    [InlineData("{x.Clean(a)}")]
    public void A_hole_that_is_not_one_clean_call_is_rejected(string hole)
    {
        Assert.False(Wrapped("a.cs", hole));
    }
}