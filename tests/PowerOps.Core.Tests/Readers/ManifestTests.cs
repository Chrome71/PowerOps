using System.Security.Cryptography;
using System.Text;

using PowerOps.Core.Deploy;
using PowerOps.Core.Readers;
using PowerOps.Tests.Shared;

namespace PowerOps.Core.Tests.Readers;

public class ManifestTests
{
    private static string Hex(string text) => DeployPlan.Sha256Hex(Encoding.UTF8.GetBytes(text));

    [Fact]
    public void Files_are_listed_with_forward_slashes_in_ordinal_order()
    {
        using var t = new TempFolder();
        // C.xml, not B.xml: Windows file names ignore case, so B.xml would overwrite b.xml there.
        t.Write("b.xml", "b").Write("Other/Solution.xml", "s").Write("a/z.xml", "z").Write("C.xml", "C");

        var m = Manifest.Read(t.Path);

        Assert.Equal("folder", m.Kind);
        Assert.Equal(["C.xml", "Other/Solution.xml", "a/z.xml", "b.xml"], m.Files.Select(f => f.Path));
        Assert.Equal(Hex("s"), m.Files[1].Sha256);
    }

    [Fact]
    public void A_backslash_in_a_file_name_is_not_a_folder_separator_on_Linux()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "a backslash separates folders on Windows");
        using var t = new TempFolder().Write("a/b.txt", "one").Write("a\\b.txt", "two");

        var m = Manifest.Read(t.Path);

        Assert.Equal(["a/b.txt", "a\\b.txt"], m.Files.Select(f => f.Path));
        Assert.Equal(m.Files.Count, Manifest.Compare(m, m).Same);
    }

    [Fact]
    public void The_digest_is_the_hash_of_the_sorted_hash_and_path_lines()
    {
        using var t = new TempFolder();
        t.Write("b.txt", "two").Write("a.txt", "one");

        var expected = Hex($"{Hex("one")}  a.txt\n{Hex("two")}  b.txt\n");

        Assert.Equal(expected, Manifest.Read(t.Path).Digest);
        Assert.Matches("^[0-9a-f]{64}$", expected);
    }

    [Fact]
    public void An_empty_folder_has_no_files_and_the_hash_of_nothing()
    {
        using var t = new TempFolder();

        var m = Manifest.Read(t.Path);

        Assert.Empty(m.Files);
        Assert.Equal(Hex(""), m.Digest);
    }

    [Fact]
    public void Crlf_and_lf_give_one_hash_and_one_digest()
    {
        using var lf = new TempFolder();
        using var crlf = new TempFolder();
        lf.Write("a.xml", "<a>\n  x\n</a>\n");
        crlf.Write("a.xml", "<a>\r\n  x\r\n</a>\r\n");

        var a = Manifest.Read(lf.Path);
        var b = Manifest.Read(crlf.Path);

        Assert.Equal(Hex("<a>\n  x\n</a>\n"), a.Files[0].Sha256);
        Assert.Equal(a.Files[0].Sha256, b.Files[0].Sha256);
        Assert.Equal(a.Digest, b.Digest);
    }

    [Fact]
    public void A_lone_carriage_return_is_kept()
    {
        using var t = new TempFolder();
        t.Write("a.txt", "a\rb\r\r\nc");

        Assert.Equal(Hex("a\rb\r\nc"), Manifest.Read(t.Path).Files[0].Sha256);
    }

    [Fact]
    public void A_file_with_a_zero_byte_is_hashed_as_its_bytes()
    {
        using var t = new TempFolder();
        var bytes = new byte[] { 1, 13, 10, 0, 13, 10 };
        File.WriteAllBytes(Path.Combine(t.Path, "app.msapp"), bytes);

        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(bytes)), Manifest.Read(t.Path).Files[0].Sha256);
    }

    [Fact]
    public void A_link_inside_is_skipped_and_reported_and_a_skipped_folder_is_not_entered()
    {
        using var t = new TempFolder();
        t.Write("real.xml", "x").Write(".git/config", "g").Write("node_modules/p/a.js", "n");
        using var outside = new TempFolder();
        outside.Write("secret.xml", "s");
        Links.Create(Path.Combine(t.Path, "link.xml"), Path.Combine(outside.Path, "secret.xml"));
        Links.Create(Path.Combine(t.Path, "linked"), outside.Path, folder: true);

        var links = new List<string>();
        var m = Manifest.Read(t.Path, links);

        Assert.Equal(["real.xml"], m.Files.Select(f => f.Path));
        var root = t.Path.Replace('\\', '/');
        Assert.Equal([root + "/link.xml", root + "/linked"], links.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void A_link_as_the_path_and_a_missing_path_and_another_file_throw_with_the_path_first()
    {
        using var t = new TempFolder();
        t.Write("a.txt", "a").Write("b.xml", "b");
        var link = Path.Combine(t.Path, "link");
        Links.Create(link, Path.Combine(t.Path, "a.txt"));
        var missing = Path.Combine(t.Path, "none");
        var other = Path.Combine(t.Path, "b.xml");

        foreach (var path in new[] { link, missing, other })
        {
            var e = Assert.Throws<InvalidDataException>(() => Manifest.Read(path));
            Assert.StartsWith(path + ":", e.Message);
        }
    }

    [Fact]
    public void A_zip_has_no_files_and_its_digest_is_the_hash_deploy_plan_records()
    {
        using var t = new TempFolder();
        using var content = new TempFolder();
        content.Write("Other/Solution.xml", "<ImportExportXml/>");
        var path = Path.Combine(t.Path, "Demo.ZIP");
        SolutionZip.Create(content.Path, path);
        var zip = File.ReadAllBytes(path);

        var m = Manifest.Read(path);

        Assert.Equal("zip", m.Kind);
        Assert.Empty(m.Files);
        Assert.Equal(DeployPlan.Sha256Hex(zip), m.Digest);
    }

    [Fact]
    public void A_file_over_the_cap_and_too_many_files_and_too_many_bytes_are_refused()
    {
        using var t = new TempFolder();
        t.Write("a.txt", "12345").Write("b.txt", "12345");

        var big = Assert.Throws<InvalidDataException>(() => Manifest.Read(t.Path, new ReadBudget(maxFileBytes: 4)));
        var many = Assert.Throws<InvalidDataException>(() => Manifest.Read(t.Path, new ReadBudget(maxFiles: 1)));
        var total = Assert.Throws<InvalidDataException>(() => Manifest.Read(t.Path, new ReadBudget(maxTotalBytes: 8)));

        Assert.Contains(".txt: larger than 4 bytes", big.Message);
        Assert.StartsWith(t.Path + ":", many.Message);
        Assert.Contains("larger than", total.Message);
    }

    [Fact]
    public void Compare_lists_added_removed_and_changed_in_ordinal_order_and_counts_the_same()
    {
        using var older = new TempFolder();
        using var newer = new TempFolder();
        older.Write("same.xml", "s").Write("gone2.xml", "g").Write("gone1.xml", "g").Write("edit/b.xml", "1").Write("edit/a.xml", "1");
        newer.Write("same.xml", "s").Write("new2.xml", "n").Write("new1.xml", "n").Write("edit/b.xml", "2").Write("edit/a.xml", "2");

        var c = Manifest.Compare(Manifest.Read(older.Path), Manifest.Read(newer.Path));

        Assert.Equal(["new1.xml", "new2.xml"], c.Added);
        Assert.Equal(["gone1.xml", "gone2.xml"], c.Removed);
        Assert.Equal(["edit/a.xml", "edit/b.xml"], c.Changed);
        Assert.Equal(1, c.Same);
    }

    [Fact]
    public void Compare_of_equal_folders_has_nothing_but_the_same_count()
    {
        using var older = new TempFolder();
        using var newer = new TempFolder();
        older.Write("a.xml", "a\n");
        newer.Write("a.xml", "a\r\n");

        var c = Manifest.Compare(Manifest.Read(older.Path), Manifest.Read(newer.Path));

        Assert.Empty(c.Added);
        Assert.Empty(c.Removed);
        Assert.Empty(c.Changed);
        Assert.Equal(1, c.Same);
    }
}