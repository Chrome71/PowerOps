namespace PowerOps.Cli.Tests;

[Collection("deploy apply")] // the end-to-end test changes the current folder
public class PacRunnerResolveTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("powerops-resolve-").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private string Make(string name, string sub = "", bool exec = true)
    {
        var folder = Path.Combine(_dir, sub);
        Directory.CreateDirectory(folder);
        var file = Path.Combine(folder, name);
        File.WriteAllText(file, "#!/bin/sh\nexit 0\n");
        if (!OperatingSystem.IsWindows() && exec)
        {
            File.SetUnixFileMode(file, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        return file;
    }

    private static string Join(params string[] entries) => string.Join(Path.PathSeparator, entries);

    [Fact]
    public void Empty_relative_and_dot_entries_are_skipped()
    {
        Make("pac", "rel");
        var old = Environment.CurrentDirectory;
        Environment.CurrentDirectory = _dir;
        try
        {
            Assert.Null(PacRunner.Resolve("pac", Join("", ".", "rel", "./rel"), windows: false, pathExt: null));
        }
        finally
        {
            Environment.CurrentDirectory = old;
        }
    }

    [Fact]
    public void An_executable_in_an_absolute_entry_is_picked_and_the_first_hit_wins()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Unix execute bits.");
        var first = Make("pac", "a");
        Make("pac", "b");

        Assert.Equal(first, PacRunner.Resolve("pac", Join(".", Path.Combine(_dir, "a"), Path.Combine(_dir, "b")), windows: false, pathExt: null));
    }

    [Fact]
    public void A_file_without_an_execute_bit_is_skipped_on_Unix()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Unix execute bits.");
        Make("pac", "a", exec: false);
        var second = Make("pac", "b");

        Assert.Equal(second, PacRunner.Resolve("pac", Join(Path.Combine(_dir, "a"), Path.Combine(_dir, "b")), windows: false, pathExt: null));
    }

    [Fact]
    public void On_Windows_PATHEXT_order_decides_and_a_name_with_an_extension_is_tried_as_is()
    {
        Make("pac.cmd", "a");
        var exe = Make("pac.exe", "a");
        var path = Path.Combine(_dir, "a");

        Assert.Equal(exe, PacRunner.Resolve("pac", path, windows: true, pathExt: ".exe;.cmd"));
        Assert.Equal(Path.Combine(path, "pac.cmd"), PacRunner.Resolve("pac.cmd", path, windows: true, pathExt: ".exe"));
        Assert.Equal(Path.Combine(path, "pac.cmd"), PacRunner.Resolve("pac", path, windows: true, pathExt: ".cmd;.exe"));

        var upper = Make("pac.EXE", "b"); // case matters on a Unix file system, so the default list is checked with its own case
        Assert.Equal(upper, PacRunner.Resolve("pac", Path.Combine(_dir, "b"), windows: true, pathExt: null)); // default .COM;.EXE;.BAT;.CMD
    }

    [Fact]
    public void On_Windows_double_quotes_around_a_PATH_entry_are_trimmed()
    {
        var exe = Make("pac.exe", "q");
        var quoted = "\"" + Path.Combine(_dir, "q") + "\"";

        Assert.Equal(exe, PacRunner.Resolve("pac", quoted, windows: true, pathExt: ".exe"));
        Assert.Null(PacRunner.Resolve("pac", quoted, windows: false, pathExt: null));
    }

    [Fact]
    public void A_name_with_a_directory_separator_is_used_as_given()
    {
        var given = Path.Combine("some", "dir", "pac");

        Assert.Equal(given, PacRunner.Resolve(given, "", windows: false, pathExt: null));
    }

    [Fact]
    public void A_pac_in_the_current_folder_but_not_on_PATH_is_not_run()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Needs a POSIX shell.");
        var name = "powerops-planted-" + Guid.NewGuid().ToString("N");
        var marker = Path.Combine(_dir, "ran");
        var script = Make(name);
        File.WriteAllText(script, $"#!/bin/sh\ntouch '{marker}'\n");
        var old = Environment.CurrentDirectory;
        Environment.CurrentDirectory = _dir;
        try
        {
            var result = new PacRunner(name).Run([]);

            Assert.False(result.Started);
            Assert.False(File.Exists(marker));
        }
        finally
        {
            Environment.CurrentDirectory = old;
        }
    }
}