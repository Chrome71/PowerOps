using System.IO.Compression;
using System.Text;

using PowerOps.Core.Model;
using PowerOps.Core.Readers;
using PowerOps.Tests.Shared;

namespace PowerOps.Core.Tests.Readers;

/// <summary>M44: the formulas of each `.msapp` in the XML layouts, read from its `Src/*.pa.yaml` in memory. Every `.msapp` is built here; none is kept in the repo.</summary>
public class CanvasReaderTests
{
    private const string Msapp = "CanvasApps/x_DocumentUri.msapp";

    private const string AppYaml = """
        App:
          Properties:
            OnStart: =Set(counter, 1)
        Screens:
          Screen1:
            Properties:
              Fill: =Color.Red
            Children:
              - Button1:
                  Control: Button@2.2.0
                  Properties:
                    OnSelect: =Launch("https://contoso.sharepoint.com/sites/ops")
        """;

    private static string Meta(string documentUri = "/CanvasApps/x_DocumentUri.msapp", string display = "Work Items") =>
        $"<CanvasApp><Name>x</Name><DisplayName>{display}</DisplayName><DocumentUri>{documentUri}</DocumentUri></CanvasApp>";

    private static TempFolder Folder() => new TempFolder().Write("Other/Solution.xml", SolutionXml.Manifest());

    private static string MsappPath(TempFolder t, string name = "x_DocumentUri.msapp") => Path.Combine(t.Path, "CanvasApps", name);

    private static Solution Read(TempFolder t, ReadBudget? budget = null) => XmlUnpackReader.Read(t.Path, budget ?? new ReadBudget());

    private static Solution ReadZip(TempFolder t, ReadBudget? budget = null)
    {
        var zip = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.zip");
        try
        {
            SolutionZip.Create(t.Path, zip);
            return ZipReader.Read(zip, budget ?? new ReadBudget());
        }
        finally
        {
            File.Delete(zip);
        }
    }

    private static string Refusal(ReadBudget? budget, params (string Name, string Text)[] entries)
    {
        using var t = Folder();
        SolutionZip.Msapp(MsappPath(t), entries);
        return Assert.Throws<InvalidDataException>(() => Read(t, budget)).Message;
    }

    [Fact]
    public void A_folder_gives_the_formulas_of_its_msapp_named_by_the_meta()
    {
        using var t = Folder().Write("CanvasApps/x.meta.xml", Meta());
        SolutionZip.Msapp(MsappPath(t), ("Src\\App.pa.yaml", AppYaml));

        var s = Read(t);

        Assert.Equal(
            [
                new CanvasFormula("Work Items", "App", "OnStart", "=Set(counter, 1)", Msapp),
                new CanvasFormula("Work Items", "Screen1", "Fill", "=Color.Red", Msapp),
                new CanvasFormula("Work Items", "Button1", "OnSelect", "=Launch(\"https://contoso.sharepoint.com/sites/ops\")", Msapp),
            ],
            s.CanvasFormulas);
        Assert.Empty(s.CanvasAppsWithoutSource);
    }

    [Fact]
    public void The_same_folder_zipped_gives_the_same_formulas()
    {
        using var t = Folder().Write("CanvasApps/x.meta.xml", Meta());
        SolutionZip.Msapp(MsappPath(t), ("Src\\App.pa.yaml", AppYaml));

        var fromZip = ReadZip(t).CanvasFormulas;

        Assert.Equal(Read(t).CanvasFormulas, fromZip);
        Assert.NotEmpty(fromZip);
    }

    [Fact]
    public void A_zip_with_only_a_customizations_canvas_app_element_names_the_app_as_the_folder_layout_does()
    {
        // A pac export holds the app's metadata in customizations.xml and no .meta.xml; the unpacked folder holds the meta file.
        using var unpacked = Folder().Write("CanvasApps/x.meta.xml", Meta(display: "Work Items"));
        SolutionZip.Msapp(MsappPath(unpacked), ("Src/App.pa.yaml", AppYaml));
        using var exported = Folder().Write("Other/Customizations.xml", $"<ImportExportXml><CanvasApps>{Meta(display: "Work Items")}</CanvasApps></ImportExportXml>");
        SolutionZip.Msapp(MsappPath(exported), ("Src/App.pa.yaml", AppYaml));

        var fromZip = ReadZip(exported).CanvasFormulas;

        Assert.NotEmpty(fromZip);
        Assert.Equal(Read(unpacked).CanvasFormulas, fromZip);
        Assert.All(fromZip, f => Assert.Equal("Work Items", f.App));
    }

