using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

public class GoldenTests
{
    [Fact]
    public void Verify_ignores_line_ending_differences()
    {
        Golden.Verify("golden self-test\r\nline two\r\n", "selftest.txt");
    }

    [Fact]
    public void Verify_fails_when_content_differs()
    {
        Assert.ThrowsAny<Exception>(() => Golden.Verify("different", "selftest.txt"));
    }
}