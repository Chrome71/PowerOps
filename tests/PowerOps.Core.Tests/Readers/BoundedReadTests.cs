using System.Diagnostics;
using System.Text;

using PowerOps.Core.Readers;

namespace PowerOps.Core.Tests.Readers;

/// <summary>M26 (R-10): a read that counts the bytes it reads, because <c>FileInfo.Length</c> is 0 for a special file.</summary>
public class BoundedReadTests
{
    [Fact]
    public void A_file_at_the_cap_is_read_and_one_byte_over_is_refused_with_the_path_first()
    {
        using var t = new TempFolder();
        t.Write("at.txt", new string('a', 100));
        t.Write("over.txt", new string('a', 101));

        Assert.Equal(100, BoundedRead.ReadAllText(Path.Combine(t.Path, "at.txt"), 100).Length);
        var e = Assert.Throws<InvalidDataException>(() => BoundedRead.ReadAllBytes(Path.Combine(t.Path, "over.txt"), 100));
        Assert.Equal($"{Path.Combine(t.Path, "over.txt")}: larger than 100 bytes.", e.Message);
    }

    [Fact]
    public void The_cap_is_named_in_megabytes_when_it_is_whole()
    {
        using var t = new TempFolder();
        t.Write("a.txt", "xx");

        var e = Assert.Throws<InvalidDataException>(() => BoundedRead.ReadAllBytes(Path.Combine(t.Path, "a.txt"), 0));
        Assert.EndsWith(": larger than 0 MB.", e.Message);
        e = Assert.Throws<InvalidDataException>(() => BoundedRead.ReadAllBytes(Path.Combine(t.Path, "a.txt"), 1));
        Assert.EndsWith(": larger than 1 bytes.", e.Message);
    }

    [Fact]
    public void A_special_file_that_never_ends_is_refused_at_the_cap()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "No /dev/zero.");
        var watch = Stopwatch.StartNew();

        var e = Assert.Throws<InvalidDataException>(() => BoundedRead.ReadAllBytes("/dev/zero", 1024 * 1024));

        Assert.StartsWith("/dev/zero: larger than 1 MB", e.Message);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(5), watch.Elapsed.ToString());
    }

    [Fact]
    public void Text_is_decoded_like_File_ReadAllText_with_or_without_a_byte_order_mark()
    {
        using var t = new TempFolder();
        t.Write("bom.txt", "héllo", bom: true);
        t.Write("plain.txt", "héllo");

        Assert.Equal("héllo", BoundedRead.ReadAllText(Path.Combine(t.Path, "bom.txt"), 100));
        Assert.Equal("héllo", BoundedRead.ReadAllText(Path.Combine(t.Path, "plain.txt"), 100));
        File.WriteAllText(Path.Combine(t.Path, "u16.txt"), "héllo", Encoding.Unicode);
        Assert.Equal("héllo", BoundedRead.ReadAllText(Path.Combine(t.Path, "u16.txt"), 100));
    }
}