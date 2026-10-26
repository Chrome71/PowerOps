using System.Text;

using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

public class TestCommandTests : IDisposable
{
    private static readonly string Fixtures = Path.Combine(Golden.RepoRoot, "tests", "fixtures");
    private static readonly string Seed = Path.Combine(Fixtures, "powerops-seed", "v2");
    private static readonly string SeedTests = Path.Combine(Fixtures, "flowtests", "seed");
    private static readonly string Alm = Path.Combine(Fixtures, "alm-makers");
    private static readonly string AlmTests = Path.Combine(Fixtures, "flowtests", "alm-makers");
    private readonly string _temp = Directory.CreateTempSubdirectory("powerops-cli-").FullName;

    public void Dispose() => Directory.Delete(_temp, recursive: true);

    private static (int Code, string Out, string Err) Run(params string[] args)
    {
        var o = new StringWriter();
        var e = new StringWriter();
        return (Program.Run(["test", .. args], o, e), o.ToString(), e.ToString());
    }

    private string Folder(string name)
    {
        var folder = Path.Combine(_temp, name);
        Directory.CreateDirectory(folder);
        return folder;
    }

    private string CopyOf(string source, string name)
    {
        var target = Path.Combine(_temp, name);
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var to = Path.Combine(target, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(to)!);
            File.Copy(file, to);
        }

