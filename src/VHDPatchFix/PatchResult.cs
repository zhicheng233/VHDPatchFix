namespace VHDPatchFix;

public sealed record PatchResult(
    string VhdPath,
    string NewParentPath,
    string? OldParentPath,
    string? BackupPath,
    bool DryRun,
    bool UsedRelocation,
    int UpdatedLocatorCount);
