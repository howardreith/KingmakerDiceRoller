# UMM compatibility baseline

## Contract

As requested on 2026-11-23, `Info.json` declares UMM **0.31.1** and the production
DLL is compiled against **UnityModManager 0.31.1.0** and **0Harmony12 1.2.0.1**,
whatever loader is installed on the build machine.

- `scripts/loader-reference-pins.json` pins both references (the same upstream
  bytes Kingmaker Cutscene Skip already pins):
  - UnityModManager 0.31.1.0 from
    `https://api.nuget.org/v3-flatcontainer/unitymodmanager/0.31.1/unitymodmanager.0.31.1.nupkg`
    (package SHA-256 `05a82b66bc44d56d1246f146d16e9fa8f9a42c450c5bc334bcfa9efddbdb53c9`,
    entry `lib/net35/UnityModManager.dll`, DLL SHA-256
    `2e038ff23ab01d538dfef792096e1f5c04802d1edac3660db20d0d54c167c2c4`);
  - 0Harmony12 1.2.0.1 from newman55/unity-mod-manager commit `10cfb1cc`
    (`lib/Harmony/1.2/0Harmony12.dll`, SHA-256
    `aa1cd48317254985d8b700cc74953477d1b40c3022ce9aa4c95ed2b8327e1292`).
- `scripts/Build-Local.ps1` downloads them into the ignored `artifacts/references`
  cache, verifies SHA-256 and assembly identity, and records their identities beside
  the DLL hash in `artifacts/build-provenance.json`. A missing, corrupt or different
  file (including the installed newer loader) fails the build; there is no fallback
  to `UnityModManagerDir`.
- The pin file must define exactly UnityModManager (`UnityModManager.dll`) and
  0Harmony12 (`0Harmony12.dll`); a dropped or renamed pin is refused by the verifier
  and by the packaging checks.
- Direct MSBuild builds import `LoaderReferences.targets`. Before reference
  resolution, on every build (incremental or not), it re-verifies the cached bytes and
  the manifest. After reference-assembly selection, immediately before compilation, the
  list Csc receives (`@(ReferencePathWithRefAssemblies)`; each loader assembly found by
  file name or by assembly identity) must contain exactly the two verified files by full
  path. A stray HintPath, a substituted reference assembly, a same-stem or renamed file
  in the pinned directory, or the installed loader is refused even when its bytes equal
  the pin. The supported build routes are `scripts/Build-Local.ps1` and command-line
  MSBuild from Visual Studio Build Tools (see `docs/BUILD-AND-RELEASE.md`); IDE builds are
  not a supported or verified route for this repository. The fast up-to-date check is
  nevertheless disabled and the pin, verifier, guard and manifest are compile inputs, so
  an incremental MSBuild build refuses a pin-only or manifest-only change.
- `Info.json` `ManagerVersion` must be a JSON string equal to the pinned UnityModManager
  version (minus the revision), and the pin must equal the declared minimum.
- `scripts/Build-Local.ps1` records in its receipt a fingerprint of every material build
  input (tracked or new files under `src/`, `Directory.Build.props`, `.editorconfig`,
  `Info.json`, `LoaderReferences.targets`, the pin file and its verifier, the build
  wrapper `Build-Local.ps1`/`Common.ps1`, and the file name and SHA-256 of every assembly
  Csc receives for that configuration, as resolved by MSBuild itself, so the check follows
  MSBuild's own reading of the project and `GamePath.props`; identical bytes relocated
  elsewhere keep their identity) and the configuration; the receipt also lists those
  compiler inputs.
  `scripts/Package.ps1` (and `Qualify.ps1 -Package`) refuse a DLL whose receipt has
  different bytes, configuration, inputs or loader identities or malformed field types,
  and require the DLL read back from the produced ZIP to equal the receipt's DLL hash.
  Candidate admission and the exported receipt use the same typed receipt object (commit
  and dirty state type-checked), and candidate version checks read the staged DLL.
  The package manifest and candidate copies are written from the staged DLL, the receipt
  bytes read once and the validated archive (never by rereading build output), and name
  the producing build (receipt commit, configuration, inputs) separately from the
  packaging checkout.
- The standalone `scripts/Validate-Package.ps1` inspects and hashes one private copy of
  the ZIP and reads the packaged DLL's AssemblyRef table (in Windows PowerShell, in a separate process, so several packages can be
  validated in one session), requires exactly UnityModManager 0.31.1.0 and 0Harmony12
  1.2.0.1, requires `ManagerVersion` to be the string "0.31.1", accepts only the six
  allow-listed files (directory records only for the package's own folders, and only
  when their decoded content is empty) and requires the four documentation/licence entries to be UTF-8 text (no loader, Harmony, game or Unity DLL can be packaged, renamed or not). Its
  report records the packaged DLL's SHA-256, MVID and observed loader references.
  `scripts/Install.ps1` copies a supplied ZIP to a private location, validates that copy
  and extracts that same copy (`Expand-ValidatedPackage`); the extracted DLL must equal
  the validated one. That private validation also runs under `Install.ps1 -WhatIf`;
  only live Mods changes stay behind `ShouldProcess`.
