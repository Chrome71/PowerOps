using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

// Runs .github/scripts/ecosystem-watch.sh against a curl stub (M84, row 5 of docs/plans/m84-ecosystem-watch/plan.md): the inventory's format,
// the report, --update and the token's hygiene. The letters in the test names are the cases of that row.
public sealed class EcosystemWatchTests
{
    private const string Sha = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"; // the stub's default commit
    private const string OtherSha = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private const string Date = "2026-11-01T10:00:00Z"; // the stub's default commit date
    private const string ManualUrl = "https://learn.microsoft.com/example/whats-new";
    private const string NugetUrl = "https://api.nuget.org/v3-flatcontainer/example.tool/index.json";
    private const string NpmUrl = "https://registry.npmjs.org/-/package/@example/cli/dist-tags";
    private const string ReleaseUrl = "https://api.github.com/repos/example/rel/releases/latest";
    private const string HeadUrl = "https://api.github.com/repos/example/head/commits?per_page=1";
    private const string PageUrl = "https://api.github.com/repos/example/docs/commits?per_page=1&path=docs/page.md";

    private static readonly string Script = Path.Combine(Golden.RepoRoot, ".github", "scripts", "ecosystem-watch.sh");
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    // The stub curl logs each call's argv and stdin, writes the URL's fixture (the URL is its last argument) to the -o file and prints the status as
    // -w '%{http_code}' would. A URL with no fixture gets a default answer by its shape, so the real inventory needs no fixture per source; any other
    // URL gets 404.
    private const string CurlStub = """
        #!/usr/bin/env bash
        n=$(( $(cat "$STUB_DIR/count" 2> /dev/null || echo 0) + 1 ))
        echo "$n" > "$STUB_DIR/count"
        printf '%s\n' "$@" > "$STUB_DIR/calls/$n.argv"
        cat > "$STUB_DIR/calls/$n.stdin"
        url="${!#}"
        out=""
        prev=""
        for a in "$@"; do
          if [ "$prev" = "-o" ]; then out="$a"; fi
          prev="$a"
        done
        f="$STUB_DIR/fixtures/$(printf '%s' "$url" | tr -c 'A-Za-z0-9' '_')"
        if [ -f "$f.exit" ]; then exit "$(cat "$f.exit")"; fi
        status=200
        if [ -f "$f.body" ]; then
          cp "$f.body" "$out"
          status="$(cat "$f.status")"
        else
          case "$url" in
            https://api.nuget.org/v3-flatcontainer/*/index.json) echo '{"versions":["1.0.0","1.2.0"]}' > "$out" ;;
            https://registry.npmjs.org/-/package/*/dist-tags) echo '{"latest":"1.2.0","next":"2.0.0-beta.1"}' > "$out" ;;
            https://api.github.com/repos/*/*/releases/latest) echo '{"tag_name":"v1.2.0","name":"Release"}' > "$out" ;;
            "https://api.github.com/repos/"*"/commits?per_page=1"*)
              echo '[{"sha":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","commit":{"committer":{"date":"2026-11-01T10:00:00Z"}}}]' > "$out" ;;
            *) echo '{"message":"Not Found"}' > "$out"; status=404 ;;
          esac
        fi
        printf '%s' "$status"
        """;

    private sealed record Call(string[] Argv, string Stdin)
    {
        public string Url => Argv[^1];
    }

    private sealed record Result(int Exit, string Stdout, string Stderr, string Summary, string Output, Call[] Calls);

