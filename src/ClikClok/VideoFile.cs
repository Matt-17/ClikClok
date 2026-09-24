using System.Buffers.Binary;

namespace ClikClok;

static class VideoFile
{
    public static string ContentType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".mov" => "video/quicktime",
        ".webm" => "video/webm",
        _ => "video/mp4",
    };

    /// <summary>Reads the duration from the MP4/MOV 'moov/mvhd' box, or null for other containers.</summary>
    public static TimeSpan? ReadDuration(string path)
    {
        using var stream = File.OpenRead(path);
        return FindBox(stream, 0, stream.Length, "moov") is { } moov
            && FindBox(stream, moov.Start, moov.End, "mvhd") is { } mvhd
            ? ReadMvhd(stream, mvhd.Start)
            : null;
    }

    static (long Start, long End)? FindBox(Stream stream, long from, long to, string type)
    {
        Span<byte> header = stackalloc byte[16];
        var position = from;
        while (position + 8 <= to)
        {
            stream.Position = position;
            if (stream.Read(header[..8]) < 8) return null;
            long size = BinaryPrimitives.ReadUInt32BigEndian(header);
            var headerSize = 8L;
            if (size == 1)
            {
                if (stream.Read(header[8..16]) < 8) return null;
                size = (long)BinaryPrimitives.ReadUInt64BigEndian(header[8..]);
                headerSize = 16;
            }
            else if (size == 0)
            {
                size = to - position;
            }
            if (size < headerSize) return null;

            if (System.Text.Encoding.ASCII.GetString(header[4..8]) == type)
                return (position + headerSize, Math.Min(position + size, to));
            position += size;
        }
        return null;
    }

    static TimeSpan? ReadMvhd(Stream stream, long start)
    {
        Span<byte> data = stackalloc byte[32];
        stream.Position = start;
        if (stream.Read(data) < 32) return null;
        // version 0: flags, 32-bit times, timescale, 32-bit duration; version 1: 64-bit times and duration
        var (timescale, duration) = data[0] == 1
            ? (BinaryPrimitives.ReadUInt32BigEndian(data[20..]), BinaryPrimitives.ReadUInt64BigEndian(data[24..]))
            : (BinaryPrimitives.ReadUInt32BigEndian(data[12..]), BinaryPrimitives.ReadUInt32BigEndian(data[16..]));
        return timescale == 0 ? null : TimeSpan.FromSeconds((double)duration / timescale);
    }
}
