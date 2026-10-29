using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Xml;

using PowerOps.Core.Diff;
using PowerOps.Core.Flows;
using PowerOps.Core.Model;
using PowerOps.Core.Readers;
using PowerOps.Core.Rules;
using PowerOps.Core.Tests.Rules;

namespace PowerOps.Core.Tests.Readers;

/// <summary>M25: hostile input is refused (or cheap) where analyze and diff read it. The exit-code theory is in PowerOps.Cli.Tests.</summary>
public class InputLimitsTests(ITestOutputHelper log)
{
    // R-3's probe: `a@`, then `.a` 40,000 times, then `@`.
    private static string Adversarial(int repeats = 40_000) => "a@" + string.Concat(Enumerable.Repeat(".a", repeats)) + "@";

    private static string Deep(string name, int depth) => string.Concat(Enumerable.Repeat($"<{name}>", depth)) + string.Concat(Enumerable.Repeat($"</{name}>", depth));

    private static int CountNodes(string xml)
    {
        using var reader = XmlReader.Create(new StringReader(xml));
        var n = 0;
        while (reader.Read())
        {
            n++;
        }

        return n;
    }

    private static string Flat(int elements) => "<ImportExportXml>" + string.Concat(Enumerable.Repeat("<a/>", elements)) + "</ImportExportXml>";

    [Fact]
    public void Defaults_are_pinned()
    {
        var budget = new ReadBudget();

        Assert.Equal(5_000_000, budget.MaxNodes);
        Assert.Equal(100_000, budget.MaxEntries);
        Assert.Equal(100_000, budget.MaxFiles);
        Assert.Equal(100_000, SolutionDiscovery.MaxFolders);
    }

    // R-1

    [Fact]
    public void Flow_240_levels_deep_with_100_character_keys_and_80000_leaves_allocates_under_200_MB()
    {
        var json = new StringBuilder("{\"properties\":{\"definition\":");
        for (var i = 0; i < 240; i++)
        {
            json.Append("{\"").Append('k', 100).Append("\":");
        }

        json.Append('[').Append(string.Join(',', Enumerable.Repeat("\"a\"", 80_000))).Append(']');
        json.Append('}', 240).Append("}}");
        var text = json.ToString();

        var before = GC.GetAllocatedBytesForCurrentThread();
        var flow = FlowDefinition.Parse(text);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        log.WriteLine($"R-1: {allocated / (1024 * 1024)} MB allocated");
        Assert.Equal(80_000, flow.StringLiterals.Count);
        Assert.True(allocated < 200L * 1024 * 1024, $"allocated {allocated / (1024 * 1024)} MB");
    }

    // R-2

    [Fact]
    public void Deep_canvas_meta_is_refused_naming_the_file()
    {
        using var t = new TempFolder()
            .Write("Other/Solution.xml", SolutionXml.Manifest())
            .Write("CanvasApps/app_1.meta.xml", Deep("a", 1000));

        var ex = Assert.Throws<InvalidDataException>(() => SolutionLoader.Load(t.Path));

        Assert.StartsWith(t.Path, ex.Message);
        Assert.Contains("CanvasApps/app_1.meta.xml", ex.Message);
    }

    [Fact]
    public void Diff_reads_canvas_metadata_through_the_depth_check()
    {
        var solution = B.Empty() with { CanvasAppTexts = [Deep("a", 1000)] };

        Assert.Throws<InvalidDataException>(() => SolutionDiff.Compare(solution, solution));
    }

    // R-3