    [Fact]
    public void The_name_falls_back_to_the_app_name_and_then_to_the_file_name()
    {
        using var t = Folder().Write("CanvasApps/x.meta.xml", "<CanvasApp><Name>schema_name</Name><DocumentUri>/CanvasApps/x_DocumentUri.msapp</DocumentUri></CanvasApp>");
        SolutionZip.Msapp(MsappPath(t), ("Src/App.pa.yaml", AppYaml));
        SolutionZip.Msapp(MsappPath(t, "other_DocumentUri.msapp"), ("Src/App.pa.yaml", AppYaml));
        SolutionZip.Msapp(MsappPath(t, "plain.msapp"), ("Src/App.pa.yaml", AppYaml));

        var apps = Read(t).CanvasFormulas.Select(f => (f.App, f.File)).Distinct().ToList();

        Assert.Equal(
            [("other", "CanvasApps/other_DocumentUri.msapp"), ("plain", "CanvasApps/plain.msapp"), ("schema_name", Msapp)],
            apps.OrderBy(a => a.App, StringComparer.Ordinal));
    }

    [Fact]
    public void A_meta_file_that_is_not_well_formed_gives_no_name_and_does_not_stop_the_read()
    {
        using var t = Folder().Write("CanvasApps/x.meta.xml", "<CanvasApp><DisplayName>Broken");
        SolutionZip.Msapp(MsappPath(t), ("Src/App.pa.yaml", AppYaml));

        Assert.All(Read(t).CanvasFormulas, f => Assert.Equal("x", f.App));
    }

    [Theory]
    [InlineData("Src\\App.pa.yaml")]
    [InlineData("src/app.PA.YAML")]
    [InlineData("Src\\Components\\C.pa.yaml")]
    public void Source_entries_are_found_with_either_separator_and_any_case(string entry)
    {
        using var t = Folder();
        SolutionZip.Msapp(MsappPath(t), (entry, "App:\n  Properties:\n    OnStart: =Set(a, 1)\n"));

        var f = Assert.Single(Read(t).CanvasFormulas);

        Assert.Equal("=Set(a, 1)", f.Formula);
    }

    [Fact]
    public void Component_definitions_are_read()
    {
        using var t = Folder();
        SolutionZip.Msapp(MsappPath(t), ("Src\\Components\\C.pa.yaml", "ComponentDefinitions:\n  Header:\n    Properties:\n      Fill: =Color.Blue\n"));

        var f = Assert.Single(Read(t).CanvasFormulas);

        Assert.Equal(("Header", "Fill", "=Color.Blue"), (f.Control, f.Property, f.Formula));
    }

    [Theory]
    [InlineData("Controls\\1.json")]
    [InlineData("Src/notes.txt")]
    [InlineData("../Src/x.pa.yaml")]
    [InlineData("/Src/x.pa.yaml")]
    [InlineData("C:/Src/x.pa.yaml")]
    [InlineData("Other/Src/x.pa.yaml")]
    public void Other_entries_are_not_read_and_leave_the_app_without_source(string entry)
    {
        using var t = Folder();
        SolutionZip.Msapp(MsappPath(t), (entry, AppYaml));

        var s = Read(t);

        Assert.Empty(s.CanvasFormulas);
        Assert.Equal(Msapp, Assert.Single(s.CanvasAppsWithoutSource));
    }

    [Fact]
    public void An_msapp_with_no_source_is_listed_in_the_folder_and_the_zip()
    {
        using var t = Folder().Write("CanvasApps/x.meta.xml", Meta());
        SolutionZip.Msapp(MsappPath(t), ("Header.json", "{}"));

        foreach (var s in new[] { Read(t), ReadZip(t) })
        {
            Assert.Empty(s.CanvasFormulas);
            Assert.Equal(Msapp, Assert.Single(s.CanvasAppsWithoutSource));
        }
    }

