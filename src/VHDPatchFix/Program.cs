using System.Diagnostics;
using VHDPatchFix;

return ProgramMain.Run(args);

public static class ProgramMain
{
    public static int Run(string[] args)
    {
        try
        {
            if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
            {
                PrintUsage();
                return args.Length == 0 ? 1 : 0;
            }

            PatchOptions options = ParseArgs(args);
            PatchResult result = VhdParentPathPatcher.Patch(options);
            PrintResult(result);

            if (options.Verbose && !options.DryRun)
            {
                TryRunGetVhd(options.VhdPath);
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 2;
        }
    }

    private static PatchOptions ParseArgs(string[] args)
    {
        string? vhdPath = null;
        string? parentPath = null;
        string? backupPath = null;
        bool dryRun = false;
        bool noBackup = false;
        bool verbose = false;

        for (int index = 0; index < args.Length; index++)
        {
            string arg = args[index];
            switch (arg)
            {
                case "--vhd":
                    vhdPath = ReadValue(args, ref index, arg);
                    break;
                case "--parent":
                    parentPath = ReadValue(args, ref index, arg);
                    break;
                case "--backup-path":
                    backupPath = ReadValue(args, ref index, arg);
                    break;
                case "--dry-run":
                    dryRun = true;
                    break;
                case "--no-backup":
                    noBackup = true;
                    break;
                case "--verbose":
                    verbose = true;
                    break;
                default:
                    throw new ArgumentException($"Unknown argument: {arg}");
            }
        }

        if (string.IsNullOrWhiteSpace(vhdPath))
        {
            throw new ArgumentException("Missing required argument: --vhd <path>");
        }

        if (string.IsNullOrWhiteSpace(parentPath))
        {
            throw new ArgumentException("Missing required argument: --parent <path>");
        }

        if (noBackup && backupPath is not null)
        {
            throw new ArgumentException("--no-backup cannot be used with --backup-path.");
        }

        return new PatchOptions(vhdPath, parentPath, dryRun, noBackup, backupPath, verbose);
    }

    private static string ReadValue(string[] args, ref int index, string name)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"Missing value for {name}.");
        }

        index++;
        return args[index];
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  vhdfix --vhd <child.vhd> --parent <new-parent-path> [--dry-run] [--no-backup] [--backup-path <path>] [--verbose]");
        Console.WriteLine();
        Console.WriteLine("Example:");
        Console.WriteLine(@"  vhdfix --vhd ""I:\internal_1.vhd"" --parent ""\Device\FscryptDisk_APP_0\internal_0.vhd""");
    }

    private static void PrintResult(PatchResult result)
    {
        Console.WriteLine(result.DryRun ? "Dry run completed." : "VHD parent path updated.");
        Console.WriteLine($"VHD: {result.VhdPath}");
        Console.WriteLine($"Old parent: {result.OldParentPath ?? "(not readable)"}");
        Console.WriteLine($"New parent: {result.NewParentPath}");
        Console.WriteLine($"Updated locators: {result.UpdatedLocatorCount}");
        Console.WriteLine($"Relocation needed: {result.UsedRelocation}");

        if (!result.DryRun)
        {
            if (!string.IsNullOrEmpty(result.BackupPath))
            {
                Console.WriteLine($"Backup: {result.BackupPath}");
            }

            Console.WriteLine($@"Verify with: Get-VHD -Path ""{result.VhdPath}""");
        }
    }

    private static void TryRunGetVhd(string vhdPath)
    {
        try
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = "powershell.exe",
                ArgumentList = { "-NoProfile", "-Command", $"Get-VHD -Path '{vhdPath.Replace("'", "''")}' | Select-Object -ExpandProperty ParentPath" },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using Process? process = Process.Start(startInfo);
            if (process is null)
            {
                return;
            }

            string output = process.StandardOutput.ReadToEnd().Trim();
            string error = process.StandardError.ReadToEnd().Trim();
            process.WaitForExit(5000);

            if (process.ExitCode == 0 && output.Length > 0)
            {
                Console.WriteLine($"Get-VHD ParentPath: {output}");
            }
            else if (error.Length > 0)
            {
                Console.WriteLine($"Get-VHD verification skipped: {error}");
            }
        }
        catch
        {
            Console.WriteLine("Get-VHD verification skipped: PowerShell or Hyper-V cmdlets are unavailable.");
        }
    }
}
