using System.ComponentModel;
using System.Diagnostics;

namespace PowerOps.Cli;

public sealed record PacResult(bool Started, bool TimedOut, int ExitCode, string Stdout);

// Runs pac as a subprocess. Arguments go through ArgumentList, never a shell string.
public sealed class PacRunner(string executable = "pac", TimeSpan? timeout = null)
{
    private readonly TimeSpan _timeout = timeout ?? TimeSpan.FromSeconds(30);

    public PacResult Run(IReadOnlyList<string> args)
    {
        var info = new ProcessStartInfo(executable)
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
            var stdout = process.StandardOutput.ReadToEndAsync();
            _ = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(_timeout))
            {
                process.Kill(entireProcessTree: true);
                return new PacResult(true, true, 0, "");
            }

            return new PacResult(true, false, process.ExitCode, stdout.GetAwaiter().GetResult());
        }
    }
}