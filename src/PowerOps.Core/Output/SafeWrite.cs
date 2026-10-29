using System.Security.Cryptography;
using System.Text;

using Microsoft.Win32.SafeHandles;

namespace PowerOps.Core.Output;

/// <summary>Why <see cref="SafeWrite.Refusal"/> will not let a file be written; <see cref="Path"/> is the target, or the folder on the way that is a link.</summary>
public sealed record WriteRefusal(string Path, string Reason)
{
    /// <summary>The line to print after <c>error: </c>, with the path made safe for a terminal and a CI log.</summary>
    public string Text => Path.Length == 0 ? $"the file name {Reason}" : $"{TextReport.Clean(Path)}: {Reason}";
}

/// <summary>The one place PowerOps writes a file: never through a link, never over a file the command was given as an input, and a regular file is replaced through a temp file in its folder instead of being cut off in place. The read-side twin is <see cref="Readers.BoundedRead"/>.</summary>
public static class SafeWrite
{
    public const string LinkReason = "is a symbolic link; symbolic links are not written.";
    public const string InputReason = "is an input of this command; it is not overwritten.";
    public const string EmptyReason = "is empty; name a file.";
    public const string FolderReason = "is a folder; name a file in it.";
    public const string LoopReason = "has a link loop or more than 40 links on the way; nothing is written.";

    private static readonly UTF8Encoding Utf8 = new(false);

    /// <summary>The refusal for writing <paramref name="path"/>, or null. Opens and creates nothing, so every command calls it first, before it does its work.</summary>
    /// <param name="inputs">The files the user named as inputs; <paramref name="path"/> may not be one of them.</param>
    /// <param name="root">When set, no folder below it on the way to <paramref name="path"/> may be a link (<paramref name="root"/> itself is the user's).</param>
    public static WriteRefusal? Refusal(string path, IEnumerable<string> inputs, string? root = null) => Check(path, inputs, root, out _);

    // full is the one path to open when the result is null: the target resolved through every link on the way, with no "." or ".." left, so the kernel opens the entry that was checked.
    private static WriteRefusal? Check(string path, IEnumerable<string> inputs, string? root, out string full)
    {
        full = path;
        if (path.Length == 0)
        {
            return new WriteRefusal(path, EmptyReason);
        }

        if (IsTypedName(path))
        {
            return null;
        }

        if (Resolve(path) is not { } resolved)
        {
            return new WriteRefusal(path, LoopReason);
        }

        full = resolved;
        if (new FileInfo(full).LinkTarget is not null)
        {
            return new WriteRefusal(path, LinkReason);
        }

        // After the link check, so a link to a folder is still named a link.
        if (Directory.Exists(full))
        {
            return new WriteRefusal(path, FolderReason);
        }

        if (root is not null)
        {
            // The user's own strings, so the kernel resolves any ".." in root the way Resolve does. The shallowest link is the one to name: nothing below it means anything.
            var dir = root;
            foreach (var part in Parts(Path.GetRelativePath(root, path)).SkipLast(1))
            {
                dir = Path.Combine(dir, part);
                if (new FileInfo(dir).LinkTarget is not null)
                {
                    return new WriteRefusal(dir, LinkReason);
                }
            }
        }

        // shortcut: target and inputs are resolved through every link on the way, but 8.3 names, \\?\ paths and Unicode normalization are not seen, and a link swapped in after this check needs write access to the folder. Hard links need no check: the move replaces the directory entry.
        // An input that cannot be resolved (a loop, more than 40 links) fails closed: it might be the target, and a refusal is exit 2 where a throw would be exit 3.
        foreach (var input in inputs)
        {
            if (RealPath(input, followLast: true) is not { } other)
            {
                return new WriteRefusal(input, LoopReason);
            }

            if (string.Equals(full, other, PathComparison))
            {
                return new WriteRefusal(path, InputReason);
            }
        }

        return null;
    }