    [Fact]
    public void PO007_value_of_160000_characters_is_fast()
    {
        var s = B.Empty() with { EnvironmentVariables = [B.Env("demo_A", def: Adversarial(80_000))] };

        var sw = Stopwatch.StartNew();
        var findings = new PO007EnvironmentSpecificDefaultValue().Evaluate(s).ToList();

        log.WriteLine($"R-3: PO007 {sw.ElapsedMilliseconds} ms");
        Assert.Empty(findings);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(2), $"took {sw.Elapsed}");
    }

    [Fact]
    public void PO007_still_finds_a_very_long_email()
    {
        var s = B.Empty() with { EnvironmentVariables = [B.Env("demo_A", def: new string('a', 400) + "@contoso.example")] };

        Assert.Single(new PO007EnvironmentSpecificDefaultValue().Evaluate(s));
    }

    [Fact]
    public void Every_rule_regex_takes_under_a_second_on_the_adversarial_value()
    {
        var v = Adversarial();
        var cases = new (string Name, IRule Rule, Solution Solution)[]
        {
            ("PO006 EnvironmentUrl", new PO006HardCodedEnvironmentUrl(), B.Empty() with { Workflows = [B.Flow(literals: [v])] }),
            ("PO007 Url, Guid, Email", new PO007EnvironmentSpecificDefaultValue(), B.Empty() with { EnvironmentVariables = [B.Env("demo_A", def: v)] }),
            ("PO010 Generated", new PO010DefaultPublisher(), B.Empty(prefix: v)),
        };

        foreach (var (name, rule, solution) in cases)
        {
            var sw = Stopwatch.StartNew();
            _ = rule.Evaluate(solution).ToList();
            log.WriteLine($"R-3: {name} {sw.ElapsedMilliseconds} ms");
            Assert.True(sw.Elapsed < TimeSpan.FromSeconds(1), $"{name} took {sw.Elapsed}");
        }
    }

    // R-4 (a): XML nodes, one budget per solution

    [Fact]
    public void Nodes_over_the_cap_in_one_file_are_refused_and_at_the_cap_are_read()
    {
        var manifest = SolutionXml.Manifest();
        var n = CountNodes(manifest);
        using var t = new TempFolder().Write("Other/Solution.xml", manifest);

        XmlUnpackReader.Read(t.Path, new ReadBudget(maxNodes: n));
        var ex = Assert.Throws<InvalidDataException>(() => XmlUnpackReader.Read(t.Path, new ReadBudget(maxNodes: n - 1)));

        Assert.Contains("Other/Solution.xml", ex.Message);
    }

    [Fact]
    public void Nodes_add_up_across_the_files_of_one_solution()
    {
        var manifest = SolutionXml.Manifest();
        var customizations = SolutionXml.Customizations("<a/><a/><a/>");
        var total = CountNodes(manifest) + CountNodes(customizations);
        using var t = new TempFolder()
            .Write("Other/Solution.xml", manifest)
            .Write("Other/Customizations.xml", customizations);

        XmlUnpackReader.Read(t.Path, new ReadBudget(maxNodes: total));
        var ex = Assert.Throws<InvalidDataException>(() => XmlUnpackReader.Read(t.Path, new ReadBudget(maxNodes: total - 1)));

        Assert.Contains("Customizations.xml", ex.Message);
    }

    [Fact]
    public void Five_million_and_one_nodes_in_a_file_are_refused_by_the_default_budget()
    {
        using var t = new TempFolder()
            .Write("Other/Solution.xml", SolutionXml.Manifest())
            .Write("Other/Customizations.xml", Flat(5_000_000));

        var sw = Stopwatch.StartNew();
        var ex = Assert.Throws<InvalidDataException>(() => SolutionLoader.Load(t.Path));

        log.WriteLine($"R-4: 5,000,002 nodes refused in {sw.ElapsedMilliseconds} ms");
        Assert.StartsWith(t.Path, ex.Message);
        Assert.Contains("Customizations.xml", ex.Message);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(10), $"took {sw.Elapsed}");
    }

    // R-4 (b): zip entries

    private static string ZipWithEntries(TempFolder t, int count)
    {
        var path = Path.Combine(t.Path, "x.zip");
        using var fs = File.Create(path);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Create);
        using (var w = new StreamWriter(zip.CreateEntry("solution.xml").Open()))
        {
            w.Write(SolutionXml.Manifest());
        }

        for (var i = 1; i < count; i++)
        {
            zip.CreateEntry($"f/{i}.txt");
        }

        return path;
    }

    [Fact]
    public void Zip_entries_over_the_cap_are_refused_and_at_the_cap_are_read()
    {
        using var t = new TempFolder();
        var zip = ZipWithEntries(t, 4);

        ZipReader.Read(zip, new ReadBudget(maxEntries: 4));
        var ex = Assert.Throws<InvalidDataException>(() => ZipReader.Read(zip, new ReadBudget(maxEntries: 3)));

        Assert.StartsWith(zip, ex.Message);
    }

    // R-4 (c): files in a solution folder

    [Fact]
    public void Folder_files_over_the_cap_are_refused_and_at_the_cap_are_read()
    {
        using var t = new TempFolder()
            .Write("Other/Solution.xml", SolutionXml.Manifest())
            .Write("a.txt", "")
            .Write("b/c.txt", "");

        XmlUnpackReader.Read(t.Path, new ReadBudget(maxFiles: 3));
        var ex = Assert.Throws<InvalidDataException>(() => XmlUnpackReader.Read(t.Path, new ReadBudget(maxFiles: 2)));

        Assert.StartsWith(t.Path, ex.Message);
    }

    [Fact]
    public void Yaml_root_files_over_the_cap_are_refused()
    {
        using var t = new TempFolder().Write("a.txt", "").Write("b.txt", "");

        var ex = Assert.Throws<InvalidDataException>(() => new YamlRoot(t.Path, new ReadBudget(maxFiles: 1)));

        Assert.StartsWith(t.Path, ex.Message);
    }

    // R-4 (d): folders walked by discovery

    [Fact]
    public void Discovery_folders_over_the_cap_are_refused_and_at_the_cap_are_walked()
    {
        using var t = new TempFolder().Write("a/f.txt", "").Write("b/f.txt", "").Write("b/c/f.txt", "");
        Assert.Empty(SolutionDiscovery.Find(t.Path, maxFolders: 4)); // the root, a, b and b/c

        var ex = Assert.Throws<InvalidDataException>(() => SolutionDiscovery.Find(t.Path, maxFolders: 3));

        Assert.StartsWith(t.Path, ex.Message);
        Assert.Contains("narrow the path", ex.Message);
    }
}