- `UnityModManagerDir` still identifies the installed runtime for contract checks and
  installation; upgrading that installation does not change the production build.

The previous source manifest required 0.32.0 and earlier releases were compiled
against the installed UMM 0.33.0 (for example the published v0.2.0 DLL references
UnityModManager 0.33.0.0). Historical release records keep their original versions
and evidence; existing published ZIPs are unchanged.

## Reproduce the checks

```powershell
.\scripts\Build-Local.ps1                 # fetch/verify pins, gates, build, receipt
.\scripts\Test-LoaderBaseline.ps1         # failure-boundary suite on the committed HEAD
.\scripts\Validate-Package.ps1 -PackagePath <zip>   # the standalone pre-install validator
```

`Test-LoaderBaseline.ps1` clones the committed HEAD into a temporary directory whose
path contains a space, seeds its ignored cache, builds it with `Build-Local.ps1`, and
runs every check in a separate PowerShell or MSBuild process; this repository's
`artifacts` and the installed game are never written. It downloads (or accepts with
`-LegacyReleaseZip` / `-WrongLoaderPackage`) two hash-checked fixtures and only reads
them: the genuine v0.2.0 release ZIP (SHA-256 `43f99fab…6dadaad`, its DLL compiled
against UnityModManager 0.33.0.0) and the author's UnityModManager 0.32.4 NuGet package
(`a8438e92…082c9ec9`, DLL `1387468b…7bfd88`) as the known distinct wrong-loader fixture.
The owner's installed loader is only measured and reported. The 85 checks cover: cache
bytes (genuine, corrupt, missing, 0.32.4); dropped/misnamed pins, raised or array-typed
manifests and raised pins; direct MSBuild with a corrupt and a genuine cache (the quoted
`csc` reference paths must be exactly the pinned files); incremental builds refusing a
manifest-only and a pin-only change and accepting the restored inputs; probe projects
whose UnityModManager resolves to an identical copy elsewhere, a same-stem `.exe` or a
renamed copy inside the pinned directory, or the 0.32.4 fixture, or is substituted as a
reference assembly after resolution; a Release-only Managed directory selected by MSBuild; packaging and
`Qualify.ps1 -Package` refusing a reused DLL after a source, helper, guard, project,
build-wrapper or resolved managed-reference change and accepting identical inputs or a
matching rebuild; producing-build provenance across commits; candidate admission refusing
an earlier commit's receipt (also when the receipt file is refreshed during packaging)
and malformed producer metadata; frozen candidate records after the build output is
replaced post-staging (in a disposable instrumented copy; the committed scripts contain no
test hooks); array/boolean receipt fields; receipts for another DLL,
configuration, observed version or loader; the archived-DLL/receipt comparison;
packages with added or swapped-in Harmony, loader or game DLLs, DLL bytes under
documentation/licence names, data-bearing directory records (and an accepted
level-0 DEFLATE empty directory), a caller ZIP replaced after the validator's snapshot, an
explicitly empty expected digest, and array/boolean/number `ManagerVersion`; the v0.2.0 DLL
with only its manifest lowered (refused); both packages validated in one Windows
PowerShell session in either order; and the validated private-copy extraction used by
`Install.ps1`, including the real `Install.ps1 -WhatIf` against a disposable
configuration (valid ZIP previews the target, invalid ZIP refused, temporary files
removed, disposable Mods folder unchanged).

Static API audit (the four-loader table below): the fleet mission's `LoaderApiAudit` tool
(a small .NET 8 console
program using System.Reflection.Metadata) resolves every TypeRef and MemberRef the DLL
makes into UnityModManager, 0Harmony12 and 0Harmony, by name and decoded signature with
base-type walk and type forwarders, against each profile's DLLs:
`LoaderApiAudit --mod <dll> --profile <label>=<UnityModManager.dll>;<0Harmony.dll>;<0Harmony12.dll> ...`.
Construct a profile from the author's NuGet `unitymodmanager.<v>.nupkg`
(`lib/net35/UnityModManager.dll`), the official `Lib.Harmony` package of the Harmony2
version that UnityModManager.dll references (`lib/net48/0Harmony.dll`) and the pinned
0Harmony12; the hashes are in the table. The tool is outside this repository; the same
method can be repeated with any metadata reader.

## Evidence for this change (2026-11-23)

Machine-generated records for the candidate are committed as structured files (no logs)
in [`docs/evidence/umm-0311/`](evidence/umm-0311/INDEX.md): the build record extracted from
the `Build-Local.ps1` run (every compiler invocation's inputs, test and contract result
lines), the build receipt, the package manifest and validation report, the boundary-suite
results, the static audit, a full loader-profile inventory (every component's identity,
SHA-256, MVID and references), the newest-stable selection record and the audit tool's
source. Local path prefixes are replaced by placeholders (see the index); the index names
the producing commit, case count and artifact identities, and supersedes counts quoted
elsewhere.