    // A temp folder with the stub first on PATH, its fixtures and an inventory; each Run starts with an empty call log, summary and output file.
    private sealed class Sandbox : IDisposable
    {
        public Sandbox(string inventory)
        {
            Tools.Require("bash", "jq");
            Root = Directory.CreateTempSubdirectory("powerops-ecosystem-watch-").FullName;
            var bin = Directory.CreateDirectory(Path.Combine(Root, "bin")).FullName;
            Directory.CreateDirectory(Path.Combine(Root, "fixtures"));
            Directory.CreateDirectory(Path.Combine(Root, "inventory"));
            var stub = Path.Combine(bin, "curl");
            File.WriteAllText(stub, CurlStub.Replace("\r\n", "\n") + "\n");
            var posix = !OperatingSystem.IsWindows(); // lets the platform analyzer see the guard
            if (posix)
            {
                File.SetUnixFileMode(stub, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }

            File.WriteAllText(Inventory, inventory);
        }

        public string Root { get; }

        public string Inventory => Path.Combine(Root, "inventory", "sources.json");

        public void Answer(string url, string body, int status = 200)
        {
            File.WriteAllText(Fixture(url) + ".body", body);
            File.WriteAllText(Fixture(url) + ".status", status.ToString(CultureInfo.InvariantCulture));
        }

        public void Fail(string url, int curlExit) => File.WriteAllText(Fixture(url) + ".exit", curlExit.ToString(CultureInfo.InvariantCulture));

        private string Fixture(string url) => Path.Combine(Root, "fixtures", Regex.Replace(url, "[^A-Za-z0-9]", "_"));

        public Result Run(params string[] args) => RunWith([], args);

        // Runs the script with --inventory pointing at this sandbox's inventory unless args name another.
        public Result RunWith(Dictionary<string, string> env, params string[] args)
        {
            var calls = Path.Combine(Root, "calls");
            if (Directory.Exists(calls))
            {
                Directory.Delete(calls, recursive: true);
            }

            Directory.CreateDirectory(calls);
            var summary = Path.Combine(Root, "summary.md");
            var output = Path.Combine(Root, "output.txt");
            foreach (var file in new[] { Path.Combine(Root, "count"), summary, output })
            {
                File.Delete(file);
            }

            var start = new ProcessStartInfo(Tools.Bash)
            {
                WorkingDirectory = Root,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
            };
            start.ArgumentList.Add(Script);
            if (!args.Contains("--inventory"))
            {
                start.ArgumentList.Add("--inventory");
                start.ArgumentList.Add(Inventory);
            }

            foreach (var a in args)
            {
                start.ArgumentList.Add(a);
            }

            start.Environment["PATH"] = Path.Combine(Root, "bin") + Path.PathSeparator + Environment.GetEnvironmentVariable("PATH");
            start.Environment["STUB_DIR"] = Root;
            start.Environment["GITHUB_STEP_SUMMARY"] = summary;
            start.Environment["GITHUB_OUTPUT"] = output;
            start.Environment.Remove("GITHUB_TOKEN");
            foreach (var (key, value) in env)
            {
                start.Environment[key] = value;
            }

            using var process = Process.Start(start)!;
            process.StandardInput.Close();
            var stderr = process.StandardError.ReadToEndAsync();
            var stdout = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            var logged = Directory.GetFiles(calls, "*.argv")
                .Select(f => int.Parse(Path.GetFileNameWithoutExtension(f), CultureInfo.InvariantCulture))
                .Order()
                .Select(n => new Call(
                    File.ReadAllLines(Path.Combine(calls, n + ".argv")),
                    File.ReadAllText(Path.Combine(calls, n + ".stdin"))))
                .ToArray();
            return new Result(
                process.ExitCode,
                stdout,
                stderr.Result,
                File.Exists(summary) ? File.ReadAllText(summary) : "",
                File.Exists(output) ? File.ReadAllText(output) : "",
                logged);
        }

        public void Dispose() => Directory.Delete(Root, recursive: true);
    }

    private static JsonObject Auto(string id, string kind, string target, string? lastSeen = null, string? path = null)
    {
        var source = new JsonObject
        {
            ["id"] = id,
            ["title"] = "Title of " + id,
            ["kind"] = kind,
            ["target"] = target,
            ["areas"] = new JsonArray("pac", "alm"),
            ["affects"] = "what depends on " + id,
            ["lastSeen"] = lastSeen,
            ["lastChecked"] = null,
        };
        if (path is not null)
        {
            source["path"] = path;
        }

        return source;
    }

    private static JsonObject Manual(string id, string url, string? lastChecked = null) => new()
    {
        ["id"] = id,
        ["title"] = "Title of " + id,
        ["kind"] = "manual",
        ["target"] = url,
        ["areas"] = new JsonArray("governance"),
        ["affects"] = "what depends on " + id,
        ["everyDays"] = 30,
        ["lastChecked"] = lastChecked,
    };

    private static string Inventory(params JsonObject[] sources) => new JsonObject { ["sources"] = new JsonArray(sources) }.ToJsonString(Indented);

