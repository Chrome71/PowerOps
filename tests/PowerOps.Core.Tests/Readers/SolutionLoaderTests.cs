using PowerOps.Core.Readers;

namespace PowerOps.Core.Tests.Readers;

public class SolutionLoaderTests
{
    [Fact]
    public void Folder_errors_name_the_solution_folder()
    {
        using var t = new TempFolder().Write("Other/Solution.xml", "<ImportExportXml><oops>");

        var ex = Assert.Throws<InvalidDataException>(() => SolutionLoader.Load(t.Path));

        Assert.StartsWith(t.Path, ex.Message);
        Assert.Contains("Other/Solution.xml", ex.Message);
    }

    [Fact]
    public void Zip_errors_name_the_zip()
    {
        using var t = new TempFolder().Write("bad.zip", "this is not a zip");
        var zip = Path.Combine(t.Path, "bad.zip");

        var ex = Assert.Throws<InvalidDataException>(() => SolutionLoader.Load(zip));

        Assert.StartsWith(zip, ex.Message);
    }

    [Fact]
    public void Io_errors_surface_as_InvalidDataException_with_the_path()
    {
        using var t = new TempFolder();
        var zip = Path.Combine(t.Path, "missing.zip");

        var ex = Assert.Throws<InvalidDataException>(() => SolutionLoader.Load(zip));

        Assert.StartsWith(zip, ex.Message);
    }
}