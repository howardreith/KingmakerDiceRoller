# Build, package, and install

## Toolchain

- Windows PowerShell 5.1 or newer;
- Python 3;
- Visual Studio Build Tools/MSBuild with .NET Framework 4.7.2 references;
- local Pathfinder: Kingmaker 2.1.7b managed assemblies;
- local UMM 0.32.x and co-installed Harmony12.

No NuGet dependency or game binary is vendored.

## Local paths

Create ignored `GamePath.props` from the example and point it to the actual
Kingmaker Managed and UnityModManager directories. Never commit this file or
embed its paths in source, documentation generated for packaging, or
provenance.

## Focused build

```powershell
powershell -NoLogo -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\Build-Local.ps1 -Configuration Release
```

This runs repository validation, Python oracle cases, compiled C# behavior
cases, exact contract verification, and the production build.

## Full qualification and package

```powershell
git diff --check

powershell -NoLogo -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\Qualify.ps1 -Build -Package
```

For final provenance, run from the clean final commit. Qualification reports
branch, commit, dirty state, test counts, exact Assembly-CSharp MVID/SHA-256,
compiler warnings/errors, DLL SHA-256, package SHA-256, and package path.

Version `0.1.0-alpha.1` packages as:

```text
artifacts/packages/KingmakerDiceRoller-0.1.0-alpha.1.zip
```

The archive has one top-level `KingmakerDiceRoller` directory and exactly six
allowlisted files:

```text
KingmakerDiceRoller/Info.json
KingmakerDiceRoller/KingmakerDiceRoller.dll
KingmakerDiceRoller/LICENSE
KingmakerDiceRoller/README.md
KingmakerDiceRoller/THIRD-PARTY-NOTICES.md
KingmakerDiceRoller/licenses/UPSTREAM-WOTR-DICE-ROLLER-MIT.txt
```

Package validation rejects duplicates, unsafe paths, unexpected files, wrong
identity/version metadata, or mismatched hashes.

## Transactional install

Always inspect preflight first:

```powershell
powershell -NoLogo -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\Install.ps1 -WhatIf
```

It must name only `<Kingmaker>\Mods\KingmakerDiceRoller`. Then install:

```powershell
powershell -NoLogo -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\Install.ps1
```

The installer stages and validates the package, backs up only a prior
KingmakerDiceRoller directory, moves the staged directory into place, validates
the installed allowlist and DLL hash, and rolls back if a transactional step
fails. It must not enable, disable, reinstall, or alter another mod.

## Publication policy

Commit coherent source checkpoints and push normally to
`pro/kingmaker-dice-roller-mvp`. Do not force-push, rewrite published history,
merge to main, create a PR, tag, or publish a public release during alpha
qualification. Build/package/install evidence is not human runtime evidence.