    // Five automated sources, one of each shape, and one manual source.
    private static string Standard(string? nuget = null, string? npm = null, string? release = null, string? head = null, string? page = null) =>
        Inventory(
            Auto("tool", "nuget", "example.tool", nuget),
            Auto("cli", "npm", "@example/cli", npm),
            Auto("rel", "github-release", "example/rel", release),
            Auto("head", "github-commits", "example/head", head),
            Auto("page", "github-commits", "example/docs", page, "docs/page.md"),
            Manual("roadmap", ManualUrl));

    // The non-empty lines of a report section, up to the next section.
    private static string[] Section(string report, string name) =>
        report.Split('\n').SkipWhile(l => l != "## " + name).Skip(1).TakeWhile(l => !l.StartsWith("## ", StringComparison.Ordinal)).Where(l => l.Length > 0).ToArray();

    private static string[] Ids(string report, string name) => Section(report, name).Select(l => l[2..].Split(' ', ':')[0]).ToArray();

    private static string Observations(string report) => string.Join(" ", Regex.Matches(report, "`[a-z0-9-]+@[^`]+`").Select(m => m.Value));

    private static string Today(string report) => Regex.Match(report, @"\A# Ecosystem watch, (\d{4}-\d{2}-\d{2}) UTC\n").Groups[1].Value;

    [Fact]
    public void A_first_run_lists_every_automated_source_as_new_with_its_links_and_writes_the_outputs_but_no_file()
    {
        var inventory = Standard(head: OtherSha);
        using var box = new Sandbox(inventory);

        var run = box.Run();

        Assert.Equal(0, run.Exit);
        Assert.Equal("", run.Stderr);
        Assert.Matches(@"\A# Ecosystem watch, \d{4}-\d{2}-\d{2} UTC\n\nNew: 5\. Not checked: 0\. Unchanged: 0\. Due by hand: 1 of 1\.\n", run.Stdout);
        Assert.Equal(["tool", "cli", "rel", "head", "page"], Ids(run.Stdout, "New"));
        Assert.Empty(Section(run.Stdout, "Not checked"));
        Assert.Empty(Section(run.Stdout, "Unchanged"));
        Assert.Equal(["roadmap"], Ids(run.Stdout, "Checked by hand"));
        Assert.Contains($"(https://github.com/example/head/compare/{OtherSha}...{Sha})", run.Stdout);
        Assert.Contains($"(https://github.com/example/docs/commits/{Sha}/docs/page.md)", run.Stdout);
        Assert.Contains("(https://github.com/example/rel/releases/tag/v1.2.0)", run.Stdout);
        Assert.DoesNotContain("/compare/", Section(run.Stdout, "New").Single(l => l.StartsWith("- page ", StringComparison.Ordinal)));
        Assert.Equal($"`tool@1.2.0` `cli@1.2.0` `rel@v1.2.0` `head@{Sha}` `page@{Sha}`", Observations(run.Stdout));
        Assert.Equal("new=5\nfailed=0\ndue=1\n", run.Output);
        Assert.Equal(run.Stdout, run.Summary);
        Assert.Equal(inventory, File.ReadAllText(box.Inventory));
        Assert.Equal([box.Inventory], Directory.GetFiles(Path.GetDirectoryName(box.Inventory)!));
    }

    [Fact]
    public void B_an_equal_value_is_unchanged_and_a_version_that_went_back_or_another_sha_is_new()
    {
        using var box = new Sandbox(Standard(nuget: "1.2.0", npm: "2.15.0", release: "v1.2.0", head: OtherSha, page: Sha));
        box.Answer(NpmUrl, """{"latest":"2.14.1"}""");

        var run = box.Run();

        Assert.Equal(0, run.Exit);
        Assert.Equal(["tool", "rel", "page"], Ids(run.Stdout, "Unchanged"));
        Assert.Equal(["cli", "head"], Ids(run.Stdout, "New"));
        Assert.Contains("`2.15.0` -> `2.14.1`", Section(run.Stdout, "New")[0]);
        Assert.Contains($"`{OtherSha}` -> `{Sha}`", Section(run.Stdout, "New")[1]);
    }

