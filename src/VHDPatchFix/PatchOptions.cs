namespace VHDPatchFix;

public sealed record PatchOptions(
    string VhdPath,
    string ParentPath,
    bool DryRun,
    bool NoBackup,
    string? BackupPath,
    bool Verbose);
