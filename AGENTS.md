# Repository Guidelines

## Project Structure & Module Organization

This repository contains a small .NET CLI for patching the parent path stored in classic VHD differencing disks.

- `src/VHDPatchFix/` contains the CLI, VHD footer/header parsing, checksum logic, and patch operation.
- `VHDPatchFix.sln` is the canonical solution file for build and test commands.

Keep VHD binary parsing code isolated in small classes. Prefer adding focused helpers under `src/VHDPatchFix/` instead of expanding `Program.cs`.

## Build, and Development Commands

- `dotnet build VHDPatchFix.sln -v minimal` restores and builds the CLI and tests.
- `dotnet run --project src\VHDPatchFix\VHDPatchFix.csproj -- --help` shows CLI usage.
- `dotnet run --project src\VHDPatchFix\VHDPatchFix.csproj -- --vhd "I:\internal_1.vhd" --parent "\Device\FscryptDisk_APP_0\internal_0.vhd" --dry-run` validates a target without writing.

Use `--dry-run` before patching real disks. Ensure the child VHD is detached before writing.

## Coding Style & Naming Conventions

Use C# nullable reference types and implicit usings as configured in the project files. Use four-space indentation, PascalCase for public types and methods, and camelCase for locals and parameters.

Keep binary offsets explicit and close to the relevant VHD structure parser. Avoid unrelated formatting-only edits in functional changes.

## Commit & Pull Request Guidelines

This checkout has no Git history, so no repository-specific convention is established. Use short imperative subjects such as `Add VHD parent locator patcher`.

Pull requests should include the target scenario, before/after command output when relevant, and test results from `dotnet build` plus the test runner.

## Security & Data Safety

Do not patch attached VHDs. Keep backup creation enabled by default for real disks. Avoid logging full local paths unless needed for troubleshooting, and never validate parent existence as a requirement because NT device paths may only exist on another machine.
