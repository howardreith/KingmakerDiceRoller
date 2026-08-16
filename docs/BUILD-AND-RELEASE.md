# Build and release

## Requirements

- Windows PowerShell 5.1 or newer.
- Visual Studio Build Tools/MSBuild with .NET Framework 4.7.2 targeting pack.
- Python 3.
- Pathfinder: Kingmaker 2.1.7b.
- Unity Mod Manager 0.32.x with Harmony12.

No NuGet package or game binary is vendored.

## Configure

```powershell
Copy-Item GamePath.props.example GamePath.props
notepad GamePath.props
```

`GamePath.props` is ignored. The build validates `Assembly-CSharp.dll`,
`UnityModManager.dll`, and `0Harmony12.dll` before resolving references.

## Source-only qualification

```powershell
.\scripts\Qualify.ps1
```

This runs the repository validator and Python behavior oracle. It does not imply
that the C# source compiled.

## Build qualification

```powershell
.\scripts\Qualify.ps1 -Build
```

This validates source, runs the Python oracle, compiles/runs the C# domain test
runner, verifies exact local Kingmaker contracts, and rebuilds the production
DLL. A passing run writes provenance and hashes.

## Package

```powershell
.\scripts\Qualify.ps1 -Build -Package
```

The deterministic ZIP contains exactly one top-level `KingmakerDiceRoller`
folder with the DLL, UMM metadata, README, license, notices, and upstream MIT
text. It never contains game assemblies or local configuration.

## Install and uninstall

```powershell
.\scripts\Install.ps1 -WhatIf
.\scripts\Install.ps1
.\scripts\Uninstall.ps1 -WhatIf
```

Installation validates an exact entry allowlist, backs up only a previous
`Mods\KingmakerDiceRoller` directory, and never touches another mod.
Uninstallation moves only that directory to a timestamped backup.

## Release gate

Do not tag or publish a runtime release until `docs/SMOKE-TEST.md` passes and the
project state is updated with concrete evidence. Keep source-, build-, runtime-,
and compatibility-qualified labels separate.