    [Theory]
    [InlineData(404, 0, "HTTP 404")]
    [InlineData(500, 0, "HTTP 500")]
    [InlineData(403, 0, "HTTP 403, possibly a rate limit")]
    [InlineData(429, 0, "HTTP 429, possibly a rate limit")]
    [InlineData(200, 28, "curl failed with exit code 28")]
    public void C_a_failed_request_is_not_checked_with_its_reason_and_never_unchanged(int status, int curlExit, string reason)
    {
        using var box = new Sandbox(Standard(release: "v1.2.0"));
        if (curlExit == 0)
        {
            box.Answer(ReleaseUrl, """{"tag_name":"v1.2.0"}""", status);
        }
        else
        {
            box.Fail(ReleaseUrl, curlExit);
        }

        var run = box.Run();

        Assert.Equal(0, run.Exit);
        Assert.Equal($"- rel (github-release): last seen `v1.2.0`, not checked: {reason}; this is not \"no change\"", Section(run.Stdout, "Not checked").Single());
        Assert.DoesNotContain("rel", Ids(run.Stdout, "Unchanged"));
        Assert.DoesNotContain("rel", Ids(run.Stdout, "New"));
        Assert.Contains("failed=1\n", run.Output);
    }

    [Fact]
    public void D_an_empty_commit_list_is_not_checked()
    {
        using var box = new Sandbox(Standard(page: Sha));
        box.Answer(PageUrl, "[]");

        var run = box.Run();

        Assert.Equal(["page"], Ids(run.Stdout, "Not checked"));
        Assert.Contains("the commit list is empty", Section(run.Stdout, "Not checked")[0]);
        Assert.Contains("Not checked: 1. Unchanged: 0.", run.Stdout);
    }

    [Fact]
    public void E_a_body_that_is_not_json_or_a_value_that_fails_its_pattern_is_not_checked_and_never_printed()
    {
        var shortSha = new string('c', 39);
        using var box = new Sandbox(Standard());
        box.Answer(NugetUrl, "<html>not json</html>");
        box.Answer(ReleaseUrl, """{"tag_name":"v1<b>"}""");
        box.Answer(HeadUrl, JsonSerializer.Serialize(new[] { new { sha = shortSha, commit = new { committer = new { date = Date } } } }));

        var run = box.Run();

        Assert.Equal(0, run.Exit);
        Assert.Equal(["tool", "rel", "head"], Ids(run.Stdout, "Not checked"));
        Assert.Equal(["cli", "page"], Ids(run.Stdout, "New"));
        Assert.DoesNotContain("<b>", run.Stdout);
        Assert.DoesNotContain("html", run.Stdout);
        Assert.DoesNotContain(shortSha, run.Stdout);
        Assert.Contains("failed=3\n", run.Output);
    }

    [Fact]
    public void F_two_runs_give_the_same_report_apart_from_line_1_and_the_same_observation_ids()
    {
        using var box = new Sandbox(Standard(nuget: "1.2.0", head: OtherSha));
        box.Answer(PageUrl, "[]");

        var first = box.Run();
        var second = box.Run();

        Assert.Equal(first.Stdout.Split('\n').Skip(1), second.Stdout.Split('\n').Skip(1));
        Assert.NotEqual("", Observations(first.Stdout));
        Assert.Equal(Observations(first.Stdout), Observations(second.Stdout));
    }