    [Fact]
    public void A_meta_whose_msapp_the_solution_does_not_hold_is_not_read_and_not_noted()
    {
        using var t = Folder().Write("CanvasApps/x.meta.xml", Meta()).Write("CanvasApps/y.meta.xml", Meta("/CanvasApps/y_DocumentUri"));

        foreach (var s in new[] { Read(t), ReadZip(t) })
        {
            Assert.Empty(s.CanvasFormulas);
            Assert.Empty(s.CanvasAppsWithoutSource);
        }
    }

    [Fact]
    public void A_solution_without_canvas_apps_has_no_entries()
    {
        using var t = Folder();

        var s = Read(t);

        Assert.Empty(s.CanvasFormulas);
        Assert.Empty(s.CanvasAppsWithoutSource);
    }

    // Hostile input. Each refusal starts with the .msapp's path.

    [Fact]
    public void More_entries_than_the_cap_are_refused()
    {
        var message = Refusal(new ReadBudget(maxEntries: 2), ("Src/A.pa.yaml", ""), ("Src/B.pa.yaml", ""), ("Header.json", "{}"));

        Assert.Equal($"{Msapp}: more than 2 entries.", message);
    }

    [Fact]
    public void An_entry_declared_over_the_file_cap_is_refused_without_reading_it()
    {
        var message = Refusal(new ReadBudget(maxFileBytes: 2000), ("Src/App.pa.yaml", new string('a', 5000)));

        Assert.StartsWith($"{Msapp}: Src/App.pa.yaml: larger than", message);
    }

    [Fact]
    public void An_entry_that_lies_about_its_length_gives_no_more_bytes_than_it_declared()
    {
        // The library stops at the declared length, so a lie cannot get past the file cap; ReadCapped still counts the bytes it is given.
        using var t = Folder();
        var path = MsappPath(t);
        SolutionZip.Msapp(path, ("Src/App.pa.yaml", new string('a', 5000)));
        var bytes = File.ReadAllBytes(path);
        // Declare 10 bytes in the central directory (uncompressed size at +24) and the local header (+22).
        foreach (var (signature, offset) in new[] { (new byte[] { 0x50, 0x4b, 0x01, 0x02 }, 24), (new byte[] { 0x50, 0x4b, 0x03, 0x04 }, 22) })
        {
            BitConverter.GetBytes(10).CopyTo(bytes, bytes.AsSpan().IndexOf(signature) + offset);
        }

        File.WriteAllBytes(path, bytes);
        using var zip = ZipFile.OpenRead(path);
        using var counted = new MemoryStream();
        zip.GetEntry("Src/App.pa.yaml")!.Open().CopyTo(counted);

        Assert.True(counted.Length <= 10, $"{counted.Length} bytes");
    }

    [Fact]
    public void Entries_that_each_fit_the_file_cap_but_together_pass_the_total_are_refused()
    {
        var bomb = new string('a', 1500);

        var message = Refusal(new ReadBudget(maxFileBytes: 2000, maxTotalBytes: 6000), ("Src/A.pa.yaml", bomb), ("Src/B.pa.yaml", bomb), ("Src/C.pa.yaml", bomb), ("Src/D.pa.yaml", bomb));

        Assert.StartsWith($"{Msapp}: ", message);
        Assert.Contains("solution is larger than", message);
    }

    [Fact]
    public void The_bytes_of_the_msapp_and_of_its_entries_count_against_the_solution_total()
    {
        using var t = Folder();
        var path = MsappPath(t);
        SolutionZip.Msapp(path, ("Src/App.pa.yaml", AppYaml));
        var manifest = Encoding.UTF8.GetByteCount(SolutionXml.Manifest());
        var package = (int)new FileInfo(path).Length;
        var entry = Encoding.UTF8.GetByteCount(AppYaml);

        Read(t, new ReadBudget(maxTotalBytes: manifest + package + entry));
        var onPackage = Assert.Throws<InvalidDataException>(() => Read(t, new ReadBudget(maxTotalBytes: manifest + package - 1)));
        var onEntry = Assert.Throws<InvalidDataException>(() => Read(t, new ReadBudget(maxTotalBytes: manifest + package + entry - 1)));

        Assert.StartsWith($"{Msapp}: solution is larger than", onPackage.Message);
        Assert.StartsWith($"{Msapp}: Src/App.pa.yaml: solution is larger than", onEntry.Message);
    }

