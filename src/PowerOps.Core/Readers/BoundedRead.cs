using System.Text;

namespace PowerOps.Core.Readers;

/// <summary>Reads a whole file but counts the bytes actually read, so a special file (<c>/dev/zero</c>, whose <c>FileInfo.Length</c> is 0) or a file that grows can't pass a size check.</summary>
public static class BoundedRead
{
    /// <exception cref="InvalidDataException">The file is longer than <paramref name="maxBytes"/>; the message starts with the path. Reading stops one byte past the cap.</exception>
    public static byte[] ReadAllBytes(string path, long maxBytes)
    {
        using var stream = File.OpenRead(path);
        using var buffer = new MemoryStream();
        var chunk = new byte[81_920];
        int read;
        while ((read = stream.Read(chunk, 0, (int)Math.Min(chunk.Length, maxBytes + 1 - buffer.Length))) > 0)
        {
            buffer.Write(chunk, 0, read);
            if (buffer.Length > maxBytes)
            {
                throw new InvalidDataException($"{path}: larger than {Describe(maxBytes)}.");
            }
        }

        return buffer.ToArray();
    }

    /// <summary>Like <see cref="File.ReadAllText(string)"/>: UTF-8 unless a byte order mark says otherwise.</summary>
    /// <exception cref="InvalidDataException">As <see cref="ReadAllBytes"/>.</exception>
    public static string ReadAllText(string path, long maxBytes)
    {
        using var reader = new StreamReader(new MemoryStream(ReadAllBytes(path, maxBytes)), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    private static string Describe(long bytes) => bytes % (1024 * 1024) == 0 ? $"{bytes / (1024 * 1024)} MB" : $"{bytes} bytes";
}