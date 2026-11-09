using PowerOps.Core.Tests.Readers;
using PowerOps.Tests.Shared;

namespace PowerOps.Core.Tests;

public class SolutionZipTests
{
    [Fact]
    public void Same_folder_with_other_file_times_gives_the_same_bytes()
    {
        using var temp = new TempFolder();
        temp.Write(Path.Combine("src", "Other", "Solution.xml"), "<ImportExportXml />");
        var file = Path.Combine(temp.Path, "src", "Other", "Solution.xml");
        var first = Path.Combine(temp.Path, "first.zip");
        var second = Path.Combine(temp.Path, "second.zip");

        File.SetLastWriteTimeUtc(file, new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        SolutionZip.Create(Path.Combine(temp.Path, "src"), first);
        File.SetLastWriteTimeUtc(file, new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc));
        SolutionZip.Create(Path.Combine(temp.Path, "src"), second);

        Assert.Equal(File.ReadAllBytes(first), File.ReadAllBytes(second));
    }
}