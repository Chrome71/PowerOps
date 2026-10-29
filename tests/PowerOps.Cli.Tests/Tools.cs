namespace PowerOps.Cli.Tests;

// Programs the script tests need. Bash is the one the tests start: macOS's own /bin/bash (3.2) on every Mac, whatever else is first on PATH.
internal static class Tools
{
    public static readonly string Bash = OperatingSystem.IsMacOS() ? "/bin/bash" : "bash";

    // The first program that is not a file on path (a rooted name is checked as it is), or null.
    public static string? Missing(IEnumerable<string> programs, string? path) =>
        programs.FirstOrDefault(program => !(Path.IsPathRooted(program)
            ? File.Exists(program)
            : (path ?? "").Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries).Any(dir => File.Exists(Path.Combine(dir, program)))));

    public static string FailMessage(string program) => program + " is not on PATH, and POWEROPS_REQUIRE_TOOLS=1 asks for every script test to run.";

    // The script tests skip on Windows by design.
    public static void Require(params string[] programs)
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "The scripts need bash.");
        RequireAnywhere(programs);
    }

    public static void RequireAnywhere(params string[] programs)
    {
        var missing = Missing(programs.Select(program => program == "bash" ? Bash : program), Environment.GetEnvironmentVariable("PATH"));
        if (missing is not null && Environment.GetEnvironmentVariable("POWEROPS_REQUIRE_TOOLS") == "1")
        {
            Assert.Fail(FailMessage(missing));
        }

        Assert.SkipWhen(missing is not null, missing + " is not on PATH.");
    }
}