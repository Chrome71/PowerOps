using System.CommandLine;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

using PowerOps.Core.Output;
using PowerOps.Core.Readers;

namespace PowerOps.Cli;

/// <summary>What <c>doctor</c> reads from the machine; tests pass their own. <paramref name="PluginRecord"/> is the path of Claude Code's <c>installed_plugins.json</c>.</summary>
public sealed record DoctorHost(string Version, string? Path, string? PathExt, string PluginRecord, Func<IReadOnlyList<string>, TimeSpan, PacResult> Az)
{
    public static DoctorHost Default() => new(
        Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0",
        Environment.GetEnvironmentVariable("PATH"),
        Environment.GetEnvironmentVariable("PATHEXT"),
        PluginRecordPath(Environment.GetEnvironmentVariable, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)),
        (args, timeout) => new PacRunner(DriftLiveReader.AzExecutable(OperatingSystem.IsWindows()), timeout).Run(args));

    // Claude Code's plugins root: CLAUDE_CODE_PLUGIN_CACHE_DIR, else CLAUDE_CONFIG_DIR/plugins, else ~/.claude/plugins (its Plugin loading and Environment variables pages).
    internal static string PluginRecordPath(Func<string, string?> env, string home) =>
        System.IO.Path.Combine(
            env("CLAUDE_CODE_PLUGIN_CACHE_DIR") is { Length: > 0 } cache ? cache
                : env("CLAUDE_CONFIG_DIR") is { Length: > 0 } config ? System.IO.Path.Combine(config, "plugins")
                : System.IO.Path.Combine(home, ".claude", "plugins"),
            "installed_plugins.json");
}

