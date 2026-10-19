using System.Text;

namespace PowerOps.Tests.Shared;

public static class Golden
{
    /// <summary>The folder containing PowerOps.slnx, found by walking up from the test output folder.</summary>
    public static string RepoRoot { get; } = FindRepoRoot();

    /// <summary>Compares <paramref name="actual"/> to tests/golden/<paramref name="relativeName"/> (CRLF-insensitive).</summary>
    public static void Verify(string actual, string relativeName)
    {
        var path = Path.Combine(RepoRoot, "tests", "golden", relativeName);
        var normalized = Normalize(actual);

        if (Environment.GetEnvironmentVariable("POWEROPS_UPDATE_GOLDEN") == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, normalized, new UTF8Encoding(false));
            return;
        }

        if (!File.Exists(path))
        {
            throw new Xunit.Sdk.XunitException(
                $"Golden file not found: {path}. Rerun with POWEROPS_UPDATE_GOLDEN=1 to create it.");
        }

        Xunit.Assert.Equal(Normalize(File.ReadAllText(path)), normalized);
    }

    private static string Normalize(string text) => text.Replace("\r\n", "\n");

    private static string FindRepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "PowerOps.slnx")))
            {
                return dir.FullName;
            }
        }

        throw new InvalidOperationException("PowerOps.slnx not found above " + AppContext.BaseDirectory);
    }
}