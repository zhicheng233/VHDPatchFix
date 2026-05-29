using System.Buffers.Binary;

namespace VHDPatchFix;

public static class Endian
{
    public static uint ReadUInt32(ReadOnlySpan<byte> bytes, int offset)
    {
        return BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(offset, 4));
    }

    public static ulong ReadUInt64(ReadOnlySpan<byte> bytes, int offset)
    {
        return BinaryPrimitives.ReadUInt64BigEndian(bytes.Slice(offset, 8));
    }

    public static void WriteUInt32(Span<byte> bytes, int offset, uint value)
    {
        BinaryPrimitives.WriteUInt32BigEndian(bytes.Slice(offset, 4), value);
    }

    public static void WriteUInt64(Span<byte> bytes, int offset, ulong value)
    {
        BinaryPrimitives.WriteUInt64BigEndian(bytes.Slice(offset, 8), value);
    }
}
