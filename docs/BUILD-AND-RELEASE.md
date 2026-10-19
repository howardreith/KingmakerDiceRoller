# Build and release

## 0.1.14 native-stat UI presentation alpha

The [v0.1.14 testing alpha](https://github.com/howardreith/KingmakerDiceRoller/releases/tag/v0.1.14)
contains the exact ZIP from qualified implementation commit
`252ff2f435476bdc74d051cf09358250dd3e204c` (SHA-256
`95603893a14a0b6538977070e893603ab1328f556c7893fe70d5229fbfca6d1a`).
The annotated tag points to documentation-only release commit
`a45e5336add15dfc45aea249ee2b6d7c5e78c142` on
`codex/native-stat-ui-v2`. A fresh zero-warning Release rebuild and strict
package validation reproduced the qualified ZIP and DLL byte for byte. The
full suite was not rerun for publication, at the owner's request. Actual-game
visuals, input, and compatibility remain unverified; see
`docs/NATIVE-STAT-UI-V3-CANDIDATE.md`. v0.1.11 remains latest stable.

## 0.1.13 revised native-stat candidate

The revised UI is packaged from clean implementation commit
`46a1f2c5fdf236ef48dc4e5ffb4a78316d6bcf05` at
`artifacts/candidates/46a1f2c5fdf236ef48dc4e5ffb4a78316d6bcf05/KingmakerDiceRoller-0.1.13.zip`.
Its SHA-256 is `e22827efea6450a089da8e4582a5fb7b82de3909c4d5b884e6850c9729067667`.
`Qualify.ps1 -Build -Package -Candidate` passed source, deterministic domain,
installed-contract, Release build, and strict package checks. A fresh Release
build and package validation reproduced the identical ZIP for the
[v0.1.13 testing alpha](https://github.com/howardreith/KingmakerDiceRoller/releases/tag/v0.1.13).
The full suite was not rerun for publication, at the owner's request. Actual
game visuals and interaction remain unverified; see
`docs/NATIVE-STAT-UI-V2-CANDIDATE.md`.

## 0.1.12 native stat UI alpha (historical)

The owner requested a GitHub alpha on 2026-10-19 for installation on another
computer. [v0.1.12](https://github.com/howardreith/KingmakerDiceRoller/releases/tag/v0.1.12)
is a published prerelease from the mission branch; 0.1.11 remains latest
stable. The alpha ZIP is SHA-256
`53e1344d0bd1d93b52f7fadf66a20c64993363462e432d7c74302518940080c0`.
Its fresh Release build and package validation passed; actual-game interaction
and visual qualification remain unrun. Use Windows PowerShell 5.1
for the .NET Framework respec contract verifier. From a clean committed branch:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Qualify.ps1 -Build -Package -Candidate
```

The command binds the DLL, reports, and six-file ZIP to the exact clean commit
under `artifacts/candidates/<commit>/`. It does not establish rendered UI or
gameplay qualification. Follow `docs/SMOKE-TEST.md` before calling the new
screen visually accepted.

# Published 0.1.11 settings-panel release

`0.1.11` simplifies the UMM settings panel only; v0.1.10 is published, so the
change takes the next unused patch version. On 2026-10-15 the owner authorized
releasing it without an additional in-game test. `PROJECT-STATE.md` records
that authorization under `## Qualification truth` with
`Runtime-qualified: **No**`. Publish from a clean release clone of the fully
pushed `main` merge commit (Windows PowerShell 5.1):

```powershell
powershell -NoLogo -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\Publish-Release.ps1 `
  -ReleaseNotesPath .\docs\RELEASE-NOTES-0.1.11.md `
  -Publish `
  -ConfirmOwnerAuthorizedPreRuntimeRelease
```

Nexus page text is in `docs/NEXUS-RELEASE-COPY.md`; upload the exact GitHub
v0.1.11 asset.

# Published 0.1.10 Mr. Bard branding release

`0.1.10` is a branding-only release: UMM `DisplayName` **Mr. Bard's Kingmaker
Roll For Stats UI**, `Author` **Mr. Bard**, the matching settings-panel title,
assembly title/product/company metadata and documentation. The UMM ID, DLL,
entry method, Harmony ID, package layout and settings are unchanged. The
publisher derives the GitHub release title (`<DisplayName> v<Version>`) and
generated install notes from `Info.json`.

On 2026-10-15 the owner authorized merging and publishing 0.1.10 as a stable
release without an additional in-game test. `PROJECT-STATE.md` records that
authorization under `## Qualification truth` with `Runtime-qualified: **No**`.
Publish from a clean release clone of the fully pushed `main` merge commit
(Windows PowerShell 5.1):

```powershell
powershell -NoLogo -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\Publish-Release.ps1 `
  -ReleaseNotesPath .\docs\RELEASE-NOTES-0.1.10.md `
  -Publish `
  -ConfirmOwnerAuthorizedPreRuntimeRelease
```

The packaged README is frozen into the ZIP, so release-status wording must be
final before this command runs. Nexus page text is in
`docs/NEXUS-RELEASE-COPY.md`; upload the exact GitHub v0.1.10 asset.

# Historical 0.1.7 official release

On 2026-09-13, after the repair handoff disclosed the disconnected-desktop
blocker, the owner explicitly requested merge, push and an actual release,
not a test. This authorizes v0.1.7 as official/latest while native themed-path,
rendered interaction, audio, runtime and compatibility acceptance remain unrun.
The automated publication gate remains unchanged; do not falsely assert
`-ConfirmRuntimeQualified` or mark runtime evidence Yes.

Prepare from the clean, fully pushed main merge commit using the existing
publisher's draft path (Windows PowerShell 5.1):

```powershell
powershell -NoLogo -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\Publish-Release.ps1 `
  -ReleaseNotesPath .\docs\RELEASE-NOTES-0.1.7.md
```

This runs the full qualification suite, packages the six allowlisted files,
creates/pushes the annotated v0.1.7 tag and uploads the ZIP/checksum to a draft.
Verify the source/tag, DLL parity with the reviewed candidate and downloaded
ZIP/checksum assets. Then apply the specific owner authorization:

```powershell
gh release edit v0.1.7 --repo howardreith/KingmakerDiceRoller `
  --draft=false --prerelease=false --latest --verify-tag
```

Verify the published release is official/latest and its bytes match the draft.
Record final source/tag, asset hashes and publication metadata in the handoff.
Runtime installation and saved-game operations are separate from publication.
This documented exception applies to the owner's v0.1.7 request; it does not
change the normal qualification gate for future releases.

# Historical 0.1.6 native book UI release

The owner authorized committing, merging, pushing and cutting a 0.1.6 release
on 2026-09-13, then selected "Official/latest, with the unrun checks disclosed."
The existing v0.1.6 release was promoted to official/latest with this command:

```powershell
gh release edit v0.1.6 --repo howardreith/KingmakerDiceRoller `
  --prerelease=false --latest --title "Kingmaker Dice Roller v0.1.6" `
  --notes-file artifacts/release-0.1.6/promotion/release-notes-official.md `
  --verify-tag
```

The prepared notes retain the full installation/verification sections and
original artifact hash and source commit. The owner explicitly selected official
publication with visual, audio, runtime and compatibility checks still NOT RUN;
this decision does not mark Runtime-qualified Yes. It supersedes the usual live
acceptance prerequisite for this specific promotion. The automated new-release
qualification gate remains unchanged; no runtime-confirmation assertion was made.

GitHub's latest-release response, prerelease/draft flags, asset IDs, sizes,
digests, upload timestamps and tag refs were verified before/after promotion.
The existing ZIP, checksum and annotated tag are unchanged. Do not rerun the
new-release publisher or replace the published package for a metadata promotion.
The ZIP's README retains its original testing-prerelease wording; the release
page and current repository record official/latest status. Exact publication
and promotion evidence is in `PROJECT-STATE.md`.

Use installed Windows PowerShell 5.1, Python 3 and the .NET 4.7.2/C# 7.3 toolchain
for future builds. Run Python in UTF-8 mode for repository documentation. The UI
uses the existing TextRenderingModule for layout alignment, referenced read-only
with Copy Local false; no game binary is distributed. Installation, desktop/game
testing and save modification remain outside the authorized work.

## Preserved local candidate workflow

After committing all source and documentation, run from the exact clean head:

```powershell
powershell -NoLogo -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\Qualify.ps1 -Build -Package -Candidate
```

`-Candidate` refuses `-Install`, mismatched/dirty build provenance, inconsistent
numeric Info/assembly versions and an existing output directory. It stages and
packages only in `artifacts/candidates/<full-commit>/`, preserving historical
`artifacts/packages/` outputs. The six-file allowlist is unchanged. This directory
also contains the exact DLL, package manifest, build provenance and contract/source
reports; no publication, tag, install or game process is started. `Build-Local`
adds 16 read-only native/candidate UI IL checks after the existing contract runner.
Record final hashes and logs without changing tracked source after the final build.
A document cannot embed its own commit hash; the ignored candidate manifest is
the authoritative exact clean source identity.

# Historical release workflow

Howie confirmed the released 0.1.3 workflow works and authorized official release.
Promote the existing GitHub release to stable/latest and record that sign-off.
Preserve the published ZIP, checksum asset, and annotated tag; no new build or
package is needed for promotion. Detailed compatibility evidence remains scoped
to the recorded tests and owner report. Local installation and gameplay remain
separate tasks.

DATA uses Windows PowerShell 5.1, its existing Python 3.12 on the process PATH,
.NET Framework 4.7.2, C# 7.3, and installed UMM 0.33.0 / Harmony12 libraries.
PowerShell Core is not a substitute for the desktop .NET contract fixture.
Do not downgrade the installed loader.

`Build-Local.ps1` runs `Verify-RespecContracts.ps1` after compilation. The verifier
patches an owned test fixture outside Unity; it never launches the game or executes
player respec. Reports remain under ignored `artifacts`.

# Build, package, install, and publish

## Toolchain

- Windows PowerShell 5.1 (desktop .NET Framework);
- Python 3;
- Visual Studio Build Tools/MSBuild with .NET Framework 4.7.2 references;
- local Pathfinder: Kingmaker 2.1.7b managed assemblies;
- DATA: verified UMM 0.33.0 and co-installed Harmony12 1.2.0.1;
  preserve the installed runtime libraries;
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
cases, exact contract verification, the production build, and optional respec contracts/scope checks.

## Full qualification and package

```powershell
git diff --check

powershell -NoLogo -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\Qualify.ps1 -Build -Package
```

For final provenance, run from the clean final commit. Qualification reports
branch, commit, dirty state, test counts, exact Assembly-CSharp MVID/SHA-256,
compiler warnings/errors, DLL SHA-256, package SHA-256, and package path.

Version `0.1.3` packages as:

```text
artifacts/packages/KingmakerDiceRoller-0.1.3.zip
```

Its release tag is `v0.1.3`; qualification and installation do not create that
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

Before claiming runtime qualification or making a stable release, verify the exact installed artifact:

1. Unity Mod Manager displays the version under test.
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
  -ReleaseNotesPath .\docs\RELEASE-NOTES-0.1.3.md
```

For a future explicitly authorized testing prerelease (the original 0.1.3 publication used this path):

```powershell
powershell -NoLogo -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\Publish-Release.ps1 `
  -ReleaseNotesPath .\docs\RELEASE-NOTES-0.1.3.md `
  -Publish `
  -TestingPrerelease
```

This requires current Source/Contract/Build/Package-qualified, Release-authorized,
and Testing-prerelease-authorized statuses to be Yes. It marks the GitHub release
as a prerelease and not latest; it does not mark runtime qualification Yes.
Keep the numeric UMM version `0.1.3` so future version ordering remains predictable.

Publish a runtime-qualified stable release only after the exact current
candidate has passed its human runtime gate and `PROJECT-STATE.md` records
`Runtime-qualified: **Yes**`:

```powershell
powershell -NoLogo -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\Publish-Release.ps1 `
  -ReleaseNotesPath .\docs\RELEASE-NOTES-0.1.3.md `
  -Publish `
  -ConfirmRuntimeQualified
```

When the owner explicitly authorizes a stable release before the runtime gate,
record it in the current `## Qualification truth` section as
`- Owner-authorized pre-runtime release: **Yes** — <version and instruction>`
while keeping `Runtime-qualified: **No**`, then use
`-Publish -ConfirmOwnerAuthorizedPreRuntimeRelease` instead of
`-ConfirmRuntimeQualified`. The gate requires current Source/Contract/Build/
Package-qualified and Release-authorized **Yes**, the exact version in the
owner-authorization line and `Runtime-qualified: **No**`; it refuses a
prerelease version and any second disposition switch. The release is stable
and latest, and its generated notes state that it is not runtime-qualified.
This path never records runtime evidence.

The publisher refuses a dirty worktree, a non-default branch, a commit that
does not exactly match `origin/<default branch>`, an existing release, a
conflicting tag, a failed qualification gate, or a malformed package. It also
refuses public publication from a private repository unless
`-AllowPrivateRepositoryRelease` is supplied deliberately. That override does
not make a private repository's release publicly downloadable.

Published assets are immutable project history. Do not replace published ZIPs,
checksum assets, or version tags. New implementation bytes need a new version.

## Promote an existing tested prerelease

Owner confirmation and official-release authorization can promote an existing
public prerelease without rebuilding it. Record the scoped runtime sign-off in
`PROJECT-STATE.md`, validate the repository and publication qualification gate,
and prepare updated notes retaining the original checksum and release commit.

Use `gh release edit v0.1.3 --repo howardreith/KingmakerDiceRoller --prerelease=false
--latest --title "Kingmaker Dice Roller v0.1.3" --notes-file <prepared-notes>
--verify-tag` as one command. The guarded new-release publisher deliberately
refuses an existing public release; do not rerun it or replace its assets.
Verify the latest-release response, prerelease flag, asset IDs/digests, and peeled
tag commit against the pre-promotion record. Promotion documentation on `main`
may be newer than the immutable tagged build. The packaged README remains the
original artifact's README; the current repository and release page record promotion.

## Publication policy

Work on mission-specific branches, but cut a release only from the clean, fully
pushed default branch. Do not force-push, retarget, or replace a published
version tag.