    [Fact]
    public void G_update_records_the_values_read_and_nothing_else_and_a_rerun_finds_them_unchanged()
    {
        var inventory = Inventory(
            Auto("tool", "nuget", "example.tool"),
            Auto("cli", "npm", "@example/cli", "0.9.0"),
            Auto("rel", "github-release", "example/rel", "v0.9.0"),
            Auto("page", "github-commits", "example/docs", OtherSha, "docs/page.md"),
            Manual("roadmap", ManualUrl, "2026-01-05"));
        using var box = new Sandbox(inventory);
        box.Answer(ReleaseUrl, "{}", 503);

        var update = box.Run("--update");

        Assert.Equal(0, update.Exit);
        Assert.Equal(["tool", "cli", "page"], Ids(update.Stdout, "New"));
        var today = Today(update.Stdout);
        var before = JsonNode.Parse(inventory)!["sources"]!.AsArray();
        var text = File.ReadAllText(box.Inventory);
        var after = JsonNode.Parse(text)!["sources"]!.AsArray();
        Assert.StartsWith("{\n  \"sources\": [\n    {\n      \"id\": \"tool\",\n", text);
        Assert.Equal(before.Select(s => string.Join(",", s!.AsObject().Select(p => p.Key))), after.Select(s => string.Join(",", s!.AsObject().Select(p => p.Key))));
        var expected = JsonNode.Parse(inventory)!["sources"]!.AsArray();
        foreach (var (index, value) in new[] { (0, "1.2.0"), (1, "1.2.0"), (3, Sha) })
        {
            expected[index]!["lastSeen"] = value;
            expected[index]!["lastChecked"] = today;
        }

        Assert.True(JsonNode.DeepEquals(expected, after), text);
        Assert.Equal([box.Inventory], Directory.GetFiles(Path.GetDirectoryName(box.Inventory)!));

        var rerun = box.Run();

        Assert.Equal(0, rerun.Exit);
        Assert.Equal(["tool", "cli", "page"], Ids(rerun.Stdout, "Unchanged"));
        Assert.Empty(Section(rerun.Stdout, "New"));
        Assert.Equal(["rel"], Ids(rerun.Stdout, "Not checked"));
        Assert.Contains("last seen `v0.9.0`", Section(rerun.Stdout, "Not checked")[0]);
    }

    [Fact]
    public void G_update_refuses_a_symbolic_link_before_any_request_and_writes_nothing()
    {
        var inventory = Standard();
        using var box = new Sandbox(inventory);
        var link = Path.Combine(Path.GetDirectoryName(box.Inventory)!, "link.json");
        File.CreateSymbolicLink(link, box.Inventory);

        var run = box.Run("--update", "--inventory", link);

        Assert.Equal(2, run.Exit);
        Assert.StartsWith("error: ", run.Stderr);
        Assert.Single(run.Stderr.Split('\n', StringSplitOptions.RemoveEmptyEntries));
        Assert.Empty(run.Calls);
        Assert.Equal(inventory, File.ReadAllText(box.Inventory));
        Assert.Equal([link, box.Inventory], Directory.GetFiles(Path.GetDirectoryName(box.Inventory)!).Order());
    }

    [Fact]
    public void H_the_token_reaches_only_the_stdin_of_calls_to_api_github_com_and_every_call_is_https_only()
    {
        const string Marker = "ghs_MarkerToken0123456789";
        using var box = new Sandbox(Standard());

        var run = box.RunWith(new() { ["GITHUB_TOKEN"] = Marker });

        Assert.Equal(0, run.Exit);
        Assert.Equal(5, run.Calls.Length);
        Assert.All(run.Calls, c => Assert.DoesNotContain(c.Argv, a => a.Contains(Marker, StringComparison.Ordinal)));
        Assert.All(run.Calls, c => Assert.Equal(c.Url.StartsWith("https://api.github.com/", StringComparison.Ordinal), c.Stdin.Contains(Marker, StringComparison.Ordinal)));
        Assert.All(run.Calls, c =>
        {
            Assert.Equal("=https", c.Argv[Array.IndexOf(c.Argv, "--proto") + 1]);
            Assert.Equal("=https", c.Argv[Array.IndexOf(c.Argv, "--proto-redir") + 1]);
            Assert.Equal("%{http_code}", c.Argv[Array.IndexOf(c.Argv, "-w") + 1]);
        });
        Assert.DoesNotContain(Marker, run.Stdout + run.Stderr + run.Summary + run.Output);

        var anonymous = box.Run();

        Assert.All(anonymous.Calls, c => Assert.Equal("", c.Stdin));
    }

