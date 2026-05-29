using System.Text;

namespace VHDPatchFix;

public sealed record VhdFooter(byte[] Bytes, ulong DataOffset, uint DiskType)
{
    public static VhdFooter Read(Stream stream)
    {
        if (stream.Length < VhdConstants.FooterSize)
        {
            throw new InvalidDataException("File is too small to contain a VHD footer.");
        }

        byte[] bytes = new byte[VhdConstants.FooterSize];
        stream.Position = stream.Length - VhdConstants.FooterSize;
        ReadExactly(stream, bytes);

        string cookie = Encoding.ASCII.GetString(bytes, 0, 8);
        if (cookie != VhdConstants.FooterCookie)
        {
            throw new InvalidDataException("The file does not end with a classic VHD footer.");
        }

        uint storedChecksum = Endian.ReadUInt32(bytes, 64);
        Endian.WriteUInt32(bytes, 64, 0);
        uint computedChecksum = VhdChecksum.Compute(bytes);
        Endian.WriteUInt32(bytes, 64, storedChecksum);
        if (storedChecksum != computedChecksum)
        {
            throw new InvalidDataException("The VHD footer checksum is invalid.");
        }

        return new VhdFooter(bytes, Endian.ReadUInt64(bytes, 16), Endian.ReadUInt32(bytes, 60));
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