public static class DoctorCommand
{
    private const long MaxRecord = 1024 * 1024;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);
    private static readonly string[] MicrosoftMarketplaces = ["power-platform-skills", "dataverse-skills", "skills-for-copilot-studio", "fabric-collection"];

    // The licence read of powerops-plan's section 3 step 8, with the same query; only part numbers reach this process.
    internal static readonly string[] LicenceRead = ["rest", "--method", "get", "--url", "https://graph.microsoft.com/v1.0/me/licenseDetails", "--query", "value[].skuPartNumber", "-o", "tsv"];

    private const string PluginShape = "Claude Code's plugin record has a shape PowerOps doesn't read";

    public static Command Create(TextWriter output, TextWriter error, Func<IReadOnlyList<string>, TimeSpan, PacResult> pac, DoctorHost? host = null)
    {
        var live = new Option<bool>("--live") { Description = "Also check the pac and az sign-ins and read the signed-in account's licence names (calls pac, az and Microsoft Graph; reads only)." };
        var format = new Option<string>("--format") { Description = "Report format.", DefaultValueFactory = _ => "text" };
        format.AcceptOnlyFromAmong("text", "json");
        var command = new Command("doctor", "Check what PowerOps needs on this machine: its version, pac and az, the Claude Code plugins (offline unless --live).") { live, format };
        command.SetAction(parse => PlanCommand.Guard(error, () =>
        {
            var checks = Checks(host ?? DoctorHost.Default(), pac, parse.GetValue(live));
            if (parse.GetValue(format) == "json")
            {
                DoctorReport.WriteJson(checks, parse.GetValue(live), output);
            }
            else
            {
                DoctorReport.WriteText(checks, parse.GetValue(live), output);
            }

            return DoctorReport.ExitCode(checks);
        }));
        return command;
    }

    internal static List<DoctorCheck> Checks(DoctorHost host, Func<IReadOnlyList<string>, TimeSpan, PacResult> pac, bool live)
    {
        var windows = OperatingSystem.IsWindows();
        var pacFound = PacRunner.Resolve("pac", host.Path, windows, host.PathExt) is not null;
        var azFound = PacRunner.Resolve(DriftLiveReader.AzExecutable(windows), host.Path, windows, host.PathExt) is not null;
        var checks = new List<DoctorCheck>
        {
            new("powerops", "powerops", "ok", TextReport.Clean(host.Version)),
            pacFound ? new("pac", "pac", "ok", "found on PATH")
                : new("pac", "pac", "missing", "needed by settings apply, drift, deploy and plan facts. Install pac (dotnet tool install --global Microsoft.PowerApps.CLI.Tool) and add the dotnet tools folder to PATH."),
            azFound ? new("az", "az", "ok", "found on PATH")
                : new("az", "az", "missing", "needed by drift, deploy and the licence read. Install the Azure CLI and sign in with az login."),
        };
        checks.AddRange(Plugins(host.PluginRecord));
        if (!live)
        {
            return checks;
        }

        checks.Add(pacFound ? PacSignIn(pac) : new("pac-sign-in", "pac sign-in", "not-read", "pac is missing"));
        var az = azFound ? AzSignIn(host.Az) : new("az-sign-in", "az sign-in", "not-read", "az is missing");
        checks.Add(az);
        checks.Add(az.Status == "ok" ? Licences(host.Az) : new("licences", "licences", "not-read", "needs the az sign-in"));
        checks.Add(new("environment-type", "environment type", "not-read", "no documented read; you name the plan's environments, and the admin center shows each one's type"));
        return checks;
    }

    // Only the friendly name is kept from pac's output, as plan facts does; nothing a failed call printed is shown.
    private static DoctorCheck PacSignIn(Func<IReadOnlyList<string>, TimeSpan, PacResult> pac)
    {
        var who = pac(["org", "who"], Timeout);
        return who is { Started: true, TimedOut: false, ExitCode: 0 } && SettingsCommand.Field(who.Stdout, "Friendly Name:") is { } name
            ? new("pac-sign-in", "pac sign-in", "ok", TextReport.Clean(name))
            : new("pac-sign-in", "pac sign-in", "failed", PlanFacts.SignInFix);
    }

    // Exit 0 shows an account is selected, not that a token can be issued (decisions A5).
    private static DoctorCheck AzSignIn(Func<IReadOnlyList<string>, TimeSpan, PacResult> az) =>
        az(["account", "show", "-o", "none"], Timeout) is { Started: true, TimedOut: false, ExitCode: 0 }
            ? new("az-sign-in", "az sign-in", "ok", "an account is selected (no token requested)")
            : new("az-sign-in", "az sign-in", "failed", "Sign in with az login.");

    // Product names only, through licensing.md section 3; a part number the table doesn't list is counted, never printed.
    private static DoctorCheck Licences(Func<IReadOnlyList<string>, TimeSpan, PacResult> az)
    {
        var read = az(LicenceRead, Timeout);
        var parts = read is { Started: true, TimedOut: false, ExitCode: 0 }
            ? read.Stdout.Split('\n').Select(l => l.TrimEnd('\r')).Where(l => l.Length > 0).Distinct(StringComparer.Ordinal).ToList()
            : [];
        // Learn's part numbers hold spaces, brackets, slashes and commas, so only control characters and length are refused; nothing read is echoed.
        if (parts.Count == 0 || parts.Any(p => p.Length > 256 || p.Any(char.IsControl)))
        {
            return new("licences", "licences", "not-read", "the licence read failed; the planning skill asks which products you hold instead");
        }

        var table = PartNumbers();
        var products = table.Where(t => parts.Contains(t.Part, StringComparer.Ordinal)).Select(t => t.Product).Distinct().ToList();
        var other = parts.Count(p => !table.Any(t => t.Part == p));
        var detail = products.Count == 0
            ? $"none in PowerOps' table ({other} other)"
            : string.Join(", ", products) + (other == 0 ? "" : $"; {other} not in PowerOps' table");
        return new("licences", "licences", "ok", detail);
    }

    /// <summary>The rows of section 3 of the shipped <c>powerops-plan/references/licensing.md</c>: part number and product.</summary>
    internal static IReadOnlyList<(string Part, string Product)> PartNumbers() =>
        [.. SkillsCommand.Shipped()["powerops-plan/references/licensing.md"].Split('\n')
            .SkipWhile(l => !l.StartsWith("## 3.", StringComparison.Ordinal)).Skip(1).TakeWhile(l => !l.StartsWith("## ", StringComparison.Ordinal))
            .Where(l => l.StartsWith("| `", StringComparison.Ordinal))
            .Select(l => l.Split('|'))
            .Select(cells => (cells[1].Trim().Trim('`'), cells[2].Trim()))];

    private static IEnumerable<DoctorCheck> Plugins(string record)
    {
        if (!File.Exists(record))
        {
            return [OursMissing, MicrosoftMissing];
        }

        if (Record(record) is not { } installs)
        {
            return [new("powerops-plugin", "powerops plugin", "not-read", PluginShape), new("microsoft-plugins", "Microsoft plugins", "not-read", PluginShape)];
        }

        var ours = installs.Where(i => i.Id == "powerops@powerops").Select(i => $"{i.Version} ({i.Scope})").Distinct().ToList();
        var microsoft = installs.Where(i => MicrosoftMarketplaces.Contains(i.Id[(i.Id.LastIndexOf('@') + 1)..]))
            .GroupBy(i => i.Id, StringComparer.Ordinal).OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => $"{g.Key} {string.Join("/", g.Select(i => i.Version).Distinct())}").ToList();
        return
        [
            ours.Count > 0 ? new("powerops-plugin", "powerops plugin", "ok", TextReport.Clean(string.Join(", ", ours) + " installed in Claude Code")) : OursMissing,
            microsoft.Count > 0 ? new("microsoft-plugins", "Microsoft plugins", "ok", TextReport.Clean(string.Join(", ", microsoft))) : MicrosoftMissing,
        ];
    }

    private static readonly DoctorCheck OursMissing = new("powerops-plugin", "powerops plugin", "not-installed",
        "not installed in Claude Code. claude plugin marketplace add Chrome71/PowerOps, then claude plugin install powerops@powerops; or powerops skills install in the repo");

    private static readonly DoctorCheck MicrosoftMissing = new("microsoft-plugins", "Microsoft plugins", "not-installed",
        "none of Microsoft's plugins in Claude Code; the build steps use them (docs/HANDOFF.md)");

    // The shape observed on 2026-11-22: {"version": 2, "plugins": {"<plugin>@<marketplace>": [{"scope": ..., "version": ..., ...}]}}. Anything else is null.
    private static List<(string Id, string Version, string Scope)>? Record(string path)
    {
        try
        {
            var root = JsonNode.Parse(BoundedRead.ReadAllText(path, MaxRecord));
            if (root?["version"] is not JsonValue v || !v.TryGetValue<int>(out var n) || n != 2 || root["plugins"] is not JsonObject plugins)
            {
                return null;
            }

            var installs = new List<(string, string, string)>();
            foreach (var (id, list) in plugins)
            {
                if (list is not JsonArray entries || !id.Contains('@'))
                {
                    return null;
                }

                foreach (var e in entries)
                {
                    if (e?["version"] is not JsonValue ver || !ver.TryGetValue<string>(out var version) || e["scope"] is not JsonValue sc || !sc.TryGetValue<string>(out var scope))
                    {
                        return null;
                    }

                    installs.Add((id, version, scope));
                }
            }

            return installs;
        }
        catch (Exception ex) when (ex is InvalidDataException or JsonException or IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            return null;
        }
    }
}