    /// <summary>Writes <paramref name="text"/> as UTF-8 without a byte order mark.</summary>
    /// <exception cref="InvalidDataException">The target changed since <see cref="Refusal"/> (a race); the message starts with the path.</exception>
    /// <exception cref="IOException">As <see cref="File.WriteAllText(string, string)"/>; the message names the target, never the temp file.</exception>
    public static void WriteText(string path, string text, IEnumerable<string> inputs, string? root = null) => WriteText(path, text, inputs, root, null);

    // beforeMove is called with the temp file's path, closed and still with the mode it was created with, for the tests.
    internal static void WriteText(string path, string text, IEnumerable<string> inputs, string? root, Action<string>? beforeMove)
    {
        var bytes = Utf8.GetBytes(text);
        if (IsTypedName(path))
        {
            File.WriteAllBytes(path, bytes);
            return;
        }

        if (Check(path, inputs, root, out var full) is { } refusal)
        {
            throw new InvalidDataException(refusal.Text);
        }

        SafeFileHandle? handle = null;
        try
        {
            handle = File.OpenHandle(full, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
        }
        catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException)
        {
        }

        UnixFileMode? mode = null;
        if (handle is not null)
        {
            using (handle)
            {
                // A file straight in /dev is a device: macOS devfs answers the length probe for /dev/null as if it were a regular file, and a folder of devices holds no regular files. Write it in place, with no probe.
                if (IsDirectlyUnderDev(full))
                {
                    using var device = new FileStream(handle, FileAccess.Write, 1);
                    device.Write(bytes);
                    device.Flush();
                    return;
                }

                // A device or a pipe cannot be given its own length (observed on Linux; read from the runtime source on Windows): write it in place on this handle.
                // An IOException on anything else (a regular file on a filesystem that refuses it) is rethrown: written in place it would keep a stale tail.
                try
                {
                    RandomAccess.SetLength(handle, RandomAccess.GetLength(handle));
                }
                catch (Exception e) when (e is IOException or NotSupportedException)
                {
                    using var stream = new FileStream(handle, FileAccess.Write, 1);
                    if (e is not NotSupportedException && stream.CanSeek && !IsInDev(full))
                    {
                        throw;
                    }

                    stream.Write(bytes);
                    stream.Flush();
                    return;
                }
            }

            if (!OperatingSystem.IsWindows())
            {
                mode = File.GetUnixFileMode(full);
            }
        }

        var temp = TempName(full);
        var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.None };
        if (mode is { } createMode && !OperatingSystem.IsWindows())
        {
            options.UnixCreateMode = createMode;
        }

        FileStream temporary;
        try
        {
            temporary = new FileStream(temp, options);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            throw Rename(e, temp, full);
        }

        var moved = false;
        try
        {
            using (temporary)
            {
                temporary.Write(bytes);
                temporary.Flush(true);
            }

            beforeMove?.Invoke(temp);
            if (mode is { } target && !OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(temp, target);
            }

            File.Move(temp, full, overwrite: true);
            moved = true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            throw Rename(e, temp, full);
        }
        finally
        {
            if (!moved)
            {
                try
                {
                    File.Delete(temp);
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                {
                }
            }
        }
    }

