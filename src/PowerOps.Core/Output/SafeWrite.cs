using System.Security.Cryptography;
using System.Text;

using Microsoft.Win32.SafeHandles;

namespace PowerOps.Core.Output;

/// <summary>Why <see cref="SafeWrite.Refusal"/> will not let a file be written; <see cref="Path"/> is the target, or the folder on the way that is a link.</summary>
public sealed record WriteRefusal(string Path, string Reason)
{
    /// <summary>The line to print after <c>error: </c>, with the path made safe for a terminal and a CI log.</summary>
    public string Text => $"{TextReport.Clean(Path)}: {Reason}";
}

/// <summary>The one place PowerOps writes a file: never through a link, never over a file the command was given as an input, and a regular file is replaced through a temp file in its folder instead of being cut off in place. The read-side twin is <see cref="Readers.BoundedRead"/>.</summary>
public static class SafeWrite
{
    public const string LinkReason = "is a symbolic link; symbolic links are not written.";
    public const string InputReason = "is an input of this command; it is not overwritten.";

    private static readonly UTF8Encoding Utf8 = new(false);

    /// <summary>The refusal for writing <paramref name="path"/>, or null. Opens and creates nothing, so every command calls it first, before it does its work.</summary>
    /// <param name="inputs">The files the user named as inputs; <paramref name="path"/> may not be one of them.</param>
    /// <param name="root">When set, no folder below it on the way to <paramref name="path"/> may be a link (<paramref name="root"/> itself is the user's).</param>
    public static WriteRefusal? Refusal(string path, IEnumerable<string> inputs, string? root = null)
    {
        if (IsTypedName(path))
        {
            return null;
        }

        if (new FileInfo(path).LinkTarget is not null)
        {
            return new WriteRefusal(path, LinkReason);
        }

        var full = Path.GetFullPath(path);
        if (root is not null)
        {
            var top = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
            var folders = new List<string>();
            for (var dir = Path.GetDirectoryName(full); dir is not null && dir.Length > top.Length && dir.StartsWith(top, PathComparison); dir = Path.GetDirectoryName(dir))
            {
                folders.Add(dir);
            }

            // The shallowest link is the one to name: nothing below it means anything.
            foreach (var dir in Enumerable.Reverse(folders))
            {
                if (new DirectoryInfo(dir).LinkTarget is not null)
                {
                    return new WriteRefusal(dir, LinkReason);
                }
            }
        }

        // shortcut: this guards the common spellings (full path, a link on an input's last name, case); 8.3 names, \\?\ paths, Unicode normalization and an alias through a linked folder are not seen. Hard links need no check: the move replaces the directory entry.
        return inputs.Any(input => SameFile(full, input)) ? new WriteRefusal(path, InputReason) : null;
    }

    /// <summary>Writes <paramref name="text"/> as UTF-8 without a byte order mark.</summary>
    /// <exception cref="InvalidDataException">The target changed since <see cref="Refusal"/> (a race), or is a regular file directly under /dev; the message starts with the path.</exception>
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

        if (Refusal(path, inputs, root) is { } refusal)
        {
            throw new InvalidDataException(refusal.Text);
        }

        var full = Path.GetFullPath(path);
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
                // A device or a pipe cannot be given its own length (observed on Linux; read from the runtime source on Windows): write it in place on this handle.
                try
                {
                    RandomAccess.SetLength(handle, RandomAccess.GetLength(handle));
                }
                catch (Exception e) when (e is IOException or NotSupportedException)
                {
                    using var stream = new FileStream(handle, FileAccess.Write, 1);
                    stream.Write(bytes);
                    stream.Flush();
                    return;
                }
            }

            if (IsDirectlyUnderDev(full))
            {
                throw new InvalidDataException($"{path}: a regular file in /dev is not replaced.");
            }

            if (!OperatingSystem.IsWindows())
            {
                mode = File.GetUnixFileMode(full);
            }
        }

        var temp = Path.Combine(Path.GetDirectoryName(full)!, $".powerops-{Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToLowerInvariant()}.tmp");
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

    /// <summary>True when <paramref name="fullPath"/> is a file straight in /dev (not /dev/shm/x); on Unix only.</summary>
    internal static bool IsDirectlyUnderDev(string fullPath) =>
        !OperatingSystem.IsWindows() && string.Equals(Path.GetDirectoryName(fullPath), "/dev", StringComparison.Ordinal);

    // The names a user types for the process's own output; not resolved, so a planted link and /dev/./stdout do not match.
    private static bool IsTypedName(string path) =>
        !OperatingSystem.IsWindows() && (string.Equals(path, "/dev/stdout", StringComparison.Ordinal) || string.Equals(path, "/dev/stderr", StringComparison.Ordinal));

    private static StringComparison PathComparison => OperatingSystem.IsLinux() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

    private static bool SameFile(string fullTarget, string input)
    {
        try
        {
            var fullInput = Path.GetFullPath(input);
            if (string.Equals(fullTarget, fullInput, PathComparison))
            {
                return true;
            }

            return new FileInfo(fullInput).ResolveLinkTarget(true) is { } resolved && string.Equals(fullTarget, resolved.FullName, PathComparison);
        }
        catch (Exception e) when (e is IOException or ArgumentException or NotSupportedException or UnauthorizedAccessException)
        {
            return false;
        }
    }

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