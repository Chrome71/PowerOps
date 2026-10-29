namespace PowerOps.Core.Readers;

/// <summary>Limits for reading one solution; solutions are untrusted, so a zip bomb or a flood of tiny files must not exhaust memory or time.</summary>
internal sealed class ReadBudget(
    long maxTotalBytes = 256L * 1024 * 1024,
    long maxFileBytes = 50L * 1024 * 1024,
    long maxNodes = 15_000_000,
    int maxEntries = 100_000,
    int maxFiles = 100_000,
    long maxZipBytes = BoundedRead.MaxZipBytes)
{
    private long _remaining = maxTotalBytes;
    private long _nodesLeft = maxNodes;

    public long MaxFileBytes { get; } = maxFileBytes;

    public long MaxTotalBytes { get; } = maxTotalBytes;

    public long MaxNodes { get; } = maxNodes;

    public int MaxEntries { get; } = maxEntries;

    public int MaxFiles { get; } = maxFiles;

    public long MaxZipBytes { get; } = maxZipBytes;

    /// <summary>Counts <paramref name="count"/> XML nodes (an element's attributes count as one each) against the solution's budget.</summary>
    public void CountNode(string name, int count = 1)
    {
        if ((_nodesLeft -= count) < 0)
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