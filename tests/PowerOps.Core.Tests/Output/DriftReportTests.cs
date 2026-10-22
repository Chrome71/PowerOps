using PowerOps.Core.Drift;
using PowerOps.Core.Output;

namespace PowerOps.Core.Tests.Output;

public class DriftReportTests
{
    private const string Hash = "deadbeefcafe0123456789";

    private static Snapshot Snap(string name, string version, string? valueHash = null, bool managed = true, bool layersRead = true) =>
        new(name, "demo", true, version, managed, layersRead, [], [new("demo_v", valueHash)], [], []);

    private static (IReadOnlyList<Snapshot> Envs, string[] Sources, IReadOnlyList<DriftItem> Items) Case(string testName = "Test")
    {
        Snapshot[] envs = [Snap("Dev", "1.0.0.0", "h1"), Snap(testName, "2.0.0.0", Hash, layersRead: false)];
        return (envs, ["live", "snapshot"], DriftCompare.Compare(envs));
    }

    private static string Render(Action<string, IReadOnlyList<Snapshot>, IReadOnlyList<string>, IReadOnlyList<DriftItem>, TextWriter> write, string testName = "Test")
    {
        var (envs, sources, items) = Case(testName);
        var w = new StringWriter();
        write("demo", envs, sources, items, w);
        return w.ToString();
    }

    [Fact]
    public void Text_lists_environments_items_and_a_summary()
    {
        Assert.Equal(
            "1 Dev: 1.0.0.0, managed\n" +
            "2 Test: 2.0.0.0, managed, layers not read\n" +
            "2 Test: version demo: 1.0.0.0 -> 2.0.0.0\n" +
            "2 Test: layers-not-read demo (info)\n" +
            "2 Test: variable-value demo_v\n" +
            "2 drift items\n",
            Render(DriftReport.WriteText));
    }

    [Fact]
    public void Text_says_no_drift_and_not_installed()
    {
        Snapshot[] envs = [Snap("Dev", "1"), new Snapshot("Test", "demo", false, null, null, true, [], [], [], [])];
        var w = new StringWriter();

        DriftReport.WriteText("demo", envs, ["live", "live"], [], w);

        Assert.Equal("1 Dev: 1, managed\n2 Test: not installed\nno drift\n", w.ToString());
    }

    [Fact]
    public void Json_has_a_stable_shape_with_nulls_written()
    {
        Assert.Equal(
            """
            {
              "schemaVersion": 1,
              "solution": "demo",
              "drift": true,
              "environments": [
                {
                  "position": 1,
                  "name": "Dev",
                  "source": "live",
                  "installed": true,
                  "version": "1.0.0.0",
                  "managed": true,
                  "layersRead": true
                },
                {
                  "position": 2,
                  "name": "Test",
                  "source": "snapshot",
                  "installed": true,
                  "version": "2.0.0.0",
                  "managed": true,
                  "layersRead": false
                }
              ],
              "items": [
                {
                  "position": 2,
                  "environment": "Test",
                  "kind": "version",
                  "subject": "demo",
                  "drift": true,
                  "expected": "1.0.0.0",
                  "actual": "2.0.0.0"
                },
                {
                  "position": 2,
                  "environment": "Test",
                  "kind": "layers-not-read",
                  "subject": "demo",
                  "drift": false,
                  "expected": null,
                  "actual": null
                },
                {
                  "position": 2,
                  "environment": "Test",
                  "kind": "variable-value",
                  "subject": "demo_v",
                  "drift": true,
                  "expected": null,
                  "actual": null
                }
              ]
            }

            """.ReplaceLineEndings("\n"),
            Render(DriftReport.WriteJson));
    }

    [Fact]
    public void Markdown_has_an_environments_table_and_an_items_table()
    {
        Assert.Equal(
            "## PowerOps drift\n\n**demo: 2 drift items**\n\n" +
            "| Position | Environment | Source | Version | Managed | Layers read |\n|---|---|---|---|---|---|\n" +
            "| 1 | Dev | live | 1.0.0.0 | managed | yes |\n" +
            "| 2 | Test | snapshot | 2.0.0.0 | managed | no |\n\n" +
            "| Position | Environment | Kind | Subject | Drift | Expected | Actual |\n|---|---|---|---|---|---|---|\n" +
            "| 2 | Test | version | demo | yes | 1.0.0.0 | 2.0.0.0 |\n" +
            "| 2 | Test | layers-not-read | demo | no |  |  |\n" +
            "| 2 | Test | variable-value | demo\\_v | yes |  |  |\n",
            Render(DriftReport.WriteMarkdown));
    }

    [Fact]
    public void A_name_with_a_pipe_and_a_line_break_cannot_break_text_or_markdown()
    {
        var text = Render(DriftReport.WriteText, "Te|st\nx");
        var md = Render(DriftReport.WriteMarkdown, "Te|st\nx");

        Assert.Contains("2 Te|st x: version", text);
        Assert.Equal(6, text.Count(c => c == '\n'));
        Assert.Contains("| Te\\|st x |", md);
        Assert.DoesNotContain("Te|st", md);
        Assert.Equal(14, md.Count(c => c == '\n'));
    }

    [Fact]
    public void No_value_or_hash_appears_in_any_format()
    {
        foreach (var write in new[] { DriftReport.WriteText, DriftReport.WriteJson, DriftReport.WriteMarkdown })
        {
            var output = Render(write);
            Assert.Contains("variable-value", output);
            Assert.DoesNotContain(Hash, output);
            Assert.DoesNotContain("h1", output);
        }
    }
}