using PowerOps.Core.Readers;

namespace PowerOps.Cli.Tests;

// Serial, because the test lowers a cap that is a static field.
[Collection("deploy apply")]
public sealed class FolderCapCommandTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("powerops-foldercap-").FullName;

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    public void Analyze_diff_and_test_exit_3_with_the_path_first_when_discovery_walks_too_many_folders()
    {
        // Three folders: the root, a and b.
        Directory.CreateDirectory(Path.Combine(_root, "a"));
        Directory.CreateDirectory(Path.Combine(_root, "b"));
        File.WriteAllText(Path.Combine(_root, "t.flowtest.json"), "{}");
        var commands = new[]
        {
            new[] { "analyze", _root },
            ["diff", _root, _root],
            ["test", _root],
        };

        var saved = SolutionDiscovery.MaxFolders;
        SolutionDiscovery.MaxFolders = 2;
        try
        {
            foreach (var args in commands)
            {
                var o = new StringWriter();
                var e = new StringWriter();

                var code = Program.Run(args, o, e);

                Assert.True(code == 3, $"{args[0]} exited {code}");
                Assert.StartsWith($"error: {_root}", e.ToString().Replace("\r", "").Split('\n')[0]);
                Assert.Contains("more than 2 folders", e.ToString());
            }
        }
        finally
        {
            SolutionDiscovery.MaxFolders = saved;
        }
    }
}