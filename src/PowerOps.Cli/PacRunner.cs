using System.ComponentModel;
using System.Diagnostics;

namespace PowerOps.Cli;

public sealed record PacResult(bool Started, bool TimedOut, int ExitCode, string Stdout, string Stderr = "");

// Runs pac as a subprocess. Arguments go through ArgumentList, never a shell string.
public sealed class PacRunner(string executable = "pac", TimeSpan? timeout = null)
{
    internal const long MaxOutput = 16 * 1024 * 1024;
    internal const string TooLarge = "the output was over 16 MB; the process was stopped";

    private readonly TimeSpan _timeout = timeout ?? TimeSpan.FromSeconds(30);

    public PacResult Run(IReadOnlyList<string> args)
    {
        var file = Resolve(executable, Environment.GetEnvironmentVariable("PATH"), OperatingSystem.IsWindows(), Environment.GetEnvironmentVariable("PATHEXT"));
        if (file is null)
        {
            return new PacResult(false, false, 0, "");
        }

        var info = new ProcessStartInfo(file)
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        args.ToList().ForEach(info.ArgumentList.Add);

        Process process;
        try
        {
            process = Process.Start(info)!;
        }
        catch (Win32Exception)
        {
            return new PacResult(false, false, 0, "");
        }

        using (process)
        {
            process.StandardInput.Close();
            var clock = Stopwatch.StartNew();
            var tooLarge = false;
            var stdout = Capture(process.StandardOutput);
            var stderr = Capture(process.StandardError);
            // The timeout covers the exit and both stream reads: a child that inherits the pipes can keep them open after pac exits.
            if (!process.WaitForExit(_timeout) || !Task.WaitAll([stdout, stderr], Remaining(clock)))
            {
                Kill();
                return new PacResult(true, true, 0, "");
            }

            // A stream past the cap killed the process, so the exit code is not pac's own.
            return tooLarge ? new PacResult(true, false, -1, "", TooLarge) : new PacResult(true, false, process.ExitCode, stdout.Result, stderr.Result);

            void Kill()
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException)
                {
                    // The process exited between the wait and the kill.
                }
            }

            async Task<string> Capture(StreamReader reader)
            {
                using var bytes = new MemoryStream();
                var buffer = new byte[81920];
                int n;
                while ((n = await reader.BaseStream.ReadAsync(buffer)) > 0)
                {
                    if (bytes.Length + n > MaxOutput)
                    {
                        tooLarge = true;
                        Kill();
                        return "";
                    }

                    bytes.Write(buffer, 0, n);
                }

                return reader.CurrentEncoding.GetString(bytes.GetBuffer(), 0, (int)bytes.Length);
            }
        }
    }

    // Returns null when a bare name is not found. A bare name is looked up in PATH's absolute entries only: .NET checks the current folder first on Unix, so a cloned repo could plant its own pac.
    internal static string? Resolve(string name, string? path, bool windows, string? pathExt)
    {
        if (name.Contains(Path.DirectorySeparatorChar) || name.Contains(Path.AltDirectorySeparatorChar))
        {
            return name;
        }

        var names = windows && Path.GetExtension(name).Length == 0
            ? (string.IsNullOrEmpty(pathExt) ? ".COM;.EXE;.BAT;.CMD" : pathExt).Split(';', StringSplitOptions.RemoveEmptyEntries).Select(ext => name + ext)
            : [name];
        // shortcut: any execute bit counts (not only the current user's), and a PATHEXT entry such as .VBS can be picked; both fail closed as "not found". Upgrade if a user reports pac not found while it is on PATH.
        foreach (var dir in (path ?? "").Split(Path.PathSeparator).Select(d => windows ? d.Trim('"') : d).Where(Path.IsPathFullyQualified))
        {
            foreach (var candidate in names.Select(n => Path.Combine(dir, n)))
            {
                if (File.Exists(candidate) && (windows || IsExecutable(candidate)))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private static bool IsExecutable(string file) =>
        OperatingSystem.IsWindows() || (File.GetUnixFileMode(file) & (UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute)) != 0;

    private TimeSpan Remaining(Stopwatch clock) => _timeout > clock.Elapsed ? _timeout - clock.Elapsed : TimeSpan.Zero;
}