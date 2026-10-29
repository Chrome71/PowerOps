using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;

using PowerOps.Core.Output;
using PowerOps.Tests.Shared;

namespace PowerOps.Core.Tests.Output;

public class SafeWriteTests : IDisposable
{
    private readonly string _temp = Directory.CreateTempSubdirectory("powerops-safewrite-").FullName;

    public void Dispose()
    {
        if (!OperatingSystem.IsWindows())
        {
            foreach (var dir in Directory.GetDirectories(_temp))
            {
                File.SetUnixFileMode(dir, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
        }

        Directory.Delete(_temp, recursive: true);
    }

    private string Path_(string name) => Path.Combine(_temp, name);

    private string Put(string name, string content)
    {
        var path = Path_(name);
        File.WriteAllText(path, content);
        return path;
    }

    private static IEnumerable<string> Temps(string folder) => Directory.GetFiles(folder, ".powerops-*.tmp");

    private static void NotWindows() => Assert.SkipWhen(OperatingSystem.IsWindows(), "Unix modes, devices and pipes.");

    [Fact]
    public void A_new_file_is_written_as_utf8_without_a_byte_order_mark_and_leaves_no_temp_file()
    {
        var path = Path_("new.json");

        SafeWrite.WriteText(path, "{\"a\":\"é\"}\n", []);

        Assert.Equal(Encoding.UTF8.GetBytes("{\"a\":\"é\"}\n"), File.ReadAllBytes(path));
        Assert.Empty(Temps(_temp));
    }

    [Fact]
    public void An_existing_file_is_replaced()
    {
        var path = Put("out.json", "old old old old");

        SafeWrite.WriteText(path, "new", []);

        Assert.Equal("new", File.ReadAllText(path));
        Assert.Empty(Temps(_temp));
    }

    [Fact]
    [UnsupportedOSPlatform("windows")]
    public void The_unix_mode_of_an_existing_file_is_kept()
    {
        NotWindows();
        var path = Put("out.json", "old");
        File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead);

        SafeWrite.WriteText(path, "new", []);

        Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead, File.GetUnixFileMode(path));
    }

