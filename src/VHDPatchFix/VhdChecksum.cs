namespace VHDPatchFix;

public static class VhdChecksum
{
    public static uint Compute(ReadOnlySpan<byte> bytes)
    {
        uint sum = 0;
        foreach (byte value in bytes)
        {
            sum += value;
        }

        return ~sum;
    }
}
