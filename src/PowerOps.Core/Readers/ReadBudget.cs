namespace PowerOps.Core.Readers;

/// <summary>Limits for reading one solution; solutions are untrusted, so a zip bomb or a flood of tiny files must not exhaust memory or time.</summary>
internal sealed class ReadBudget(
    long maxTotalBytes = 256L * 1024 * 1024,
    long maxFileBytes = 50L * 1024 * 1024,
    long maxNodes = 5_000_000,
    int maxEntries = 100_000,
    int maxFiles = 100_000)
{
    private long _remaining = maxTotalBytes;
    private long _nodesLeft = maxNodes;

    public long MaxFileBytes { get; } = maxFileBytes;

    public long MaxTotalBytes { get; } = maxTotalBytes;

    public long MaxNodes { get; } = maxNodes;

    public int MaxEntries { get; } = maxEntries;

    public int MaxFiles { get; } = maxFiles;

    /// <summary>Counts one XML node against the solution's budget.</summary>
    public void CountNode(string name)
    {
        if (--_nodesLeft < 0)
        {
            throw new InvalidDataException($"{name}: solution has more than {MaxNodes} XML nodes.");
        }
    }

    /// <summary>Passes <paramref name="files"/> through and refuses the one past <see cref="MaxFiles"/>.</summary>
    public IEnumerable<string> LimitFiles(IEnumerable<string> files, string folder)
    {
        var count = 0;
        foreach (var f in files)
        {
            if (++count > MaxFiles)
            {
                throw new InvalidDataException($"{folder}: more than {MaxFiles} files.");
            }

            yield return f;
        }
    }

    public void Consume(long bytes, string name)
    {
        _remaining -= bytes;
        if (_remaining < 0)
        {
            throw new InvalidDataException($"{name}: solution is larger than {MaxTotalBytes / (1024 * 1024)} MB when read.");
        }
    }
}