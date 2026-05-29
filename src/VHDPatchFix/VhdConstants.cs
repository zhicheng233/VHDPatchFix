namespace VHDPatchFix;

public static class VhdConstants
{
    public const int SectorSize = 512;
    public const int FooterSize = 512;
    public const int DynamicHeaderSize = 1024;
    public const uint DifferencingDiskType = 4;
    public const string FooterCookie = "conectix";
    public const string DynamicHeaderCookie = "cxsparse";
}
