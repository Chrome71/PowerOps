using System.Diagnostics;

using PowerOps.Tests.Shared;

using YamlDotNet.Serialization;

namespace PowerOps.Cli.Tests;

public sealed class ToolsTests
{
    // The bash every script test starts must be macOS's 3.2, so the scripts are held to what 3.2 lacks.
    [Fact]
    public void Bash_on_macOS_is_version_3_2()
    {
        Assert.SkipUnless(OperatingSystem.IsMacOS(), "Only macOS names a bash to pin.");

        var start = new ProcessStartInfo(Tools.Bash) { RedirectStandardOutput = true };
        start.ArgumentList.Add("-c");
        start.ArgumentList.Add("echo ${BASH_VERSION}");
        using var process = Process.Start(start)!;
        var version = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit();

        Assert.StartsWith("3.2.", version);
    }

    private static string FolderWithPresent()
    {
        var folder = Directory.CreateTempSubdirectory("powerops-tools-").FullName;
        File.WriteAllText(Path.Combine(folder, "present"), "");
        return folder;
    }

    [Fact]
    public void Missing_names_a_program_that_is_not_in_the_folder_used_as_PATH()
    {
        var folder = FolderWithPresent();
        try
        {
            Assert.Equal("absent", Tools.Missing(["present", "absent"], folder));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void Missing_is_null_when_the_file_is_in_the_folder()
    {
        var folder = FolderWithPresent();
        try
        {
            Assert.Null(Tools.Missing(["present"], folder));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void FailMessage_names_the_program_and_the_variable()
    {
        var message = Tools.FailMessage("jq");

        Assert.Contains("jq", message);
        Assert.Contains("POWEROPS_REQUIRE_TOOLS", message);
    }

    // Nothing else notices if the line leaves the test step: the variable is what turns a missing program into a failure in CI.
    [Fact]
    public void The_test_step_of_the_build_job_sets_the_variable_except_on_Windows()
    {
        var ci = new DeserializerBuilder().Build().Deserialize<Dictionary<string, object>>(File.ReadAllText(Path.Combine(Golden.RepoRoot, ".github", "workflows", "ci.yml")));
        var jobs = (Dictionary<object, object>)ci["jobs"];
        var build = (Dictionary<object, object>)jobs["build"];
        var step = ((List<object>)build["steps"]).Cast<Dictionary<object, object>>().Single(s => s.TryGetValue("run", out var run) && (string)run == "dotnet test -c Release --no-build");

        var env = Assert.IsType<Dictionary<object, object>>(step["env"]);
        Assert.Equal("${{ runner.os != 'Windows' && '1' || '' }}", env["POWEROPS_REQUIRE_TOOLS"]);
    }
}