        return target;
    }

    // The flow files start with a byte order mark; keep it.
    private static void Edit(string folder, string pattern, Func<string, string> change)
    {
        var file = Directory.GetFiles(folder, pattern, SearchOption.AllDirectories).Single();
        File.WriteAllText(file, change(File.ReadAllText(file)), new UTF8Encoding(true));
    }

    private static void Write(string folder, string name, string content) => File.WriteAllText(Path.Combine(folder, name), content, new UTF8Encoding(false));

    private const string Assertions = """[{"kind":"runStatus","equals":"Succeeded"}]""";

    // Passing

    [Fact]
    public void The_seed_tests_pass()
    {
        var (code, output, err) = Run(Seed, "--tests", SeedTests);

        Assert.Equal(0, code);
        Assert.Equal("", err);
        Assert.Contains("2 tests: 2 passed, 0 failed, 0 unmocked, 0 unsupported", output);
    }

    [Fact]
    public void The_alm_makers_tests_pass_and_match_the_text_golden()
    {
        var (code, output, err) = Run(Alm, "--tests", AlmTests);

        Assert.Equal(0, code);
        Assert.Equal("", err);
        Golden.Verify(output, "test-fixtures.txt");
    }

    [Fact]
    public void Json_matches_golden() => Golden.Verify(Run(Alm, "--tests", AlmTests, "--format", "json").Out, "test-fixtures.json");

    [Fact]
    public void Markdown_matches_golden() => Golden.Verify(Run(Alm, "--tests", AlmTests, "--format", "markdown").Out, "test-fixtures.md");

    [Fact]
    public void Out_writes_the_report_to_a_file_and_keeps_the_exit_code()
    {
        var file = Path.Combine(_temp, "report.json");

        var (code, output, _) = Run(Seed, "--tests", SeedTests, "--format", "json", "--out", file);

        Assert.Equal(0, code);
        Assert.Equal("", output);
        Assert.Contains("\"schemaVersion\": 1", File.ReadAllText(file));
    }

    [Fact]
    public void The_tests_folder_defaults_to_the_path()
    {
        var folder = CopyOf(Seed, "seed");
        File.Copy(Path.Combine(SeedTests, "stamp-work-item-status-default.flowtest.json"), Path.Combine(folder, "t.flowtest.json"));

        var (code, output, _) = Run(folder);

        Assert.Equal(0, code);
        Assert.Contains("1 test: 1 passed", output);
        Assert.Contains("PASS t.flowtest.json: Stamp work item status", output);
    }

    [Fact]
    public void A_zip_path_needs_tests_to_name_the_folder()
    {
        var zip = Path.Combine(_temp, "seed.zip");
        SolutionZip.Create(Seed, zip);

        var (code, _, err) = Run(zip);
        var (zipCode, zipOutput, _) = Run(zip, "--tests", SeedTests);

        Assert.Equal(2, code);
        Assert.StartsWith("error: ", err);
        Assert.Contains("--tests", err);
        Assert.Equal(0, zipCode);
        Assert.Contains("2 passed", zipOutput);
    }

    // Failing

    [Fact]
    public void A_changed_condition_fails_the_cancelled_request_test_and_the_report_names_the_assertion()
    {
        var alm = CopyOf(Alm, "alm");
        Edit(alm, "BuildRequestCancellation-*.json", text => text.Replace("809060013", "809060099", StringComparison.Ordinal));

        var (code, output, err) = Run(alm, "--tests", AlmTests);

        Assert.Equal(1, code);
        Assert.Equal("", err);
        Assert.Contains("FAIL build-request-cancellation-cancelled.flowtest.json: BuildRequestCancellation", output);
        Assert.Contains("  assertion 1, inputs of List_records at [\"parameters\",\"$filter\"]: ", output);
        Assert.Contains("PASS build-request-cancellation-other-status.flowtest.json", output);
        Assert.Contains("4 tests: 3 passed, 1 failed, 0 unmocked, 0 unsupported", output);
    }

    [Fact]
    public void A_changed_expectation_a_missing_mock_and_an_unsupported_function_each_exit_one_and_match_the_goldens()
    {
        var alm = CopyOf(Alm, "alm");
        Edit(alm, "BuildRequestCancellation-*.json", text => text.Replace("createdon desc", "@addHours('2026-01-01T00:00:00Z', 1)", StringComparison.Ordinal));
        var tests = Folder("failures");
        Write(tests, "changed-expectation.flowtest.json", """
            {
              "flow": "BuildRequestCancellation",
              "trigger": { "body": { "cat_deploymentstatus": 809060001, "cat_projectid": "00000000-0000-0000-0000-000000000001" } },
              "assertions": [
                { "kind": "runStatus", "equals": "Failed" },
                { "kind": "actionStatus", "action": "Terminate", "equals": "Succeeded" },
                { "kind": "actionStatus", "action": "List_records", "equals": "Succeeded" }
              ]
            }
            """);
        Write(tests, "missing-mock.flowtest.json", """
            {
              "flow": "ReadEnvironmentVariable",
              "trigger": { "headers": { "X-MS-APIM-Tokens": "eyIkQ29ubmVjdGlvbktleSI6ImZha2UifQ==" }, "body": { "text": "cat_OrgName" } },
              "guid": "00000000-0000-0000-0000-000000000009",
              "assertions": [ { "kind": "runStatus", "equals": "Succeeded" } ]
            }
            """);
        Write(tests, "unsupported-function.flowtest.json", """
            {
              "flow": "BuildRequestCancellation",
              "trigger": { "body": { "cat_deploymentstatus": 809060013, "cat_projectid": "00000000-0000-0000-0000-000000000001" } },
              "mocks": { "List_records": { "status": "Succeeded", "outputs": { "body": { "value": [] } } } },
              "assertions": [ { "kind": "runStatus", "equals": "Succeeded" } ]
            }
            """);

        var (code, output, err) = Run(alm, "--tests", tests);
        var json = Run(alm, "--tests", tests, "--format", "json");

        Assert.Equal(1, code);
        Assert.Equal(1, json.Code);
        Assert.Equal("", err);
        Assert.Contains("FAIL changed-expectation", output);
        Assert.Contains("UNMOCKED missing-mock", output);
        Assert.Contains("UNSUPPORTED unsupported-function", output);
        Golden.Verify(output, "test-failures.txt");
        Golden.Verify(json.Out, "test-failures.json");
    }

    [Fact]
    public void Two_flows_with_one_name_in_a_solution_fail_the_test_as_ambiguous()
    {
        var seed = CopyOf(Seed, "seed");
        var original = Directory.GetFiles(seed, "Stampworkitemstatus-*.json", SearchOption.AllDirectories).Single();
        var data = original + ".data.xml";
        File.WriteAllText(
            Path.Combine(seed, "Workflows", "Copy-00000000-0000-0000-0000-0000000000AA.json.data.xml"),
            File.ReadAllText(data)
                .Replace(Path.GetFileName(original), "Copy-00000000-0000-0000-0000-0000000000AA.json", StringComparison.Ordinal)
                .Replace("4c8dd007-dfcb-f111-a05c-0022480697a8", "00000000-0000-0000-0000-0000000000aa", StringComparison.OrdinalIgnoreCase),
            new UTF8Encoding(true));
        File.Copy(original, Path.Combine(seed, "Workflows", "Copy-00000000-0000-0000-0000-0000000000AA.json"));

        var (code, output, _) = Run(seed, "--tests", SeedTests);

        Assert.Equal(1, code);
        Assert.Contains("FAIL stamp-work-item-status-default.flowtest.json: Stamp work item status", output);
        Assert.Contains("is ambiguous: 2 flows match", output);
    }

    // Usage errors

    [Fact]
    public void A_path_that_does_not_exist_exits_two()
    {
        var (code, output, err) = Run(Path.Combine(_temp, "nope"), "--tests", SeedTests);

        Assert.Equal(2, code);
        Assert.Equal("", output);
        Assert.StartsWith("error: path not found", err);
    }

    [Fact]
    public void A_tests_folder_that_does_not_exist_exits_two()
    {
        var (code, _, err) = Run(Seed, "--tests", Path.Combine(_temp, "nope"));

        Assert.Equal(2, code);
        Assert.StartsWith("error: tests folder not found", err);
    }

    [Fact]
    public void A_tests_location_that_is_a_file_exits_two()
    {
        var file = Path.Combine(SeedTests, "stamp-work-item-status-default.flowtest.json");

        var (code, _, err) = Run(Seed, "--tests", file);

        Assert.Equal(2, code);
        Assert.StartsWith("error: ", err);
        Assert.Contains("is a file", err);
    }

    [Fact]
    public void A_tests_folder_with_no_test_files_exits_two()
    {
        var empty = Folder("empty");
        Write(empty, "notes.json", "{}");

        var (code, _, err) = Run(Seed, "--tests", empty);

        Assert.Equal(2, code);
        Assert.StartsWith("error: no *.flowtest.json files", err);
    }

    [Fact]
    public void A_path_with_no_solution_exits_two()
    {
        var none = Folder("none");
        Write(none, "t.flowtest.json", "{}");

        var (code, _, err) = Run(none);

        Assert.Equal(2, code);
        Assert.StartsWith("error: no solutions found", err);
    }

    [Fact]
    public void A_bad_format_exits_two()
    {
        Assert.Equal(2, Run(Seed, "--tests", SeedTests, "--format", "sarif").Code);
    }

    // Unreadable input

    [Fact]
    public void A_test_file_that_is_not_json_exits_three_with_only_the_error()
    {
        var tests = Folder("bad");
        Write(tests, "a.flowtest.json", "{" + """ "flow": "Stamp work item status", "assertions": """ + Assertions + "}");
        Write(tests, "b.flowtest.json", "not json");

        var (code, output, err) = Run(Seed, "--tests", tests);

        Assert.Equal(3, code);
        Assert.Equal("", output);
        Assert.StartsWith($"error: {Path.Combine(tests, "b.flowtest.json")}:", err);
    }

    [Fact]
    public void A_test_file_that_breaks_a_rule_exits_three_before_any_test_runs()
    {
        var tests = Folder("rule");
        Write(tests, "a.flowtest.json", """{ "flow": "Stamp work item status", "assertions": [ { "kind": "runStatus", "equals": "Succeeded", "path": ["a"] } ] }""");

        var (code, output, err) = Run(Seed, "--tests", tests);

        Assert.Equal(3, code);
        Assert.Equal("", output);
        Assert.StartsWith($"error: {Path.Combine(tests, "a.flowtest.json")}:", err);
    }

    [Fact]
    public void A_solution_that_cannot_be_read_exits_three_and_blocks_every_test()
    {
        var solutions = Folder("solutions");
        var bad = Path.Combine(solutions, "bad", "Other");
        Directory.CreateDirectory(bad);
        File.WriteAllText(Path.Combine(bad, "Solution.xml"), "<not xml");
        CopyOf(Seed, Path.Combine("solutions", "seed"));

        var (code, output, err) = Run(solutions, "--tests", SeedTests);

        Assert.Equal(3, code);
        Assert.Equal("", output);
        Assert.StartsWith("error: ", err);
    }
}