using System.Text.Json.Nodes;

using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

public class ManifestCommandTests : IDisposable
{
    private static readonly string Seed = Path.Combine(Golden.RepoRoot, "tests", "fixtures", "powerops-seed");
    private static readonly string V1 = Path.Combine(Seed, "v1");
    private static readonly string V2 = Path.Combine(Seed, "v2");
    private readonly string _temp = Directory.CreateTempSubdirectory("powerops-manifest-").FullName;

    public void Dispose() => Directory.Delete(_temp, recursive: true);

    private static (int Code, string Out, string Err) Run(params string[] args)
    {
        var o = new StringWriter();
        var e = new StringWriter();
        return (Program.Run(args, o, e), o.ToString().Replace("\r\n", "\n"), e.ToString().Replace("\r\n", "\n"));
    }

    // The paths in the output are the ones given; the golden holds the same text on every OS and checkout.
    private static string Shown(string output) => output.Replace(V1, "powerops-seed/v1").Replace(V2, "powerops-seed/v2");

    // The same, for JSON, which escapes a backslash in a Windows path.
    private static string ShownJson(string output) => output
        .Replace(V1.Replace("\\", "\\\\"), "powerops-seed/v1").Replace(V2.Replace("\\", "\\\\"), "powerops-seed/v2");

    [Fact]
    public void One_folder_prints_its_manifest_and_exits_0()
    {
        var (code, output, err) = Run("manifest", V1);

        Assert.Equal(0, code);
        Assert.Equal("", err);
        Golden.Verify(Shown(output), "manifest-seed-v1.txt");
    }

    [Fact]
    public void Two_folders_print_the_comparison_and_exit_1_when_they_differ()
    {
        var (code, output, err) = Run("manifest", V1, V2);

        Assert.Equal(1, code);
        Assert.Equal("", err);
        Golden.Verify(Shown(output), "manifest-seed-v1-v2.txt");
    }

    [Fact]
    public void Two_equal_folders_exit_0()
    {
        var (code, output, _) = Run("manifest", V1, V1);

        Assert.Equal(0, code);
        Assert.EndsWith("23 files the same\n", output);
    }

    [Fact]
    public void A_zip_prints_its_digest_alone()
    {
        var zip = Path.Combine(_temp, "Seed.zip");
        SolutionZip.Create(V1, zip);

        var (code, output, _) = Run("manifest", zip);

        Assert.Equal(0, code);
        Assert.Matches($"^{System.Text.RegularExpressions.Regex.Escape(zip)}: zip, digest [0-9a-f]{{64}}\n$", output);
    }

    [Fact]
    public void One_folder_in_JSON_prints_the_same_facts_and_exits_0()
    {
        var (code, output, err) = Run("manifest", V1, "--format", "json");

        Assert.Equal(0, code);
        Assert.Equal("", err);
        Golden.Verify(ShownJson(output), "manifest-seed-v1.json");
    }

    [Fact]
    public void Two_folders_in_JSON_print_both_manifests_and_the_comparison_and_exit_1_when_they_differ()
    {
        var (code, output, err) = Run("manifest", V1, V2, "--format", "json");

        Assert.Equal(1, code);
        Assert.Equal("", err);
        Golden.Verify(ShownJson(output), "manifest-seed-v1-v2.json");
    }

    [Fact]
    public void Two_equal_folders_in_JSON_exit_0_with_an_empty_comparison()
    {
        var (code, output, _) = Run("manifest", V1, V1, "--format", "json");

        var doc = JsonNode.Parse(output)!;
        Assert.Equal(0, code);
        Assert.Equal(2, doc["manifests"]!.AsArray().Count);
        Assert.Empty(doc["comparison"]!["added"]!.AsArray());
        Assert.Empty(doc["comparison"]!["removed"]!.AsArray());
        Assert.Empty(doc["comparison"]!["changed"]!.AsArray());
        Assert.Equal(23, (int)doc["comparison"]!["same"]!);
    }

    [Fact]
    public void A_zip_in_JSON_has_kind_zip_a_digest_and_no_files_and_no_comparison()
    {
        var zip = Path.Combine(_temp, "Seed.zip");
        SolutionZip.Create(V1, zip);

        var (code, output, _) = Run("manifest", zip, "--format", "json");

        var doc = JsonNode.Parse(output)!;
        var entry = doc["manifests"]!.AsArray().Single()!;
        Assert.Equal(0, code);
        Assert.Equal(zip, (string?)entry["path"]);
        Assert.Equal("zip", (string?)entry["kind"]);
        Assert.Matches("^[0-9a-f]{64}$", (string?)entry["digest"] ?? "");
        Assert.Empty(entry["files"]!.AsArray());
        Assert.Null(doc["comparison"]);
    }

    [Fact]
    public void Format_text_prints_the_same_as_no_format()
    {
        Assert.Equal(Run("manifest", V1, V2), Run("manifest", V1, V2, "--format", "text"));
    }

    [Fact]
    public void An_unknown_format_exits_2()
    {
        var (code, output, err) = Run("manifest", V1, "--format", "xml");

        Assert.Equal(2, code);
        Assert.Equal("", output);
        Assert.Contains("Argument 'xml' not recognized", err);
    }

    [Fact]
    public void A_missing_path_a_plain_file_and_a_zip_in_the_two_path_form_exit_2_with_an_error_line()
    {
        var text = Path.Combine(_temp, "notes.txt");
        File.WriteAllText(text, "x");
        var zip = Path.Combine(_temp, "Seed.zip");
        SolutionZip.Create(V1, zip);

        foreach (var args in new string[][] { ["manifest", Path.Combine(_temp, "none")], ["manifest", text], ["manifest", V1, zip], ["manifest", zip, V1], ["manifest", V1, Path.Combine(_temp, "none")] })
        {
            var (code, output, err) = Run(args);

            Assert.Equal(2, code);
            Assert.Equal("", output);
            Assert.StartsWith("error: ", err);
        }
    }

    [Fact]
    public void A_link_as_the_path_exits_2()
    {
        var link = Path.Combine(_temp, "link");
        Links.Create(link, V1, folder: true);

        var (code, output, err) = Run("manifest", link);

        Assert.Equal(2, code);
        Assert.Equal("", output);
        Assert.Contains("a symbolic link, not read", err);
    }

    [Fact]
    public void A_link_inside_is_skipped_with_a_note_on_stderr()
    {
        var folder = Path.Combine(_temp, "sol");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "a.xml"), "a");
        var outside = Path.Combine(_temp, "outside.xml");
        File.WriteAllText(outside, "o");
        Links.Create(Path.Combine(folder, "b.xml"), outside);

        var (code, output, err) = Run("manifest", folder);

        Assert.Equal(0, code);
        Assert.Contains("folder, 1 file,", output);
        Assert.Equal($"note: {Path.Combine(folder, "b.xml").Replace('\\', '/')}: a symbolic link, not read\n", err);
    }

    [Fact]
    public void An_unreadable_input_exits_3_with_an_error_line()
    {
        var zip = Path.Combine(_temp, "Big.zip");
        File.WriteAllBytes(zip, []);
        using (var stream = new FileStream(zip, FileMode.Open))
        {
            stream.SetLength(PowerOps.Core.Readers.BoundedRead.MaxZipBytes + 1);
        }

        var (code, output, err) = Run("manifest", zip);

        Assert.Equal(3, code);
        Assert.Equal("", output);
        Assert.StartsWith($"error: {zip}: larger than 100 MB", err);
    }

}