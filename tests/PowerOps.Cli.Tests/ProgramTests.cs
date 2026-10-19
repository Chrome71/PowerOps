using System.Diagnostics;
using System.Reflection;

namespace PowerOps.Cli.Tests;

public class ProgramTests
{
    [Fact]
    public void Version_prints_informational_version_and_exits_zero()
    {
        // The built-in --version reads the entry assembly, which in-process is the test host,
        // so run the real CLI assembly as a child process.
        var cliAssembly = typeof(Program).Assembly;
        var startInfo = new ProcessStartInfo("dotnet")
        {
            ArgumentList = { cliAssembly.Location, "--version" },
            RedirectStandardOutput = true,
        };

        using var process = Process.Start(startInfo)!;
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        var expected = cliAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
        Assert.Equal(0, process.ExitCode);
        Assert.Equal(expected, output.Trim());
    }

    [Fact]
    public void Unknown_command_exits_non_zero()
    {
        var exitCode = Program.Run(["nope"], new StringWriter(), new StringWriter());

        Assert.NotEqual(0, exitCode);
    }
}