    [Fact]
    public void Yaml_nested_deeper_than_the_cap_is_refused()
    {
        var deep = string.Concat(Enumerable.Range(0, 250).Select(i => new string(' ', i * 2) + "k:\n")) + new string(' ', 500) + "v: 1\n";

        var message = Refusal(null, ("Src/App.pa.yaml", deep));

        Assert.StartsWith($"{Msapp}: Src/App.pa.yaml: ", message);
        Assert.Contains("nested deeper", message);
    }

    [Fact]
    public void Yaml_with_an_anchor_is_refused()
    {
        var message = Refusal(null, ("Src/S.pa.yaml", "Screens:\n  S:\n    Properties: &p\n      Fill: =red\n"));

        Assert.StartsWith($"{Msapp}: Src/S.pa.yaml: ", message);
        Assert.Contains("anchors", message);
    }

    [Fact]
    public void Two_source_names_equal_without_case_are_refused()
    {
        var message = Refusal(null, ("Src/App.pa.yaml", AppYaml), ("src\\app.pa.yaml", AppYaml));

        Assert.StartsWith($"{Msapp}: ", message);
        Assert.Contains("listed more than once", message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("version https://git-lfs.github.com/spec/v1\noid sha256:0000000000000000000000000000000000000000000000000000000000000000\nsize 123\n")]
    [InlineData("placeholder")]
    public void A_file_without_the_zip_signature_is_not_opened_and_is_listed_without_source(string content)
    {
        using var t = Folder();
        Directory.CreateDirectory(Path.GetDirectoryName(MsappPath(t))!);
        File.WriteAllText(MsappPath(t), content);

        foreach (var s in new[] { Read(t), ReadZip(t) })
        {
            Assert.Empty(s.CanvasFormulas);
            Assert.Equal(Msapp, Assert.Single(s.CanvasAppsWithoutSource));
        }
    }

    [Fact]
    public void A_zip_signature_on_a_file_that_is_not_a_zip_and_a_truncated_zip_are_refused()
    {
        using var t = Folder();
        var path = MsappPath(t);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "PK but not a zip");

        Assert.StartsWith($"{Msapp}: ", Assert.Throws<InvalidDataException>(() => Read(t)).Message);

        File.Delete(path);
        SolutionZip.Msapp(path, ("Src/App.pa.yaml", AppYaml));
        var bytes = File.ReadAllBytes(path);
        File.WriteAllBytes(path, bytes[..(bytes.Length / 2)]);

        Assert.StartsWith($"{Msapp}: ", Assert.Throws<InvalidDataException>(() => Read(t)).Message);
    }

    [Fact]
    public void An_msapp_over_the_file_cap_but_under_the_zip_cap_is_read_and_over_the_zip_cap_is_refused()
    {
        using var t = Folder();
        var noise = string.Concat(Enumerable.Range(0, 1500).Select(i => (i * 2654435761L % 4294967296L).ToString("x8")));
        SolutionZip.Msapp(MsappPath(t), ("Src/App.pa.yaml", AppYaml), ("Resources/media.txt", noise));
        var size = new FileInfo(MsappPath(t)).Length;
        Assert.InRange(size, 3000, 90_000);

        foreach (var s in new[] { Read(t, new ReadBudget(maxFileBytes: 2000, maxZipBytes: 100_000)), ReadZip(t, new ReadBudget(maxFileBytes: 2000, maxZipBytes: 100_000)) })
        {
            Assert.NotEmpty(s.CanvasFormulas);
        }

        var message = Assert.Throws<InvalidDataException>(() => Read(t, new ReadBudget(maxZipBytes: size - 1))).Message;
        Assert.StartsWith($"{Msapp}: larger than", message);
    }

