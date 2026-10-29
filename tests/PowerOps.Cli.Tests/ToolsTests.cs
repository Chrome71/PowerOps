using System.Diagnostics;

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
}
