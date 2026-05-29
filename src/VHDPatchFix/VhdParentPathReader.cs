using System.Text;

namespace VHDPatchFix;

public static class VhdParentPathReader
{
    public static string? ReadLocatorPath(Stream stream, ParentLocator locator)
    {
        if (!locator.IsPresent || locator.PlatformDataLength == 0)
        {
            return null;
        }

        if (locator.PlatformDataOffset > long.MaxValue ||
            locator.PlatformDataOffset + locator.PlatformDataLength > (ulong)stream.Length)
        {
            return null;
        }

        byte[] bytes = new byte[locator.PlatformDataLength];
        stream.Position = (long)locator.PlatformDataOffset;
        ReadExactly(stream, bytes);
        return DecodeLocatorPath(bytes);
    }

    public static string DecodeLocatorPath(ReadOnlySpan<byte> bytes)
    {
        ReadOnlySpan<byte> trimmed = TrimTrailingZeros(bytes);
        string littleEndian = Encoding.Unicode.GetString(trimmed);
        if (LooksLikePath(littleEndian))
        {
            return littleEndian.TrimEnd('\0');
        }

        string bigEndian = Encoding.BigEndianUnicode.GetString(trimmed);
        if (LooksLikePath(bigEndian))
        {
            return bigEndian.TrimEnd('\0');
        }

        return Encoding.UTF8.GetString(trimmed).TrimEnd('\0');
    }

    public static byte[] EncodeWindowsLocatorPath(string parentPath)
    {
        return Encoding.Unicode.GetBytes(parentPath);
    }

    private static ReadOnlySpan<byte> TrimTrailingZeros(ReadOnlySpan<byte> bytes)
    {
        int length = bytes.Length;
        while (length > 0 && bytes[length - 1] == 0)
        {
            length--;
        }

        if (length % 2 != 0 && length < bytes.Length)
        {
            length++;
        }

        return bytes.Slice(0, length);
    }

    private static bool LooksLikePath(string value)
    {
        string trimmed = value.TrimEnd('\0');
        if (trimmed.Length == 0)
        {
            return false;
        }

        int controlChars = trimmed.Count(char.IsControl);
        return controlChars == 0 && (trimmed.Contains('\\') || trimmed.Contains('/') || trimmed.Contains(':'));
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