    [Fact]
    public void I_untrusted_text_in_a_response_never_reaches_the_report()
    {
        const string Evil = "[x](y) @user #1 ` <script>alert(1)</script> %0A\n::error::boom https://example.invalid/steal www.example.invalid café";
        var person = new { name = Evil, email = Evil, date = Date };
        using var box = new Sandbox(Standard(release: "v1.0.0", head: OtherSha));
        box.Answer(ReleaseUrl, JsonSerializer.Serialize(new { tag_name = "v1.2.0", name = Evil, body = Evil, html_url = "https://example.invalid/r" }));
        box.Answer(HeadUrl, JsonSerializer.Serialize(new[] { new { sha = Sha, commit = new { message = Evil, author = person, committer = person }, author = new { login = Evil } } }));
        var badDate = new { name = Evil, date = Date + "` [x](https://example.invalid/d) " + Evil };
        box.Answer(PageUrl, JsonSerializer.Serialize(new[] { new { sha = Sha, commit = new { message = Evil, committer = badDate } } }));

        var report = box.Run().Stdout;

        Assert.Equal(["tool", "cli", "rel", "head", "page"], Ids(report, "New"));
        Assert.All(report, c => Assert.True(c < 128, report));
        Assert.All(report.Split('\n').Skip(1), l => Assert.True(l.Length == 0 || l[0] == '#' || l[0] == '-' || char.IsAsciiLetterUpper(l[0]), l));
        var outsideSpans = Regex.Replace(report, "`[^`\n]*`", "");
        var targets = Regex.Matches(outsideSpans, @"\]\(([^)]*)\)").Select(m => m.Groups[1].Value).ToArray();
        Assert.NotEmpty(targets);
        Assert.All(targets, t => Assert.True(t.StartsWith("https://github.com/example/", StringComparison.Ordinal) || t == ManualUrl, t));
        var plain = Regex.Replace(outsideSpans, @"\]\([^)]*\)", "]");
        foreach (var bad in new[] { "example.invalid", "@user", "#1", "<script>", "%0A", "::error::", "[x]", "caf", "`" })
        {
            Assert.DoesNotContain(bad, plain);
        }

        foreach (var value in new[] { "v1.2.0", "v1.0.0", Sha, OtherSha, Date, "1.2.0" })
        {
            Assert.DoesNotContain(value, plain);
        }

        Assert.Contains($"(committed `{Date}`)", Section(report, "New").Single(l => l.StartsWith("- head ", StringComparison.Ordinal)));
        Assert.DoesNotContain("committed", Section(report, "New").Single(l => l.StartsWith("- page ", StringComparison.Ordinal)));
    }

    // Each case changes one thing in a good inventory, or passes an unknown option.
    private static (string Inventory, string[] Args) Refusal(string bad)
    {
        var npm = Auto("cli", "npm", "@example/cli");
        var commits = Auto("page", "github-commits", "example/docs", null, "docs/page.md");
        var manual = Manual("roadmap", ManualUrl);
        string[] args = [];
        switch (bad)
        {
            case "none": break;
            case "missing inventory": args = ["--inventory", Path.Combine(Path.GetTempPath(), "powerops-no-such-folder", "sources.json")]; break;
            case "invalid JSON": return ("{\"sources\": [", []);
            case "duplicate id": commits["id"] = "cli"; break;
            case "unknown kind": npm["kind"] = "pypi"; break;
            case "unknown area": npm["areas"] = new JsonArray("pac", "sales"); break;
            case "automated source without lastSeen": npm.Remove("lastSeen"); break;
            case "manual source without everyDays": manual.Remove("everyDays"); break;
            case "manual source with lastSeen": manual["lastSeen"] = null; break;
            case "path on an npm source": npm["path"] = "docs"; break;
            case "owner/repo with /../": commits["target"] = "example/../docs"; break;
            case "owner/repo of two dots": commits["target"] = "../.."; break;
            case "npm name with a space": npm["target"] = "@example/my cli"; break;
            case "manual URL with http:": manual["target"] = "http://learn.microsoft.com/example"; break;
            case "path with ..": commits["path"] = "docs/../secret"; break;
            case "bad date": npm["lastChecked"] = "2026-02-30"; break;
            case "lastSeen sha of 39 characters": commits["lastSeen"] = new string('a', 39); break;
            case "lastSeen tag holding a backtick": npm["lastSeen"] = "1.0`"; break;
            case "manual URL holding a space": manual["target"] = "https://learn.microsoft.com/a b"; break;
            case "everyDays of 0": manual["everyDays"] = 0; break;
            case "title holding a bracket": npm["title"] = "Title [x](y)"; break;
            case "unknown option": args = ["--frobnicate"]; break;
            default: throw new ArgumentException(bad);
        }

        return (Inventory(npm, commits, manual), args);
    }

