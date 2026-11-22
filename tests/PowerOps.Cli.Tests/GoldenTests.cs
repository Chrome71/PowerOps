using PowerOps.Tests.Shared;

using Xunit.Sdk;

namespace PowerOps.Cli.Tests;

public class GoldenTests : IDisposable
{
    private readonly DirectoryInfo _temp = Directory.CreateTempSubdirectory("powerops-golden-");
    private string TempGolden => Path.Combine(_temp.FullName, "golden.txt");

    public void Dispose() => _temp.Delete(recursive: true);

    [Fact]
    public void Verify_ignores_line_ending_differences()
    {
        Golden.Verify("golden self-test\r\nline two\r\n", "selftest.txt", allowUpdate: false);
    }

    [Fact]
    public void Verify_fails_when_content_differs()
    {
        Assert.Throws<EqualException>(() => Golden.Verify("different", "selftest.txt", allowUpdate: false));
    }

    [Fact]
    public void Compare_preserves_original_bytes_on_mismatch()
    {
        var original = "original\r\n"u8.ToArray();
        File.WriteAllBytes(TempGolden, original);

        Assert.Throws<EqualException>(() => Golden.VerifyFile("different", TempGolden, update: false));
        Assert.Equal(original, File.ReadAllBytes(TempGolden));
    }

    [Fact]
    public void Update_creates_and_overwrites_with_normalized_utf8()
    {
        Golden.VerifyFile("café\r\nfirst line\r\n", TempGolden, update: true);
        Assert.Equal("café\nfirst line\n"u8.ToArray(), File.ReadAllBytes(TempGolden));

        Golden.VerifyFile("déjà\r\n", TempGolden, update: true);
        Assert.Equal("déjà\n"u8.ToArray(), File.ReadAllBytes(TempGolden));
    }

    [Fact]
    public void Compare_reports_how_to_create_a_missing_golden()
    {
        var error = Assert.Throws<XunitException>(() => Golden.VerifyFile("missing", TempGolden, update: false));
        Assert.Contains("Rerun with POWEROPS_UPDATE_GOLDEN=1 to create it.", error.Message);
        Assert.False(File.Exists(TempGolden));
    }
}