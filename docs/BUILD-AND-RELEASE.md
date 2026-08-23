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

Version `0.1.1` packages as:

```text
artifacts/packages/KingmakerDiceRoller-0.1.1.zip
```

Its release tag is `v0.1.1`; qualification and installation do not create that
tag.

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
identity/version metadata, or mismatched hashes. Its expected package version is
read from the repository's root `Info.json`; do not introduce another
hard-coded release number.

## Unity Mod Manager version ordering

Unity Mod Manager 0.32.x does not implement Semantic Versioning prerelease
precedence. It splits version text on periods, removes non-digits from each
segment, and constructs a .NET `Version`. As a result:

```text
0.1.0-alpha.2 -> 0.1.0.2
0.1.0         -> 0.1.0
0.1.1         -> 0.1.1
```

The `0.1.1` patch release therefore supersedes both earlier packages. The source
validator models this parser and fails unless the active stable version sorts
above the published alpha. Avoid future stable/prerelease sequences that violate
that ordering.

## Transactional install and corrective smoke

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

Before publication, verify the exact installed `0.1.1` artifact:

1. Unity Mod Manager displays `0.1.1`.
2. UMM does not offer `0.1.0-alpha.2` as an update.
3. Kingmaker loads Dice Roller without a red UMM indicator.
4. A supported new-character build can Roll and Return to Point Buy.
5. The restored point-buy values and budget remain correct.

Record the evidence in `PROJECT-STATE.md`. Build/package/install evidence is not
a substitute for the human runtime smoke.

## GitHub release publishing

Authenticate GitHub CLI once:

```powershell
gh auth login
```

The guarded publisher requires a clean checkout of the repository's fully
pushed default branch. It re-runs the full build and package qualification,
validates the exact UMM ZIP, writes `SHA256SUMS.txt`, creates and pushes an
annotated `v<Info.json Version>` tag, and uploads the ZIP and checksum as GitHub
Release assets.

Create a draft release for review:

```powershell
powershell -NoLogo -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\Publish-Release.ps1 `
  -ReleaseNotesPath .\docs\RELEASE-NOTES-0.1.1.md
```

Publish publicly only after the exact current candidate has passed its human
runtime gate and `PROJECT-STATE.md` records `Runtime-qualified: **Yes**`:

```powershell
powershell -NoLogo -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\Publish-Release.ps1 `
  -ReleaseNotesPath .\docs\RELEASE-NOTES-0.1.1.md `
  -Publish `
  -ConfirmRuntimeQualified
```

The publisher refuses a dirty worktree, a non-default branch, a commit that
does not exactly match `origin/<default branch>`, an existing release, a
conflicting tag, a failed qualification gate, or a malformed package. It also
refuses public publication from a private repository unless
`-AllowPrivateRepositoryRelease` is supplied deliberately. That override does
not make a private repository's release publicly downloadable.

Published assets are immutable project history. Do not replace `v0.1.0` or
`v0.1.0-alpha.2`; publish `v0.1.1` as a new release.

## Publication policy

Work on mission-specific branches, but cut a release only from the clean, fully
pushed default branch. Do not force-push, retarget, or replace a published
version tag.
