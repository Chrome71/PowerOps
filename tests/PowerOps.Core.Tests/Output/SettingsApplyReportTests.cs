using System.Text.Json;

using PowerOps.Core.Output;
using PowerOps.Core.Settings;

namespace PowerOps.Core.Tests.Output;

public class SettingsApplyReportTests
{
    private static readonly string[] Import = ["solution", "import", "--environment", "<environment id>", "--path", "my seed.zip", "--settings-file", "s.json"];

    private static ApplyResult DryRun(params ApplyFlow[] flows) => new(
        false, "Contoso Dev", false, Import,
        [new("validate", "ok"), new("target", "ok"), new("plan", "ok"), new("import", "planned"), new("flows", "planned")],
        [], null, null, flows);

    private static ApplyResult Applied(params ApplyFlow[] flows) => DryRun(flows) with
    {
        Applied = true,
        Steps = [new("validate", "ok"), new("target", "ok"), new("plan", "ok"), new("import", "ok"), new("flows", "ok")],
    };

    private static string Text(ApplyResult r, bool header = true)
    {
        var w = new StringWriter();
        SettingsApplyReport.WriteText(r, w, header);
        return w.ToString();
    }

    [Fact]
    public void Dry_run_text_shows_the_target_the_command_and_the_predictions()
    {
        var text = Text(DryRun(new("A", "on", false, null, "on", null), new("B", "on", true, "off", "off", null)));

        Assert.Equal(
            "environment: Contoso Dev\n" +
            "managed: false\n" +
            "command: pac solution import --environment \"<environment id>\" --path \"my seed.zip\" --settings-file s.json\n" +
            "validate: ok\ntarget: ok\nplan: ok\nimport: planned\nflows: planned\n" +
            "flow A: new, expected on\n" +
            "flow B: exists, off\n" +
            "dry run: nothing imported\n",
            text);
    }

    [Fact]
    public void The_header_line_can_be_left_out_when_it_was_printed_before_the_import()
    {
        Assert.Equal("environment: Contoso Dev\n", SettingsApplyReport.Header("Contoso Dev"));
        Assert.StartsWith("managed: false\n", Text(Applied(), header: false));
    }

    [Theory]
    [InlineData(new[] { "on", "on" }, "imported, all flows on")]
    [InlineData(new[] { "on", "off" }, "imported, 1 of 2 flows not on")]
    [InlineData(new[] { "suspended", "missing" }, "imported, 2 of 2 flows not on")]
    [InlineData(new string[0], "imported, no cloud flows")]
    public void Applied_text_shows_each_flow_after_the_import(string[] after, string summary)
    {
        var text = Text(Applied(after.Select((a, i) => new ApplyFlow($"F{i}", "on", false, null, "on", a)).ToArray()));

        Assert.EndsWith(string.Concat(after.Select((a, i) => $"flow F{i}: {a}\n")) + summary + "\n", text);
    }

    [Fact]
    public void Problems_use_the_validate_lines_and_a_message_wins_the_summary()
    {
        var problems = DryRun() with { Problems = [new("missing", "connectionReference", "pops_X", "in the solution but not in the settings file")] };
        var failed = DryRun() with { Environment = null, Message = "pac org who failed" };

        Assert.EndsWith("missing connectionReference pops_X: in the solution but not in the settings file\nvalidate: ok\ntarget: ok\nplan: ok\nimport: planned\nflows: planned\n1 problem\n", Text(problems));
        Assert.StartsWith("managed: false\n", Text(failed));
        Assert.EndsWith("pac org who failed\n", Text(failed));
    }

    [Fact]
    public void Json_starts_with_the_schema_version_and_holds_every_field()
    {
        var w = new StringWriter();
        SettingsApplyReport.WriteJson(Applied(new ApplyFlow("A", "on", true, "on", "on", "off")) with { PacExitCode = 1, Message = "m" }, w);
        var json = w.ToString();

        Assert.StartsWith("{\n  \"schemaVersion\": 1,\n  \"applied\": true,\n", json);
        Assert.EndsWith("}\n", json);
        var root = JsonDocument.Parse(json).RootElement;
        Assert.Equal(
            ["schemaVersion", "applied", "environment", "managed", "importArguments", "steps", "problems", "pacExitCode", "message", "flows"],
            root.EnumerateObject().Select(p => p.Name));
        Assert.Equal("<environment id>", root.GetProperty("importArguments")[3].GetString());
        Assert.Equal(1, root.GetProperty("pacExitCode").GetInt32());
        var flow = root.GetProperty("flows")[0];
        Assert.Equal(["name", "exportState", "exists", "stateBefore", "expectedState", "stateAfter"], flow.EnumerateObject().Select(p => p.Name));
        Assert.Equal("off", flow.GetProperty("stateAfter").GetString());
    }
}