    /// <summary>True when a file can be created in the folder of <paramref name="path"/>: makes a temp file there and deletes it. Prints and throws nothing, so a command can check before it does work whose report it writes last.</summary>
    public static bool CanCreateBeside(string path)
    {
        try
        {
            // A typed name and a device are written in place, with no temp file.
            // shortcut: a pipe outside /dev in a folder that takes no new file is refused too, though it would be written in place; probe it on its own handle if that is ever needed.
            if (IsTypedName(path))
            {
                return true;
            }

            if (Resolve(path) is not { } full)
            {
                return false;
            }

            if (IsInDev(full))
            {
                return true;
            }

            var temp = TempName(full);
            new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None).Dispose();
            File.Delete(temp);
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static string TempName(string fullPath) =>
        Path.Combine(Path.GetDirectoryName(fullPath)!, $".powerops-{Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToLowerInvariant()}.tmp");

    // /dev/null can seek, so the length probe alone does not tell it from a regular file.
    private static bool IsInDev(string fullPath) => !OperatingSystem.IsWindows() && fullPath.StartsWith("/dev/", StringComparison.Ordinal);

    /// <summary>True when <paramref name="fullPath"/> is a file straight in /dev (not /dev/shm/x); on Unix only.</summary>
    internal static bool IsDirectlyUnderDev(string fullPath) =>
        !OperatingSystem.IsWindows() && string.Equals(Path.GetDirectoryName(fullPath), "/dev", StringComparison.Ordinal);

    // The names a user types for the process's own output; not resolved, so a planted link and /dev/./stdout do not match.
    private static bool IsTypedName(string path) =>
        !OperatingSystem.IsWindows() && (string.Equals(path, "/dev/stdout", StringComparison.Ordinal) || string.Equals(path, "/dev/stderr", StringComparison.Ordinal));

    private static StringComparison PathComparison => OperatingSystem.IsLinux() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

    // A write target: the last name is kept as it is, so a link there is seen by the caller and never followed.
    private static string? Resolve(string path) => RealPath(path, followLast: false);

    // Resolves path one component at a time through every link on the way, like realpath(3); null when it cannot (a link loop, more than 40 links, an unreadable folder).
    // On Unix the start is not GetFullPath, which would collapse ".." before a link is followed: after a link, ".." is the link target's parent. On Windows Win32 collapses ".." itself, so GetFullPath is the same view.
    // The result has no link in its folders and no "." or "..", so opening it opens the entry that was checked.
    private static string? RealPath(string path, bool followLast)
    {
        try
        {
            var start = OperatingSystem.IsWindows() ? Path.GetFullPath(path) : Path.Combine(Directory.GetCurrentDirectory(), path);
            var current = Path.GetPathRoot(start)!;
            var rest = new Stack<string>(Enumerable.Reverse(Parts(start[current.Length..])));
            for (var hops = 0; rest.Count > 0;)
            {
                var name = rest.Pop();
                if (name == ".")
                {
                    continue;
                }

                if (name == "..")
                {
                    current = Path.GetDirectoryName(current) ?? current;
                    continue;
                }

                var next = Path.Combine(current, name);
                if (!followLast && rest.Count == 0)
                {
                    current = next;
                    continue;
                }

                if (new FileInfo(next).LinkTarget is not { } link)
                {
                    current = next;
                    continue;
                }

                if (++hops > 40)
                {
                    return null;
                }

                if (OperatingSystem.IsWindows())
                {
                    // A target such as "\\foo" or "C:foo" keeps its drive.
                    var abs = Path.GetFullPath(link, current);
                    current = Path.GetPathRoot(abs)!;
                    link = abs[current.Length..];
                }
                else if (Path.IsPathRooted(link))
                {
                    current = Path.GetPathRoot(link)!;
                    link = link[current.Length..];
                }

                foreach (var part in Enumerable.Reverse(Parts(link)))
                {
                    rest.Push(part);
                }
            }

            return current;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return null;
        }
    }

    private static string[] Parts(string path) => path.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);

    // The same kind of exception, the temp file's name replaced by the target's, the original kept inside.
    private static Exception Rename(Exception e, string temp, string target)
    {
        var message = e.Message.Replace(temp, target, StringComparison.Ordinal);
        return e switch
        {
            DirectoryNotFoundException => new DirectoryNotFoundException(message, e),
            FileNotFoundException => new FileNotFoundException(message, e),
            PathTooLongException => new PathTooLongException(message, e),
            UnauthorizedAccessException => new UnauthorizedAccessException(message, e),
            _ => new IOException(message, e),
        };
    }
}