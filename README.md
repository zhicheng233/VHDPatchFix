# VHDPatchFix

`vhdfix` patches the parent path stored inside a classic VHD differencing disk. It is intended for cases where the parent VHD was moved and PowerShell `Set-VHD` cannot update the stored parent locator.

## Usage

Preview a change without writing:

```powershell
dotnet run --project src\VHDPatchFix\VHDPatchFix.csproj -- --vhd "I:\B.vhd" --parent "I:\A.vhd" --dry-run
```

Patch the VHD and create the default `.bak` backup:

```powershell
dotnet run --project src\VHDPatchFix\VHDPatchFix.csproj -- --vhd "I:\B.vhd" --parent "I:\A.vhd"
```

Verify after writing:

```powershell
Get-VHD -Path "I:\A.vhd"
```

## Safety Notes

- Only classic `.vhd` differencing disks are supported.
- `.vhdx` is not supported.
- The child VHD should be detached before patching.
- The tool does not require the new parent path to exist on the current computer.

## Development

```powershell
dotnet build VHDPatchFix.sln -v minimal
dotnet run --project src\VHDPatchFix\VHDPatchFix.csproj -- --help
```
