using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using PowerOps.Core.Deploy;
using PowerOps.Core.Output;

namespace PowerOps.Core.Tests.Output;

public class DeployPlanReportTests
{
    private const string Hash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    private static DeployPlanResult Result(string solution = "Sol", PlanItem[]? items = null, PlannedFlow[]? flows = null, InstalledState? installed = null, int solutions = 1)
    {
        var planned = Enumerable.Range(1, solutions)
            .Select(i => new PlannedSolution(i, i == 1 ? solution : $"{solution}{i}", $"{solution}.zip", null, "2.0.0.0", true, PlanAction.Upgrade, installed, [], i == 1 ? flows ?? [] : []))
            .ToList();
        var all = items ?? [];
        return new DeployPlanResult("Contoso Test", Hash, all.Any(i => i.Severity == PlanSeverity.Block), new PlanInputs([], []), planned, all);
    }

    private static PlanItem Item(string subject, string message = "m", string severity = PlanSeverity.Warning, string solution = "Sol") =>
        new(solution, PlanKind.Deletes, severity, subject, message);

    private static string Text(DeployPlanResult r)
    {
        var w = new StringWriter();
        DeployPlanReport.WriteText(r, w);
        return w.ToString();
    }

    private static string Markdown(DeployPlanResult r, int? max = null)
    {
        var w = new StringWriter();
        if (max is { } m)
        {
            DeployPlanReport.WriteMarkdown(r, w, m);
        }
        else
        {
            DeployPlanReport.WriteMarkdown(r, w);
        }

        return w.ToString();
    }

    [Fact]
    public void Text_has_the_target_the_hash_one_line_per_solution_its_flows_and_items_and_a_summary()
    {
        var r = Result(
            items: [Item("flow x", "gone", PlanSeverity.Block)],
            flows: [new PlannedFlow("Notify", "on", true, "off", "off"), new PlannedFlow("New one", "on", false, null, "on")],
            installed: new InstalledState("1.0.0.0", true, true));

        Assert.Equal(
            $"Target: Contoso Test\nPlan hash: {Hash}\n" +
            "1 Sol (Sol.zip): upgrade 1.0.0.0 -> 2.0.0.0, managed\n" +
            "  flow Notify: off -> off\n  flow New one: new -> on\n" +
            "  block deletes flow x: gone\n" +
            "plan blocked: 1 blocking item\n",
            Text(r));
    }

    [Fact]
    public void Text_says_not_installed_names_the_settings_file_and_ends_with_plan_ok()
    {
        var r = Result() with { Solutions = [new PlannedSolution(1, "Sol", "Sol.zip", "Sol.settings.json", "1.0.0.0", false, PlanAction.Install, null, [], [])] };

        Assert.Equal($"Target: Contoso Test\nPlan hash: {Hash}\n1 Sol (Sol.zip): install not installed -> 1.0.0.0, unmanaged, settings Sol.settings.json\nplan ok\n", Text(r));
    }

    [Fact]
    public void Text_cleans_a_name_with_a_line_break_and_a_workflow_command()
    {
        var r = Result("Evil\n::error::boom", [Item("x\r\n::error::y", "a\n::error::b")]);

        var text = Text(r);

        Assert.DoesNotContain('\r', text);
        Assert.All(text.Split('\n', StringSplitOptions.RemoveEmptyEntries), l => Assert.DoesNotMatch(@"^\s*::", l));
        Assert.Contains("Evil ::error::boom", text);
    }

    [Fact]
    public void Markdown_escapes_a_pipe_and_turns_a_line_break_into_a_space()
    {
        var r = Result("A|B\nC", [Item("s|1\n::error::x")]);
        r = r with { Items = [r.Items[0] with { Solution = "A|B\nC" }] };

        var text = Markdown(r);

        Assert.Contains("A\\|B C", text);
        Assert.Contains("s\\|1 ::error::x", text);
        Assert.All(text.Split('\n').Where(l => l.StartsWith("| ", StringComparison.Ordinal)), l => Assert.EndsWith(" |", l));
        Assert.All(text.Split('\n', StringSplitOptions.RemoveEmptyEntries), l => Assert.DoesNotMatch(@"^::", l));
    }

