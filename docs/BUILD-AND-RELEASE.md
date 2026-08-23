# Build, package, install, and publish

## Toolchain

- Windows PowerShell 5.1 or newer;
- Python 3;
- Visual Studio Build Tools/MSBuild with .NET Framework 4.7.2 references;
- local Pathfinder: Kingmaker 2.1.7b managed assemblies;
- local UMM 0.32.x and co-installed Harmony12;
- GitHub CLI (`gh`) authenticated with write access to this repository.

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

Version `0.1.0-alpha.2` packages as:

```text
artifacts/packages/KingmakerDiceRoller-0.1.0-alpha.2.zip
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

## GitHub release publishing

Authenticate GitHub CLI once:

```powershell
gh auth login
```

The guarded publisher requires a clean checkout of the repository's fully
pushed default branch. It re-runs the full build and package qualification,
validates the exact UMM ZIP, writes `SHA256SUMS.txt`, creates and pushes an
annotated `v<Info.json Version>` tag, and uploads the ZIP and checksum as
GitHub Release assets.

Create a draft release for review:

```powershell
powershell -NoLogo -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\Publish-Release.ps1
```

Publish publicly only after the exact current candidate has passed its human
runtime gate and `PROJECT-STATE.md` records `Runtime-qualified: **Yes**`:

```powershell
powershell -NoLogo -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\Publish-Release.ps1 `
  -Publish `
  -ConfirmRuntimeQualified
```

Use `-ReleaseNotesPath <path>` to prepend project-specific notes. Versions with
a prerelease suffix, such as `0.1.0-alpha.2`, are automatically marked as
GitHub prereleases and are not marked Latest.

The publisher refuses a dirty worktree, a non-default branch, a commit that
does not exactly match `origin/<default branch>`, an existing release, a
conflicting tag, a failed qualification gate, or a malformed package. It also
refuses public publication from a private repository unless
`-AllowPrivateRepositoryRelease` is supplied deliberately. That override does
not make a private repository's release publicly downloadable.

Published assets are immutable project history. Advance the version rather than
replacing a released ZIP.

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

Feature work may continue on `pro/kingmaker-dice-roller-mvp`, but a release is
cut only from the clean, fully pushed default branch. Do not force-push,
retarget, or replace a published version tag. Build/package/install evidence is
not a substitute for human runtime evidence.
