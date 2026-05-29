namespace VHDPatchFix;

public static class VhdParentPathPatcher
{
    public static PatchResult Patch(PatchOptions options)
    {
        if (!File.Exists(options.VhdPath))
        {
            throw new FileNotFoundException("VHD file was not found.", options.VhdPath);
        }

        using FileStream stream = new(options.VhdPath, options.DryRun ? FileMode.Open : FileMode.Open, options.DryRun ? FileAccess.Read : FileAccess.ReadWrite, FileShare.Read);
        VhdFooter footer = VhdFooter.Read(stream);
        if (footer.DiskType != VhdConstants.DifferencingDiskType)
        {
            throw new InvalidDataException("Only classic VHD differencing disks are supported.");
        }

        DynamicHeader header = DynamicHeader.Read(stream, footer.DataOffset);
        ParentLocator[] locators = header.ParentLocators.Where(locator => locator.IsPresent && locator.IsWindowsUnicode).ToArray();
        if (locators.Length == 0)
        {
            throw new InvalidDataException("No Windows unicode parent locator was found in the VHD dynamic header.");
        }

        string? oldParentPath = locators
            .Select(locator => VhdParentPathReader.ReadLocatorPath(stream, locator))
            .FirstOrDefault(path => !string.IsNullOrWhiteSpace(path)) ?? header.ParentUnicodeName;

        byte[] newLocatorData = VhdParentPathReader.EncodeWindowsLocatorPath(options.ParentPath);
        bool needsRelocation = locators.Any(locator => newLocatorData.Length > CheckedCapacity(locator));
        if (options.DryRun)
        {
            return new PatchResult(options.VhdPath, options.ParentPath, oldParentPath, null, true, needsRelocation, locators.Length);
        }

        string backupPath = CreateBackup(options);
        header.SetParentUnicodeName(options.ParentPath);

        foreach (ParentLocator locator in locators)
        {
            ParentLocator updated = newLocatorData.Length <= CheckedCapacity(locator)
                ? WriteInPlace(stream, locator, newLocatorData)
                : WriteRelocated(stream, footer, locator, newLocatorData);
            header.SetLocator(updated);
        }

        header.UpdateChecksum();
        stream.Position = header.Offset;
        stream.Write(header.Bytes);
        stream.Flush(true);

        stream.Position = 0;
        VhdFooter verifyFooter = VhdFooter.Read(stream);
        DynamicHeader verifyHeader = DynamicHeader.Read(stream, verifyFooter.DataOffset);
        ParentLocator? verifyLocator = verifyHeader.ParentLocators.FirstOrDefault(locator => locator.IsPresent && locator.IsWindowsUnicode);
        string? verifiedPath = verifyLocator is null ? null : VhdParentPathReader.ReadLocatorPath(stream, verifyLocator);
        if (!string.Equals(verifiedPath, options.ParentPath, StringComparison.Ordinal))
        {
            throw new InvalidDataException("The VHD was written, but verification did not read back the requested parent path.");
        }

        return new PatchResult(options.VhdPath, options.ParentPath, oldParentPath, backupPath, false, needsRelocation, locators.Length);
    }

    private static string CreateBackup(PatchOptions options)
    {
        if (options.NoBackup)
        {
            return string.Empty;
        }

        string backupPath = options.BackupPath ?? options.VhdPath + ".bak";
        if (File.Exists(backupPath))
        {
            throw new IOException($"Backup path already exists: {backupPath}");
        }

        File.Copy(options.VhdPath, backupPath);
        return backupPath;
    }

    private static ParentLocator WriteInPlace(Stream stream, ParentLocator locator, byte[] data)
    {
        uint capacity = CheckedCapacity(locator);
        stream.Position = (long)locator.PlatformDataOffset;
        stream.Write(data);
        WriteZeros(stream, checked((int)(capacity - data.Length)));
        return locator with { PlatformDataLength = (uint)data.Length };
    }

    private static ParentLocator WriteRelocated(Stream stream, VhdFooter footer, ParentLocator locator, byte[] data)
    {
        long footerOffset = stream.Length - VhdConstants.FooterSize;
        stream.SetLength(footerOffset);
        long dataOffset = Align(stream.Length, VhdConstants.SectorSize);
        stream.Position = stream.Length;
        WriteZeros(stream, checked((int)(dataOffset - stream.Length)));
        stream.Position = dataOffset;
        stream.Write(data);

        uint dataSpace = checked((uint)Align(data.Length, VhdConstants.SectorSize));
        long paddedEnd = dataOffset + dataSpace;
        WriteZeros(stream, checked((int)(paddedEnd - stream.Position)));
        stream.Write(footer.Bytes);

        return locator with
        {
            PlatformDataOffset = (ulong)dataOffset,
            PlatformDataSpace = dataSpace,
            PlatformDataLength = (uint)data.Length
        };
    }

    private static uint CheckedCapacity(ParentLocator locator)
    {
        return locator.PlatformDataSpace;
    }

    private static long Align(long value, int alignment)
    {
        long remainder = value % alignment;
        return remainder == 0 ? value : value + alignment - remainder;
    }

    private static void WriteZeros(Stream stream, int count)
    {
        if (count <= 0)
        {
            return;
        }

        byte[] zeros = new byte[Math.Min(count, 8192)];
        while (count > 0)
        {
            int toWrite = Math.Min(count, zeros.Length);
            stream.Write(zeros, 0, toWrite);
            count -= toWrite;
        }
    }
}