    [Fact]
    public void Markdown_has_a_heading_the_target_the_hash_and_the_three_tables()
    {
        var r = Result(items: [Item("s")], flows: [new PlannedFlow("Notify", "on", true, "on", "on")]);

        var text = Markdown(r);

        Assert.StartsWith($"## PowerOps deploy plan\n\n**plan ok**\n\nTarget: Contoso Test\n\nPlan hash: `{Hash}`\n", text);
        Assert.Contains("| Order | Solution |", text);
        Assert.Contains("| Solution | Severity | Kind | Subject | Message |", text);
        Assert.Contains("| Sol | Notify | on | on |", text);
        Assert.DoesNotContain("not shown", text);
    }

    [Fact]
    public void Markdown_of_a_huge_plan_cuts_at_a_whole_row_and_says_how_many_rows_are_left_out()
    {
        var r = Result(items: [.. Enumerable.Range(0, 2000).Select(i => Item($"subject {i:D5}"))]);

        var text = Markdown(r);

        Assert.True(text.Length <= DiffReport.MarkdownLimit, $"{text.Length} chars");
        var shown = Regex.Matches(text, @"^\| Sol \| warning ", RegexOptions.Multiline).Count;
        var omitted = int.Parse(Regex.Match(text, @"_(\d+) more rows not shown").Groups[1].Value);
        Assert.True(shown > 0 && omitted > 0);
        Assert.Equal(2001, 1 + shown + omitted); // the solution row, the item rows shown and the rows left out
        Assert.EndsWith("full plan._\n", text);
    }

    [Fact]
    public void Markdown_cuts_at_a_whole_row_and_leaves_out_the_tables_after_it()
    {
        var r = Result(items: [.. Enumerable.Range(0, 50).Select(i => Item($"subject {i:D5}"))], flows: [new PlannedFlow("Notify", "on", true, "on", "on")]);

        var text = Markdown(r, 1500);

        Assert.True(text.Length <= 1500);
        Assert.All(text.Split('\n').Where(l => l.StartsWith("| ", StringComparison.Ordinal)), l => Assert.EndsWith(" |", l));
        Assert.DoesNotContain("| Solution | Flow |", text);
        Assert.Matches(@"_\d+ more rows not shown", text);
    }

    [Fact]
    public void Json_has_the_properties_in_order_with_nulls_a_lf_ending_and_no_id_or_url()
    {
        var w = new StringWriter();
        DeployPlanReport.WriteJson(Result(), "9.9.9", w);
        var json = w.ToString();

        Assert.EndsWith("}\n", json);
        Assert.DoesNotContain('\r', json);
        var node = JsonNode.Parse(json)!.AsObject();
        Assert.Equal(["schemaVersion", "tool", "target", "hash", "blocked", "inputs", "solutions", "items"], node.Select(p => p.Key));
        Assert.Equal(["zips", "dependencies"], node["inputs"]!.AsObject().Select(p => p.Key));
        var s = node["solutions"]![0]!.AsObject();
        Assert.Equal(["order", "solution", "zip", "settings", "version", "managed", "action", "installed", "dependsOn", "flows"], s.Select(p => p.Key));
        Assert.Null(s["settings"]);
        Assert.Contains("\"installed\": null", json);
        Assert.Equal("9.9.9", (string?)node["tool"]);
    }

    [Fact]
    public void Json_item_and_flow_properties_are_in_order()
    {
        var w = new StringWriter();
        DeployPlanReport.WriteJson(Result(items: [Item("s")], flows: [new PlannedFlow("Notify", "on", true, "off", "off")], installed: new InstalledState("1.0.0.0", true, false)), "t", w);

        var node = JsonNode.Parse(w.ToString())!.AsObject();

        Assert.Equal(["solution", "kind", "severity", "subject", "message"], node["items"]![0]!.AsObject().Select(p => p.Key));
        Assert.Equal(["name", "exportState", "exists", "stateBefore", "expectedState"], node["solutions"]![0]!["flows"]![0]!.AsObject().Select(p => p.Key));
        Assert.Equal(["version", "managed", "layersRead"], node["solutions"]![0]!["installed"]!.AsObject().Select(p => p.Key));
    }
}