    [Fact]
    public void An_entry_count_declared_over_the_cap_is_refused_by_the_guard_before_the_directory_is_parsed()
    {
        // Only an end-of-central-directory record: 5 entries declared, a directory that starts past the end of the file.
        // ZipArchive would fail on that with its own message, so only ZipGuard can give the entry-count one.
        var record = new byte[22];
        BitConverter.GetBytes(0x06054b50).CopyTo(record, 0);
        BitConverter.GetBytes((ushort)5).CopyTo(record, 8);
        BitConverter.GetBytes((ushort)5).CopyTo(record, 10);
        BitConverter.GetBytes(1000).CopyTo(record, 12);
        BitConverter.GetBytes(5000).CopyTo(record, 16);
        using var t = Folder();
        Directory.CreateDirectory(Path.GetDirectoryName(MsappPath(t))!);
        File.WriteAllBytes(MsappPath(t), record);

        var message = Assert.Throws<InvalidDataException>(() => Read(t, new ReadBudget(maxEntries: 2))).Message;

        Assert.Equal($"{Msapp}: more than 2 entries.", message);
    }

    [Fact]
    public void Flipping_the_low_bit_of_each_byte_of_an_msapp_loads_it_or_refuses_it_with_its_path()
    {
        using var t = Folder();
        var path = MsappPath(t);
        SolutionZip.Msapp(path, ("Src\\App.pa.yaml", AppYaml), ("Src\\Components\\C.pa.yaml", "ComponentDefinitions:\n  Header:\n    Properties:\n      Fill: =Color.Blue\n"), ("Header.json", "{}"));
        var original = File.ReadAllBytes(path);
        Assert.InRange(original.Length, 200, 3000);

        for (var i = 0; i < original.Length; i++)
        {
            var mutant = (byte[])original.Clone();
            mutant[i] ^= 1;
            File.WriteAllBytes(path, mutant);
            try
            {
                Read(t);
            }
            catch (InvalidDataException e) when (e.Message.StartsWith($"{Msapp}: ", StringComparison.Ordinal))
            {
            }
            catch (Exception e)
            {
                Assert.Fail($"byte {i}: {e}");
            }
        }
    }

    [Fact]
    public void Many_properties_in_an_msapp_entry_are_refused_by_the_node_budget_naming_the_msapp()
    {
        var many = "App:\n  Properties:\n" + string.Concat(Enumerable.Range(0, 300).Select(i => $"    P{i}: =1\n"));
        using var t = Folder();
        SolutionZip.Msapp(MsappPath(t), ("Src/App.pa.yaml", many));

        Assert.Equal(300, Read(t).CanvasFormulas.Count);
        var message = Assert.Throws<InvalidDataException>(() => Read(t, new ReadBudget(maxNodes: 240))).Message;

        Assert.StartsWith($"{Msapp}: Src/App.pa.yaml: ", message);
        Assert.Equal(1, message.Split($"{Msapp}:").Length - 1);
        Assert.Contains("XML nodes", message);
    }

    [Fact]
    public void A_refusal_through_the_loader_starts_with_the_solution_path_and_names_the_msapp()
    {
        using var t = Folder();
        SolutionZip.Msapp(MsappPath(t), ("Src/S.pa.yaml", "Screens:\n  S:\n    Properties: &p\n      Fill: =red\n"));

        var ex = Assert.Throws<InvalidDataException>(() => SolutionLoader.Load(t.Path));

        Assert.StartsWith(t.Path, ex.Message);
        Assert.Contains($"{Msapp}: Src/S.pa.yaml: ", ex.Message);
    }

    [Fact]
    public void An_entry_outside_Src_is_never_opened()
    {
        using var t = Folder();
        var path = MsappPath(t);
        SolutionZip.Msapp(path, ("Controls\\1.json", string.Join(',', Enumerable.Range(0, 2000))), ("Src\\App.pa.yaml", AppYaml));
        var bytes = File.ReadAllBytes(path);
        // The first local header starts the file; its data follows the 30-byte header and the name.
        Array.Fill(bytes, (byte)0xFF, 30 + "Controls\\1.json".Length, 16);
        File.WriteAllBytes(path, bytes);
        using (var zip = ZipFile.OpenRead(path))
        {
            using var entry = zip.GetEntry("Controls\\1.json")!.Open();
            Assert.Throws<InvalidDataException>(() => entry.CopyTo(Stream.Null));
        }

        Assert.NotEmpty(Read(t).CanvasFormulas);
    }
}