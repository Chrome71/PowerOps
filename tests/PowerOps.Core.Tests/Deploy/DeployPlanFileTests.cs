using System.Text.Json.Nodes;

using PowerOps.Core.Deploy;
using PowerOps.Core.Output;

namespace PowerOps.Core.Tests.Deploy;

public class DeployPlanFileTests
{
    private const string Tool = "1.2.3";
    private const string H1 = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
    private const string H2 = "fedcba9876543210fedcba9876543210fedcba9876543210fedcba9876543210";

    private static readonly PlanInputs Inputs = new(
        [
            new ZipInput("a.zip", H1, "a.settings.json", H2, H1),
            new ZipInput("b.zip", H2, null, null, H2),
        ],
        [new DependencyInput("core_dep", "9.0.0.1"), new DependencyInput("other", null)]);

    private static string File(string tool = Tool, bool blocked = false)
    {
        var plan = new DeployPlanResult("Contoso Test", H1, blocked, Inputs, [], []);
        var w = new StringWriter();
        DeployPlanReport.WriteJson(plan, tool, w);
        return w.ToString();
    }

    // path: "inputs.zips.0.zip"; a null value removes the property.
    private static string Edit(string path, string? rawJson, string? text = null)
    {
        var root = JsonNode.Parse(text ?? File())!;
        var parts = path.Split('.');
        var node = root;
        foreach (var p in parts[..^1])
        {
            node = int.TryParse(p, out var i) ? node![i] : node![p];
        }

        if (int.TryParse(parts[^1], out var index))
        {
            node!.AsArray()[index] = JsonNode.Parse(rawJson!);
            return root.ToJsonString();
        }

        var obj = node!.AsObject();
        if (rawJson is null)
        {
            obj.Remove(parts[^1]);
        }
        else
        {
            obj[parts[^1]] = JsonNode.Parse(rawJson);
        }

        return root.ToJsonString();
    }

    private static ReviewedPlan Ok(string text)
    {
        var read = DeployPlanFile.Read(text, Tool);
        Assert.Null(read.Refusal);
        return read.Plan!;
    }

    [Fact]
    public void A_file_written_by_the_plan_report_reads_back()
    {
        var plan = Ok(File());

        Assert.Equal((JsonOutput.SchemaVersion, Tool, H1, "Contoso Test", false), (plan.SchemaVersion, plan.Tool, plan.Hash, plan.Target, plan.Blocked));
        Assert.Equal(Inputs.Zips, plan.Inputs.Zips);
        Assert.Equal(Inputs.Dependencies, plan.Inputs.Dependencies);
        Assert.True(Ok(File(blocked: true)).Blocked);
    }

    [Fact]
    public void Unused_properties_are_ignored()
    {
        Assert.Equal(H1, Ok(Edit("extra", "1")).Hash);
    }

    [Theory]
    [InlineData("schemaVersion", null)]
    [InlineData("schemaVersion", "\"1\"")]
    [InlineData("schemaVersion", "1.5")]
    [InlineData("tool", null)]
    [InlineData("tool", "1")]
    public void A_missing_or_mistyped_schemaVersion_or_tool_is_unreadable(string field, string? value)
    {
        var e = Assert.Throws<InvalidDataException>(() => DeployPlanFile.Read(Edit(field, value), Tool));

        Assert.Contains(field, e.Message);
    }

