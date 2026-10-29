namespace PowerOps.Tests.Shared;

public static class Links
{
    /// <summary>Makes a symbolic link at <paramref name="link"/> to <paramref name="target"/>. Where it cannot be made, the test skips on Windows (the runner may not allow it) and fails on Linux and macOS, where a link can always be made and a skip would hide a missing check.</summary>
    public static void Create(string link, string target, bool folder = false)
    {
        try
        {
            if (folder)
            {
                Directory.CreateSymbolicLink(link, target);
            }
            else
            {
                File.CreateSymbolicLink(link, target);
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            Assert.SkipWhen(OperatingSystem.IsWindows(), "Symbolic links are not available here.");
            Assert.Fail($"A symbolic link could not be made at {link}: {e.Message}");
        }
    }
}