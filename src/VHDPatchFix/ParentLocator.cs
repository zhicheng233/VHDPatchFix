using System.Text;

namespace VHDPatchFix;

public sealed record ParentLocator(int Index, string PlatformCode, uint PlatformDataSpace, uint PlatformDataLength, ulong PlatformDataOffset)
{
    private static readonly HashSet<string> WindowsCodes = new(StringComparer.Ordinal)
    {
        "W2ku",
        "W2ru"
    };

    public bool IsPresent => PlatformCode.Trim('\0') != string.Empty && PlatformDataSpace > 0 && PlatformDataOffset > 0;

    public bool IsWindowsUnicode => WindowsCodes.Contains(PlatformCode);

    public static ParentLocator Read(ReadOnlySpan<byte> header, int index)
    {
        int offset = 576 + index * 24;
        string platformCode = Encoding.ASCII.GetString(header.Slice(offset, 4));
        return new ParentLocator(
            index,
            platformCode,
            Endian.ReadUInt32(header, offset + 4),
            Endian.ReadUInt32(header, offset + 8),
            Endian.ReadUInt64(header, offset + 16));
    }

    public void Write(Span<byte> header)
    {
        int offset = 576 + Index * 24;
        Encoding.ASCII.GetBytes(PlatformCode, header.Slice(offset, 4));
        Endian.WriteUInt32(header, offset + 4, PlatformDataSpace);
        Endian.WriteUInt32(header, offset + 8, PlatformDataLength);
        Endian.WriteUInt32(header, offset + 12, 0);
        Endian.WriteUInt64(header, offset + 16, PlatformDataOffset);
    }
}
