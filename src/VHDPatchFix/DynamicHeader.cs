using System.Text;

namespace VHDPatchFix;

public sealed class DynamicHeader
{
    public DynamicHeader(long offset, byte[] bytes)
    {
        Offset = offset;
        Bytes = bytes;
        ParentLocators = Enumerable.Range(0, 8).Select(index => ParentLocator.Read(bytes, index)).ToArray();
    }

    public long Offset { get; }
    public byte[] Bytes { get; }
    public ParentLocator[] ParentLocators { get; private set; }

    public string ParentUnicodeName
    {
        get
        {
            byte[] nameBytes = Bytes.AsSpan(64, 512).ToArray();
            string value = Encoding.BigEndianUnicode.GetString(nameBytes);
            return value.TrimEnd('\0');
        }
    }

    public static DynamicHeader Read(Stream stream, ulong offset)
    {
        if (offset > long.MaxValue || offset + VhdConstants.DynamicHeaderSize > (ulong)stream.Length)
        {
            throw new InvalidDataException("The VHD dynamic header offset is outside the file.");
        }

        byte[] bytes = new byte[VhdConstants.DynamicHeaderSize];
        stream.Position = (long)offset;
        ReadExactly(stream, bytes);

        string cookie = Encoding.ASCII.GetString(bytes, 0, 8);
        if (cookie != VhdConstants.DynamicHeaderCookie)
        {
            throw new InvalidDataException("The VHD dynamic header cookie is invalid.");
        }

        uint storedChecksum = Endian.ReadUInt32(bytes, 36);
        Endian.WriteUInt32(bytes, 36, 0);
        uint computedChecksum = VhdChecksum.Compute(bytes);
        Endian.WriteUInt32(bytes, 36, storedChecksum);
        if (storedChecksum != computedChecksum)
        {
            throw new InvalidDataException("The VHD dynamic header checksum is invalid.");
        }

        return new DynamicHeader((long)offset, bytes);
    }

    public void SetParentUnicodeName(string parentPath)
    {
        Bytes.AsSpan(64, 512).Clear();
        string value = parentPath.Length > 255 ? parentPath[..255] : parentPath;
        Encoding.BigEndianUnicode.GetBytes(value, Bytes.AsSpan(64, 512));
    }

    public void SetLocator(ParentLocator locator)
    {
        locator.Write(Bytes);
        ParentLocators[locator.Index] = locator;
    }

    public void UpdateChecksum()
    {
        Endian.WriteUInt32(Bytes, 36, 0);
        Endian.WriteUInt32(Bytes, 36, VhdChecksum.Compute(Bytes));
    }

    private static void ReadExactly(Stream stream, byte[] buffer)
    {
        int read = 0;
        while (read < buffer.Length)
        {
            int count = stream.Read(buffer, read, buffer.Length - read);
            if (count == 0)
            {
                throw new EndOfStreamException();
            }

            read += count;
        }
    }
}