Candidate DLL (Release, built by `Build-Local.ps1` on Windows against the pinned
cache, Kingmaker 2.1.7b managed assemblies):
SHA-256 `634f22775c62bb34461fb202d85ab9d088f4e1cb1bd062e959812c8bcf9b12ee`,
MVID `7a6d0a75-f788-47a5-83c9-fed6fb14e3ff`; AssemblyRefs UnityModManager 0.31.1.0 and
0Harmony12 1.2.0.1. The build ran the repository validator, source oracle, 416/416
domain tests, Kingmaker/respec/native-UI contract checks, and the build gate.
`Test-LoaderBaseline.ps1`: 85/85 checks passed on the candidate commit (package SHA-256
`b8cb456b8061fc48bfa464bd5af66f368516ab24cbb22d3d1f2c6e376d436e81`), including the
v0.2.0 regression. The newest-stable row was selected on 2026-11-23 from the author's
NuGet feed (0.33.4 published 2026-11-19, the highest version; project URL the Nexus
page) and confirmed by the author's GitHub HEAD `3d14bf0d` (`<Version>0.33.4</Version>`).

Four observations are kept separate:

| Observation | Result |
| --- | --- |
| 1. Declared minimum | UMM 0.31.1 (`Info.json`) |
| 2. Verified compile baseline | UnityModManager 0.31.1.0 + 0Harmony12 1.2.0.1, pinned bytes above, confirmed on the `csc` command line and in the DLL's AssemblyRefs |
| 3. Static/API compatibility | See the table below (same candidate DLL) |
| 4. Observed native behaviour | **NOT RUN** for every loader version (see below) |

Static/API audit: every type and member reference the candidate DLL makes into
UnityModManager, 0Harmony12 and 0Harmony (7 types, 25 members) was resolved by name and
decoded signature against each profile's real assemblies, following base types and type
forwarders. Profiles are component-assembled, not official UMM distribution ZIPs: the
author's NuGet `unitymodmanager` `lib/net35/UnityModManager.dll` plus the Harmony2 that
DLL references, taken from the official `Lib.Harmony` package's `lib/net48` build (the
variant UMM installs), plus the pinned 0Harmony12. The owner row is a read-only copy of
the live installation.

| Loader | UnityModManager (SHA-256) | 0Harmony (Harmony2) | 0Harmony12 | Static result |
| --- | --- | --- | --- | --- |
| 0.31.1 (baseline) | 0.31.1.0 `2e038ff2…` | 2.3.1.1 net48 `5f7ad0df…` | 1.2.0.1 `aa1cd483…` | all resolved |
| 0.32.4 | 0.32.4.0 `1387468b…` | 2.3.6.0 net48 `df043f05…` | 1.2.0.1 `aa1cd483…` | all resolved |
| 0.33.0 (owner install, measured 2026-11-23) | 0.33.0.0 `63e5baf7…` | 2.3.6.0 `df043f05…` | 1.2.0.1 `aa1cd483…` | all resolved |
| 0.33.4 (newest stable: author's NuGet 2026-11-19, GitHub `3d14bf0d`) | 0.33.4.0 `c5ed5617…` | 2.4.3.0 net48 `ddc070b5…` | 1.2.0.1 `aa1cd483…` | all resolved |

This static audit shows the referenced names and signatures exist. It does not prove
assembly binding, reflection by name, Harmony patch application, Unity integration,
UI behaviour or save compatibility, and it is not a compatibility claim.

Native runtime: **NOT RUN** on 0.31.1, 0.32.4, 0.33.0 and 0.33.4. This repository has
no disposable-game harness; its runtime route installs into the live game, and this
change must not replace the owner's live loader, mods or saves. The candidate is
therefore not runtime-verified on any loader, including the 0.31.1 minimum. No
combined mod-stack check was run.

## Required native qualification (before claiming runtime support)

| Runtime | Required disposition |
| --- | --- |
| UMM 0.31.1 | Mandatory: disposable-game smoke on the same candidate DLL before claiming support |
| UMM 0.32.4 | Compatibility target; same smoke, same DLL |
| UMM 0.33.0 / 0.33.4 | Compatibility targets; old evidence does not qualify the new baseline build |
| Other versions | Not qualified; record the exact loader and Harmony identities before testing |

For every run record the mod DLL hash, UMM/Harmony hashes and versions, game
version, loaded mod set, settings round trip, enable/disable outcome, product
behaviour and save/reload outcome. Use only a disposable game copy; the developer's
live installation and saves must not be downgraded or replaced.

For Roll For Stats, exercise character creation, roll/assignment and the final
created scores, then save/reload in a fresh process. Check the settings panel and
saved-array slots. Report mercenary and respec provider scenarios separately from
ordinary creation.