    [Theory]
    [InlineData("missing inventory")]
    [InlineData("invalid JSON")]
    [InlineData("duplicate id")]
    [InlineData("unknown kind")]
    [InlineData("unknown area")]
    [InlineData("automated source without lastSeen")]
    [InlineData("manual source without everyDays")]
    [InlineData("manual source with lastSeen")]
    [InlineData("path on an npm source")]
    [InlineData("owner/repo with /../")]
    [InlineData("owner/repo of two dots")]
    [InlineData("npm name with a space")]
    [InlineData("manual URL with http:")]
    [InlineData("path with ..")]
    [InlineData("bad date")]
    [InlineData("lastSeen sha of 39 characters")]
    [InlineData("lastSeen tag holding a backtick")]
    [InlineData("manual URL holding a space")]
    [InlineData("everyDays of 0")]
    [InlineData("title holding a bracket")]
    [InlineData("unknown option")]
    public void J_a_bad_inventory_or_option_is_refused_with_one_error_line_before_any_request(string bad)
    {
        var (inventory, args) = Refusal(bad);
        using var box = new Sandbox(inventory);

        var run = box.Run(args);

        Assert.Equal(2, run.Exit);
        Assert.Equal("", run.Stdout);
        Assert.StartsWith("error: ", Assert.Single(run.Stderr.Split('\n', StringSplitOptions.RemoveEmptyEntries)));
        Assert.Empty(run.Calls);
        Assert.Equal("", run.Output);
    }

    [Fact]
    public void J_the_good_inventory_the_refusals_start_from_is_accepted()
    {
        var (inventory, args) = Refusal("none");
        using var box = new Sandbox(inventory);

        var run = box.Run(args);

        Assert.Equal(0, run.Exit);
        Assert.Equal(["cli", "page"], Ids(run.Stdout, "New"));
    }

    [Fact]
    public void K_a_manual_source_is_due_when_never_checked_and_not_when_checked_within_its_days()
    {
        var yesterday = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        using var box = new Sandbox(Inventory(Manual("never", ManualUrl), Manual("recent", ManualUrl + "/recent", yesterday)));

        var run = box.Run();

        Assert.Equal(0, run.Exit);
        Assert.Empty(run.Calls);
        var lines = Section(run.Stdout, "Checked by hand");
        Assert.Equal($"- never: [Title of never]({ManualUrl}), every 30 days, last checked never, due", lines[0]);
        Assert.Equal($"- recent: [Title of recent]({ManualUrl}/recent), every 30 days, last checked {yesterday}", lines[1]);
        Assert.Contains("Due by hand: 1 of 2.", run.Stdout);
        Assert.Equal("new=0\nfailed=0\ndue=1\n", run.Output);
    }

    [Fact]
    public void A_run_past_its_time_budget_makes_no_more_requests_and_says_why()
    {
        using var box = new Sandbox(Standard());

        // bash takes SECONDS from the environment, so the run starts as if 1000 seconds had passed.
        var run = box.RunWith(new() { ["SECONDS"] = "1000" });

        Assert.Equal(0, run.Exit);
        Assert.Empty(run.Calls);
        Assert.Equal(["tool", "cli", "rel", "head", "page"], Ids(run.Stdout, "Not checked"));
        Assert.All(Section(run.Stdout, "Not checked"), l => Assert.Contains("not checked: the run's 420-second budget was spent", l));
    }

    [Fact]
    public void L_the_real_inventory_is_accepted_and_every_source_is_read_or_listed()
    {
        var real = File.ReadAllText(Path.Combine(Golden.RepoRoot, "docs", "ecosystem", "sources.json"));
        var sources = JsonNode.Parse(real)!["sources"]!.AsArray().Select(s => (Id: (string)s!["id"]!, Kind: (string)s!["kind"]!)).ToArray();
        using var box = new Sandbox(real);

        var run = box.Run();

        Assert.Equal(0, run.Exit);
        Assert.Empty(Section(run.Stdout, "Not checked"));
        Assert.Equal(sources.Where(s => s.Kind != "manual").Select(s => s.Id), Ids(run.Stdout, "New").Concat(Ids(run.Stdout, "Unchanged")).OrderBy(id => Array.FindIndex(sources, s => s.Id == id)));
        Assert.Equal(sources.Where(s => s.Kind == "manual").Select(s => s.Id), Ids(run.Stdout, "Checked by hand"));
        Assert.Equal(sources.Count(s => s.Kind != "manual"), run.Calls.Length);
    }
}