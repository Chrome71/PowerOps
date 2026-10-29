using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Nodes;

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

    // M33 F-4: no arguments at all prints the root help on stderr, then the error line; stdout stays empty.
    [Fact]
    public void A_bare_powerops_prints_the_help_then_the_error_line_on_stderr_and_exits_2()
    {
        var (o, e) = (new StringWriter(), new StringWriter());

        var code = Program.Run([], o, e);

        Assert.Equal(2, code);
        Assert.Equal("", o.ToString());
        var lines = e.ToString().ReplaceLineEndings("\n").TrimEnd('\n').Split('\n');
        Assert.Contains(lines, l => l.StartsWith("Usage:", StringComparison.Ordinal));
        Assert.Equal("error: Required command was not provided.", lines[^1]);
    }

    // Any other parse error keeps the one error line.
    [Fact]
    public void Another_parse_error_prints_no_help()
    {
        var (o, e) = (new StringWriter(), new StringWriter());

        var code = Program.Run(["--bogus"], o, e);

        Assert.Equal(2, code);
        Assert.DoesNotContain("Usage:", e.ToString());
    }

    // `deploy gate` as a hook runs it: the built assembly as a child process with the hook input on stdin.
    private static (int Code, string Out, string Err) Gate(string stdin, string planFile)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            ArgumentList = { typeof(Program).Assembly.Location, "deploy", "gate", planFile },
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        using var process = Process.Start(startInfo)!;
        process.StandardInput.Write(stdin);
        process.StandardInput.Close();
        var error = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, output, error.Result.ReplaceLineEndings("\n"));
    }

    private static string Hook(string command) => new JsonObject { ["tool_name"] = "Bash", ["tool_input"] = new JsonObject { ["command"] = command } }.ToJsonString();

    [Fact]
    public void The_gate_allows_a_call_that_is_not_an_import_with_no_output()
    {
        Assert.Equal((0, "", ""), Gate(Hook("git status"), "no-such-plan.json"));
    }

    [Fact]
    public void The_gate_blocks_an_import_whose_plan_file_is_missing_with_the_dry_runs_line_and_the_guidance()
    {
        var dir = Directory.CreateTempSubdirectory("powerops-gate-").FullName;
        try
        {
            var zip = Path.Combine(dir, "Seed.zip");
            File.WriteAllText(zip, "not a real zip");

            var (code, output, error) = Gate(Hook($"pac solution import --path '{zip}' --environment 00000000-0000-0000-0000-000000000001"), Path.Combine(dir, "powerops-plan.json"));

            Assert.Equal(2, code);
            Assert.Equal("", output);
            Assert.Contains("error: the plan file does not exist\n", error);
            Assert.EndsWith("Do not retry this import another way.\n", error);
            Assert.DoesNotContain(dir, error);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void The_gate_blocks_empty_stdin()
    {
        var (code, output, error) = Gate("", "no-such-plan.json");

        Assert.Equal(2, code);
        Assert.Equal("", output);
        Assert.StartsWith("error: deploy gate: the hook input is not JSON\n", error);
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