    [Fact]
    [UnsupportedOSPlatform("windows")]
    public void The_temp_file_is_never_wider_than_the_target()
    {
        NotWindows();
        var path = Put("out.json", "old");
        File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        UnixFileMode? seen = null;

        SafeWrite.WriteText(path, "new", [], null, temp => seen = File.GetUnixFileMode(temp));

        Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, seen);
    }

    [Fact]
    [UnsupportedOSPlatform("windows")]
    public void A_failed_move_leaves_the_target_whole_removes_the_temp_file_and_names_the_target()
    {
        NotWindows();
        var path = Put("out.json", "old");
        File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead);

        var error = Assert.ThrowsAny<IOException>(() => SafeWrite.WriteText(path, "new", [], null, File.Delete));

        Assert.Contains(path, error.Message);
        Assert.DoesNotContain(".powerops-", error.Message);
        Assert.Equal("old", File.ReadAllText(path));
        Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead, File.GetUnixFileMode(path));
        Assert.Empty(Temps(_temp));
    }

    [Fact]
    [UnsupportedOSPlatform("windows")]
    public void A_new_file_in_a_read_only_folder_fails_with_a_message_that_names_the_target()
    {
        NotWindows();
        var folder = Directory.CreateDirectory(Path_("ro")).FullName;
        File.SetUnixFileMode(folder, UnixFileMode.UserRead | UnixFileMode.UserExecute);
        Assert.SkipWhen(CanCreateIn(folder), "This user can write there anyway.");
        var path = System.IO.Path.Combine(folder, "out.json");

        var error = Assert.Throws<UnauthorizedAccessException>(() => SafeWrite.WriteText(path, "new", []));

        Assert.Contains(path, error.Message);
        Assert.DoesNotContain(".powerops-", error.Message);
    }

    [Fact]
    [UnsupportedOSPlatform("windows")]
    public void A_read_only_file_is_not_replaced()
    {
        NotWindows();
        var path = Put("out.json", "old");
        File.SetUnixFileMode(path, UnixFileMode.UserRead);
        Assert.SkipWhen(CanWrite(path), "This user can write there anyway.");

        Assert.Throws<UnauthorizedAccessException>(() => SafeWrite.WriteText(path, "new", []));

        Assert.Equal("old", File.ReadAllText(path));
        Assert.Empty(Temps(_temp));
    }

    // Each row plants a link at report.json that leads to victim.txt (or to nothing); none of them may be followed or touched.
    [Theory]
    [InlineData("file")]
    [InlineData("dangling")]
    [InlineData("loop")]
    [InlineData("folder")]
    [InlineData("proc-root")]
    [InlineData("proc-cwd")]
    [InlineData("dev-shm")]
    public void A_link_at_the_target_is_refused_before_anything_is_opened_and_not_followed(string kind)
    {
        Assert.SkipWhen(kind is "proc-root" or "proc-cwd" or "dev-shm" && !OperatingSystem.IsLinux(), "Linux only.");
        var victim = Put("victim.txt", "victim");
        var folder = Directory.CreateDirectory(Path_("dir")).FullName;
        var link = Path_("report.json");
        var absolute = victim.TrimStart(System.IO.Path.DirectorySeparatorChar);
        switch (kind)
        {
            case "file":
                Links.Create(link, victim);
                break;
            case "dangling":
                Links.Create(link, Path_("missing.txt"));
                break;
            case "loop":
                Links.Create(Path_("other.json"), link);
                Links.Create(link, Path_("other.json"));
                break;
            case "folder":
                Links.Create(link, folder, folder: true);
                break;
            case "proc-root":
                Links.Create(link, "/proc/self/root" + victim);
                break;
            case "proc-cwd":
                Links.Create(link, "/proc/self/cwd/" + System.IO.Path.GetRelativePath(Environment.CurrentDirectory, victim));
                break;
            case "dev-shm":
                Links.Create(link, "/dev/shm/../../" + absolute);
                break;
        }

        var linkTarget = new FileInfo(link).LinkTarget;

        Assert.Equal(new WriteRefusal(link, SafeWrite.LinkReason), SafeWrite.Refusal(link, []));
        var error = Assert.Throws<InvalidDataException>(() => SafeWrite.WriteText(link, "new", []));
        Assert.Equal($"{link}: {SafeWrite.LinkReason}", error.Message);
        Assert.Equal("victim", File.ReadAllText(victim));
        Assert.Equal(linkTarget, new FileInfo(link).LinkTarget);
        Assert.Empty(Temps(_temp));
    }

    [Fact]
    public void A_link_on_the_way_is_refused_with_a_root_and_written_through_without_one()
    {
        var real = Directory.CreateDirectory(Path_("real")).FullName;
        var through = Path_("linked");
        Links.Create(through, real, folder: true);
        var target = System.IO.Path.Combine(through, "out.json");

        Assert.Equal(new WriteRefusal(through, SafeWrite.LinkReason), SafeWrite.Refusal(target, [], _temp));
        Assert.Null(SafeWrite.Refusal(target, []));
        SafeWrite.WriteText(target, "through", []);

        Assert.Equal("through", File.ReadAllText(System.IO.Path.Combine(real, "out.json")));
    }

    [Fact]
    public void The_root_itself_may_be_a_link()
    {
        var real = Directory.CreateDirectory(Path_("real")).FullName;
        var root = Path_("linked");
        Links.Create(root, real, folder: true);

        Assert.Null(SafeWrite.Refusal(System.IO.Path.Combine(root, "out.json"), [], root + System.IO.Path.DirectorySeparatorChar));
    }

    [Theory]
    [InlineData("same")]
    [InlineData("spelling")]
    [InlineData("link")]
    public void A_named_input_is_refused(string kind)
    {
        var path = Put("in.zip", "zip bytes");
        var input = kind switch
        {
            "same" => path,
            "spelling" => System.IO.Path.Combine(_temp, "sub", "..", ".", "in.zip"),
            _ => LinkTo(path),
        };

        Assert.Equal(new WriteRefusal(path, SafeWrite.InputReason), SafeWrite.Refusal(path, [input]));
    }

    [Fact]
    public void WriteText_over_an_input_throws_with_the_path_first_and_leaves_the_bytes()
    {
        var path = Put("in.zip", "zip bytes");

        var error = Assert.Throws<InvalidDataException>(() => SafeWrite.WriteText(path, "new", [path]));

        Assert.Equal($"{path}: {SafeWrite.InputReason}", error.Message);
        Assert.Equal("zip bytes", File.ReadAllText(path));
        Assert.Empty(Temps(_temp));
    }

    [Fact]
    public void An_input_that_is_a_link_loop_is_no_match_and_the_write_goes_ahead()
    {
        var path = Path_("out.json");
        var a = Path_("a.zip");
        var b = Path_("b.zip");
        Links.Create(a, b);
        Links.Create(b, a);

        SafeWrite.WriteText(path, "new", [a]);

        Assert.Equal("new", File.ReadAllText(path));
    }

    [Fact]
    public void Another_file_in_the_same_folder_is_not_an_input()
    {
        var path = Put("out.json", "old");
        var other = Put("in.zip", "zip");

        Assert.Null(SafeWrite.Refusal(path, [other]));
    }

    [Fact]
    public void A_difference_in_case_is_an_input_on_windows_and_macos_and_not_on_linux()
    {
        var path = Put("out.json", "old");
        var input = Path_("OUT.JSON");

        Assert.Equal(!OperatingSystem.IsLinux(), SafeWrite.Refusal(path, [input]) is not null);
    }

    [Fact]
    [UnsupportedOSPlatform("windows")]
    public void A_link_to_dev_null_is_refused_and_the_typed_name_dev_stdout_is_not()
    {
        NotWindows();
        var link = Path_("null.json");
        Links.Create(link, "/dev/null");

        Assert.Equal(new WriteRefusal(link, SafeWrite.LinkReason), SafeWrite.Refusal(link, []));
        Assert.Null(SafeWrite.Refusal("/dev/stdout", []));
        Assert.Null(SafeWrite.Refusal("/dev/stderr", []));
    }

    [Fact]
    [UnsupportedOSPlatform("windows")]
    public void Another_spelling_of_dev_stdout_is_a_link_and_refused()
    {
        NotWindows();

        Assert.Equal(new WriteRefusal("/dev/./stdout", SafeWrite.LinkReason), SafeWrite.Refusal("/dev/./stdout", []));
    }

    [Fact]
    [UnsupportedOSPlatform("windows")]
    public void Dev_null_is_written_in_place()
    {
        NotWindows();

        SafeWrite.WriteText("/dev/null", "text", []);

        Assert.Empty(Temps("/dev"));
    }

    [Fact]
    [UnsupportedOSPlatform("windows")]
    public async Task A_fifo_named_directly_is_written_in_place_and_a_reader_gets_the_whole_text()
    {
        NotWindows();
        var fifo = Path_("pipe");
        using (var mkfifo = Process.Start(new ProcessStartInfo("mkfifo") { ArgumentList = { fifo }, RedirectStandardError = true }) ?? throw new InvalidOperationException("mkfifo did not start."))
        {
            await mkfifo.WaitForExitAsync(TestContext.Current.CancellationToken);
            Assert.SkipWhen(mkfifo.ExitCode != 0, "mkfifo is not available here.");
        }

        var reader = Task.Run(() => File.ReadAllText(fifo), TestContext.Current.CancellationToken);
        SafeWrite.WriteText(fifo, "through the pipe\n", []);

        Assert.Equal("through the pipe\n", await reader.WaitAsync(TimeSpan.FromSeconds(20), TestContext.Current.CancellationToken));
        Assert.Empty(Temps(_temp));
    }

    [Fact]
    public void A_link_swapped_in_after_the_check_is_replaced_and_not_followed()
    {
        var victim = Put("victim.txt", "victim");
        var path = Put("out.json", "old");

        SafeWrite.WriteText(path, "new", [], null, _ =>
        {
            File.Delete(path);
            Links.Create(path, victim);
        });

        Assert.Equal("victim", File.ReadAllText(victim));
        Assert.Null(new FileInfo(path).LinkTarget);
        Assert.Equal("new", File.ReadAllText(path));
    }

    [Fact]
    [UnsupportedOSPlatform("windows")]
    public void IsDirectlyUnderDev_is_true_for_a_file_in_dev_and_false_one_level_down()
    {
        NotWindows();

        Assert.True(SafeWrite.IsDirectlyUnderDev("/dev/null"));
        Assert.False(SafeWrite.IsDirectlyUnderDev("/dev/shm/x"));
    }

    private string LinkTo(string target)
    {
        var link = Path_("in-link.zip");
        Links.Create(link, target);
        return link;
    }

    private static bool CanCreateIn(string folder)
    {
        try
        {
            File.Delete(Path.Combine(folder, Path.GetRandomFileName()));
            using var stream = new FileStream(Path.Combine(folder, ".probe"), FileMode.CreateNew);
            stream.Dispose();
            File.Delete(Path.Combine(folder, ".probe"));
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static bool CanWrite(string file)
    {
        try
        {
            File.OpenHandle(file, FileMode.Open, FileAccess.Write).Dispose();
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}