    [Theory]
    [InlineData("zip")]
    [InlineData("settings")]
    [InlineData("dependency")]
    public void A_duplicate_name_is_unreadable_ignoring_case_and_the_message_names_the_field_but_never_the_value(string kind)
    {
        var text = kind switch
        {
            "zip" => Edit("inputs.zips.1.zip", "\"SENTINEL.ZIP\"", Edit("inputs.zips.0.zip", "\"sentinel.zip\"")),
            "settings" => Edit("inputs.zips.1.settingsSha256", "\"" + H1 + "\"", Edit("inputs.zips.1.settings", "\"SENTINEL.settings.json\"", Edit("inputs.zips.0.settings", "\"sentinel.SETTINGS.json\""))),
            _ => Edit("inputs.dependencies.1.name", "\"SENTINEL_DEP\"", Edit("inputs.dependencies.0.name", "\"sentinel_dep\"")),
        };

        var e = Assert.Throws<InvalidDataException>(() => DeployPlanFile.Read(text, Tool));

        Assert.Contains(kind switch { "zip" => "inputs.zips", "settings" => "settings", _ => "inputs.dependencies" }, e.Message);
        Assert.Contains("twice", e.Message);
        Assert.DoesNotContain("SENTINEL", e.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("hash", null)]
    [InlineData("hash", "5")]
    [InlineData("hash", "\"SENTINEL\"")]
    [InlineData("hash", "\"0123456789ABCDEF0123456789abcdef0123456789abcdef0123456789abcdef\"")]
    [InlineData("target", null)]
    [InlineData("target", "\"\"")]
    [InlineData("target", "3")]
    [InlineData("blocked", null)]
    [InlineData("blocked", "\"false\"")]
    [InlineData("inputs", null)]
    [InlineData("inputs", "[]")]
    [InlineData("inputs.zips", null)]
    [InlineData("inputs.zips", "{}")]
    [InlineData("inputs.zips.0", "5")]
    [InlineData("inputs.zips.0.zip", null)]
    [InlineData("inputs.zips.0.zip", "7")]
    [InlineData("inputs.zips.0.zip", "\"SENTINEL\\n::error::x\"")]
    [InlineData("inputs.zips.0.zip", "\"SENTINEL/x.zip\"")]
    [InlineData("inputs.zips.0.zip", "\"\"")]
    [InlineData("inputs.zips.0.zipSha256", null)]
    [InlineData("inputs.zips.0.zipSha256", "\"SENTINEL\"")]
    [InlineData("inputs.zips.0.settings", "7")]
    [InlineData("inputs.zips.0.settings", "\"SENTINEL\\n::error::x\"")]
    [InlineData("inputs.zips.0.settings", null)]
    [InlineData("inputs.zips.0.settingsSha256", "null")]
    [InlineData("inputs.zips.0.settingsSha256", "\"SENTINEL\"")]
    [InlineData("inputs.zips.1.settingsSha256", "\"" + H1 + "\"")]
    [InlineData("inputs.zips.0.snapshotSha256", null)]
    [InlineData("inputs.zips.0.snapshotSha256", "\"SENTINEL\"")]
    [InlineData("inputs.dependencies", null)]
    [InlineData("inputs.dependencies", "5")]
    [InlineData("inputs.dependencies.0.name", null)]
    [InlineData("inputs.dependencies.0.name", "\"SENTINEL-dash\"")]
    [InlineData("inputs.dependencies.0.name", "\"SENTINEL\\n::error::x\"")]
    [InlineData("inputs.dependencies.0.name", "4")]
    [InlineData("inputs.dependencies.0.version", null)]
    [InlineData("inputs.dependencies.0.version", "4")]
    public void A_bad_field_is_unreadable_and_the_message_names_the_field_but_never_the_value(string path, string? value)
    {
        var e = Assert.Throws<InvalidDataException>(() => DeployPlanFile.Read(Edit(path, value), Tool));

        var parts = path.Split('.');
        Assert.Contains(int.TryParse(parts[^1], out _) ? parts[^2] : parts[^1], e.Message);
        Assert.DoesNotContain("SENTINEL", e.Message);
        Assert.DoesNotContain("::error", e.Message);
        Assert.DoesNotContain('\n', e.Message);
    }

    [Theory]
    [InlineData("{\"schemaVersion\": 1,")]
    [InlineData("")]
    [InlineData("SENTINEL")]
    [InlineData("[1]")]
    [InlineData("\"SENTINEL\"")]
    public void Text_that_is_not_a_JSON_object_is_unreadable_and_never_quoted(string text)
    {
        var e = Assert.Throws<InvalidDataException>(() => DeployPlanFile.Read(text, Tool));

        Assert.DoesNotContain("SENTINEL", e.Message);
    }

    [Fact]
    public void Bad_JSON_names_the_line_and_position_only()
    {
        var e = Assert.Throws<InvalidDataException>(() => DeployPlanFile.Read("{\n  \"a\": SENTINEL\n}", Tool));

        Assert.Equal("the plan file is not JSON (line 1, position 7)", e.Message);
    }

    [Theory]
    [InlineData("schemaVersion", "2")]
    [InlineData("tool", "\"0.9.0\"")]
    public void Another_schemaVersion_or_tool_is_refused_even_when_the_other_fields_are_broken(string field, string value)
    {
        var broken = Edit("inputs", "5", Edit("hash", "\"x\"", Edit("blocked", null, Edit(field, value))));

        var read = DeployPlanFile.Read(broken, Tool);

        Assert.Null(read.Plan);
        Assert.Equal(
            $"the plan was made by powerops {(field == "tool" ? "0.9.0" : Tool)}; this is {Tool}; plan again with this version",
            read.Refusal);
    }

    [Fact]
    public void A_refusal_cleans_and_cuts_the_tool_text_from_the_file()
    {
        var read = DeployPlanFile.Read(Edit("tool", "\"x\\n::error::" + new string('y', 500) + "\""), Tool);

        Assert.DoesNotContain("\n", read.Refusal);
        Assert.True(read.Refusal!.Length < 200);
    }

    [Fact]
    public void A_matching_tool_with_a_broken_field_is_not_a_refusal()
    {
        Assert.Throws<InvalidDataException>(() => DeployPlanFile.Read(Edit("hash", "1"), Tool));
    }
}