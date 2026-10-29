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
    public void Unknown_command_exits_two()
    {
        var exitCode = Program.Run(["nope"], new StringWriter(), new StringWriter());

        Assert.Equal(2, exitCode);
    }

    [Fact]
    public void An_at_file_argument_is_a_plain_path_not_a_response_file()
    {
        var dir = Directory.CreateTempSubdirectory("powerops-rsp-").FullName;
        try
        {
            var file = Path.Combine(dir, "args.rsp");
            File.WriteAllText(file, "--help\n");
            var (o, e) = (new StringWriter(), new StringWriter());

            var exitCode = Program.Run(["analyze", "@" + file], o, e);

            Assert.Equal(2, exitCode); // the path "@<file>" does not exist; a response file would have printed the help and exited 0
            Assert.DoesNotContain("Usage:", o.ToString());
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}