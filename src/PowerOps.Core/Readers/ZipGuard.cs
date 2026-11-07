using System.Buffers.Binary;

namespace PowerOps.Core.Readers;

/// <summary>
/// Refuses a zip that is too long or declares too many entries, before <c>ZipFile.OpenRead</c> parses the whole central directory
/// (about 600 bytes of memory per entry). Only the zip's tail is read.
/// </summary>
internal static class ZipGuard
{
    private const uint EocdSignature = 0x06054b50;
    private const uint Zip64LocatorSignature = 0x07064b50;
    private const uint Zip64EocdSignature = 0x06064b50;
    private const int EocdSize = 22;
    private const int LocatorSize = 20;
    private const int Zip64EocdSize = 56;
    private const int MaxTail = EocdSize + ushort.MaxValue; // the record, then a comment of up to 65,535 bytes

    /// <exception cref="InvalidDataException">The zip is longer than the budget allows or declares too many entries; the message starts with <paramref name="zipPath"/>.</exception>
    /// <remarks>A missing or malformed end-of-central-directory record passes: <c>ZipFile.OpenRead</c> reports it.</remarks>
    internal static void Check(string zipPath, ReadBudget budget)
    {
        using var file = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        Check(file, zipPath, budget);
    }

    /// <summary>The same check for a zip held in a seekable stream, such as an <c>.msapp</c> read into memory; <paramref name="name"/> starts the message.</summary>
    internal static void Check(Stream zip, string name, ReadBudget budget)
    {
        var length = zip.Length;
        if (length > budget.MaxZipBytes)
        {
            throw new InvalidDataException($"{name}: larger than {budget.MaxZipBytes / (1024 * 1024)} MB.");
        }

        if (DeclaredEntries(zip, length) > (ulong)budget.MaxEntries)
        {
            throw new InvalidDataException($"{name}: more than {budget.MaxEntries} entries.");
        }
    }

    private static ulong DeclaredEntries(Stream file, long length)
    {
        var tail = new byte[(int)Math.Min(length, MaxTail)];
        var tailStart = length - tail.Length;
        file.Seek(tailStart, SeekOrigin.Begin);
        file.ReadExactly(tail);

        // The last signature wins, as it does for ZipFile.OpenRead.
        for (var i = tail.Length - EocdSize; i >= 0; i--)
        {
            if (BinaryPrimitives.ReadUInt32LittleEndian(tail.AsSpan(i)) != EocdSignature)
            {
                continue;
            }

            var disk = BinaryPrimitives.ReadUInt16LittleEndian(tail.AsSpan(i + 4));
            var entries = (ulong)BinaryPrimitives.ReadUInt16LittleEndian(tail.AsSpan(i + 10));
            var directorySize = BinaryPrimitives.ReadUInt32LittleEndian(tail.AsSpan(i + 12));
            var directoryOffset = BinaryPrimitives.ReadUInt32LittleEndian(tail.AsSpan(i + 16));
            // .NET reads the Zip64 record when the disk number, count or offset is at its limit; the size is checked as well.
            if (disk == ushort.MaxValue || entries == ushort.MaxValue || directorySize == uint.MaxValue || directoryOffset == uint.MaxValue)
            {
                entries = Math.Max(entries, Zip64Entries(file, length, tailStart + i));
            }

            return entries;
        }

        return 0;
    }

    private static ulong Zip64Entries(Stream file, long length, long eocdPosition)
    {
        var locatorPosition = eocdPosition - LocatorSize;
        if (locatorPosition < 0)
        {
            return 0;
        }

        var locator = new byte[LocatorSize];
        file.Seek(locatorPosition, SeekOrigin.Begin);
        file.ReadExactly(locator);
        if (BinaryPrimitives.ReadUInt32LittleEndian(locator) != Zip64LocatorSignature)
        {
            return 0;
        }

        var recordPosition = BinaryPrimitives.ReadUInt64LittleEndian(locator.AsSpan(8));
        if (recordPosition > (ulong)(length - Zip64EocdSize))
        {
            return 0;
        }

        var record = new byte[Zip64EocdSize];
        file.Seek((long)recordPosition, SeekOrigin.Begin);
        file.ReadExactly(record);
        return BinaryPrimitives.ReadUInt32LittleEndian(record) == Zip64EocdSignature
            ? BinaryPrimitives.ReadUInt64LittleEndian(record.AsSpan(32))
            : 0;
    }
}