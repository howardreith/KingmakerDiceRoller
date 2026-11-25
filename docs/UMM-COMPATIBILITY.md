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
  not a supported or verified route for this repository: the owner stated on 2026-11-24 that
  this mod has never been opened or built in an IDE, and no IDE is installed on the build host, so no
  IDE up-to-date trace exists (an open, owner-accepted gap rather than a verified property). The fast up-to-date check is
  nevertheless disabled and the pin, verifier, guard and manifest are compile inputs, so
  an incremental MSBuild build refuses a pin-only or manifest-only change.
- `Info.json` `ManagerVersion` must be a JSON string equal to the pinned UnityModManager
  version (minus the revision), and the pin must equal the declared minimum.
- `scripts/Build-Local.ps1` records in its receipt a fingerprint of every material build
  input (tracked or new files under `src/`, the root `Directory.Build.props`, `Directory.Build.targets`
  and `Directory.Build.rsp`, `.editorconfig`,
  `Info.json`, `LoaderReferences.targets`, the pin file and its verifier, the build
  wrapper `Build-Local.ps1`/`Common.ps1`, and the compiler inventory) and the configuration.
  The compiler inventory is the exact Csc command line MSBuild produces for the production
  project (an opt-in `WriteCompilerReferenceList` target with `ProvideCommandLineArgs`; the
  evaluation is a `SkipCompilerExecution` pass through `PrepareResources` and `CoreCompile` in a
  fresh intermediate directory, so anything `GamePath.props` or another import sets is included):
  the effective compiler binary first (`/noconfig` is required: without it the compiler would read the
  default `csc.rsp` beside it, whose contents the receipt cannot bind, so an inventory without it is refused;
  MSBuild's Csc task always passes it, because `Microsoft.CSharp.Core.targets` sets `NoConfig="true"`), then
  every argument in the compiler's order - options verbatim (for example `/checked+`,
  `/baseaddress:...`), references and sources (generated ones included) by name and SHA-256, and
  file-valued options (`/win32res`, `/win32icon`, `/keyfile`, `/resource`, rule sets and others) by
  the content of the file they name. Identical bytes relocated elsewhere keep their identity; an
  input that cannot be identified refuses reuse. Forms whose material files the inventory cannot
  name are refused outright, at the build and at every receipt admission: an explicit compiler
  response file (`@file`), linked (embedded-interop) references (`/link`) and wildcard source
  recursion (`/recurse`). The production compile writes its own command line, which must equal the
  evaluated one entry by entry; the receipt records it, and packaging checks the recorded inventory
  (shape, including no control characters in names, then the same number of entries and, in order,
  the same kind, name and digest as what MSBuild evaluates now) before the fingerprint.
- Bounded local configuration. Because the inventory comes from an evaluation that skips the
  compiler, anything that could act only during a real compile is refused instead of trusted, by
  `Build-Local.ps1` and by every receipt admission (`Package.ps1`, `Qualify.ps1 -Package`): the ignored
  `GamePath.props` may only set `KingmakerInstallDir`, `KingmakerManagedDir` and `UnityModManagerDir`
  (conditions only on `Configuration` or `Platform`; values referring only to those properties; no
  imports, items, targets or property functions); for each of `Directory.Build.props`, `.targets` and
  `.rsp`, the file MSBuild imports for the production project (the first one found walking up from
  `src/KingmakerDiceRoller`; one further up is shadowed, and files beside other folders are not
  imported) must be inside the checkout and tracked by Git, and so must the production project's
  `.user` file if present;
  no environment variable that selects MSBuild imports (`DirectoryBuildTargetsPath`,
  `CustomAfterMicrosoftCommonTargets` and the related names); no per-user MSBuild
  `ImportBefore`/`ImportAfter` extension file. Compiler settings belong in tracked files such as
  `Directory.Build.props`, whose effective options the inventory records. Extension files that ship
  with the installed MSBuild are part of the toolchain and are not inventoried.
  `scripts/Package.ps1` (and `Qualify.ps1 -Package`) refuse a DLL whose receipt has
  different bytes, configuration, inputs or loader identities or malformed field types,
  and require the DLL read back from the produced ZIP to equal the receipt's DLL hash.
  Candidate admission and the exported receipt use the same typed receipt object (commit
  and dirty state type-checked), and candidate version checks read the staged DLL.
  The package manifest and candidate copies are written from the staged DLL, the receipt
  bytes read once and the validated archive (never by rereading build output), and name
  the producing build (receipt commit, configuration, inputs) separately from the
  packaging checkout. The packaged documents are read once and staged from those bytes; a
  candidate's documents must equal the admitted commit's files as Git checks them out
  (`git cat-file --filters`), so an edit after admission is refused. A candidate's
  qualification reports are read once and must describe this build: the respec and native-UI
  reports name the packaged DLL, the three contract reports name the game assembly the
  receipt's compile referenced, and the source qualification names the admitted `Info.json`.
  The whole candidate (ZIP, validation report, manifest, DLL, receipt and reports) is prepared
  in private storage and published once with a directory move that refuses an existing
  destination, so a failure leaves no commit directory and a competing same-commit publisher's
  files are kept. Ordinary publication of the ZIP, validation report and manifest runs one
  publisher at a time per destination (an exclusive lock file removed when its handle closes);
  both records are prepared first, and backup acquisition, placement and rollback are one
  recovery scope that tracks exactly the files this invocation moved and placed. If restoration
  itself fails, the previous files are kept (not deleted) and named.
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
The owner's installed loader is only measured and reported. The checks (the current count is in
`loader-baseline.json`, listed in the evidence index; the historical round-6 run recorded 111/111) cover: cache
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
level-0 DEFLATE empty directory, whose fixture is checked first: writer exit status, six files,
method 8, compressed size above two bytes, zero decoded bytes; a failed fixture write fails the
case), a validator whose input copy fails (exclusively held ZIP: reported, no private directory
left behind), a caller ZIP replaced after the validator's snapshot, an
explicitly empty expected digest, and array/boolean/number `ManagerVersion`; the v0.2.0 DLL
with only its manifest lowered (refused); both packages validated in one Windows
PowerShell session in either order; and the validated private-copy extraction used by
`Install.ps1`, including the real `Install.ps1 -WhatIf` against a disposable
configuration (valid ZIP previews the target, invalid ZIP refused, temporary files
removed, disposable Mods folder unchanged); and, from round 5, only the overflow-checking
option changed through the tracked `Directory.Build.props` (stale package refused, the matching rebuild's
receipt records the effective option and its DLL differs, removal refused until rebuilt; compiler settings
in `GamePath.props` are refused at the build and at package admission, whether conditioned on a real
compile or not), the
receipt's reference inventory equal to the real Csc `/reference` list, and a receipt with one
recorded reference digest changed or with no inventory refused on the ordinary package route;
and, from round 6, `BaseAddress` set in the tracked `Directory.Build.props` and an external `Win32Resource` replaced at
the same path (stale packages refused, rebuilt receipts record the option and the resource's digest, the
DLL changes, matching rebuilds accepted), the receipt naming the effective compiler first, a document
restaged after the staging copy (refused; the previously published package and records unchanged; no
private staging left), and a failure during publication (the previous package set restored exactly);
and, in the final review round, a tracked root `Directory.Build.targets` with a production-only overflow
setting added after the build (stale package refused, the build refused, accepted once removed), a
disposable compiler copy whose default `csc.rsp` names the external resource, selected with `NoConfig`
disabled (the build still passes `/noconfig` and reproduces the first DLL, so the file is not read; the
inventory reader refuses that run's compiler list with `/noconfig` removed), a shadowed parent `Directory.Build.props`
with an unrelated `tools/Directory.Build.targets` (the build reproduces the first DLL and the package is
accepted), and a repository-local Managed directory under ignored `artifacts` (the receipt names the game
assembly repository-relatively; the build and its matching local candidate are accepted). That directory is a
copy of the suite's disposable Managed copy: its `Assembly-CSharp.dll` is the game's bytes, but its
`UnityEngine.UI.dll` keeps the one-bit change made earlier in the run for the resolved-managed-reference case,
so the case tests the repository-local `Assembly-CSharp.dll` path, not a byte-identical copy of the whole folder
(the 140/140 run at `3874fc2` used the same fixture).

The owner-authorized extra round adds: an explicit `Import` in an applicable `Directory.Build.props`,
`Directory.Build.targets` or project `.user` file is refused at package admission and before Build-Local compiles,
because the imported file is not a fingerprinted input (a tracked root `Directory.Build.targets` importing the
parent above the checkout: the old receipt is refused, also after only the parent's production-only overflow
setting changes; the build is refused with the DLL and receipt unchanged; with the file removed the receipt is
accepted again). `Directory.Build.*` files below `src` that MSBuild does not import for the production project
are no longer fingerprinted, and only the per-user `ImportBefore`/`ImportAfter` folders of the selected MSBuild
toolset version are checked: with a shadowed parent `Directory.Build.props`, untracked `tools/` and
`src/Scratch/` `Directory.Build.targets` and a per-user hook for toolset 15.0 in a disposable user profile, the
existing DLL and receipt are packaged without rebuilding, then a rebuild reproduces the first DLL and is
packaged; a hook in the selected toolset's (`Current`) folder is refused, and an untracked
`src/Directory.Build.targets` stays refused.
`Package.ps1` stages, archives and validates in a folder private to the invocation, compares the
documents read back from the validated archive with the repository files hashed before staging,
prepares both records, and only then publishes the ZIP, validation report and manifest together, keeping
the previous set aside and restoring it if any publication step fails.

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

## Evidence for this change (2026-11-24, final review round)

The final round changes only build, packaging and test tooling (`Common.ps1`, `Package.ps1`,
`Test-LoaderBaseline.ps1`) and this guide; the product source and therefore the candidate DLL are
unchanged, so the build and package records below keep their original identities (index), and
`loader-baseline.json` is the new boundary-suite run on the corrected tooling. The candidate qualification
reports a local candidate carries (source, runtime, respec and native-UI contracts) are bound to the
build by the checks described above (status, packaged DLL digest, the game assembly the receipt's compile
referenced and the admitted `Info.json`); binding them further, for example to the Unity UI assembly the
native-UI check read, is not part of the UMM 0.31.1 baseline and is left as follow-up work.

### Previous submission (2026-11-24; labelled round 7 here at the time)

Machine-generated records are committed as structured files (no logs) in
[`docs/evidence/umm-0311/`](evidence/umm-0311/INDEX.md); the index names the producer of every
file. Local path prefixes are replaced by placeholders (see the index).

- `build-provenance.json` is the receipt of a clean `Build-Local.ps1` run of the documentation
  commit, and `build-local-summary.json` is a structured summary of that wrapper run's own result
  lines (repository validator, source oracle, domain tests, contract checks, the build and the
  recorded compiler inventory). There is no separate direct-MSBuild record any more; the direct
  compile is exercised by the boundary suite (`receipt-reference-inventory-equals-csc-inputs`
  compares the receipt's references with the direct `csc` command line).
- `package-manifest.json` and `package-validation.json` come from `Package.ps1` on that build.
- That submission's boundary suite recorded 129/129 at `8c198e0`; the current `loader-baseline.json`
  is the final round's run (the index gives the commit and the counts, including any host-gated case).
- The static audit and loader-profile inventory describe the candidate DLL
  `634f22775c62bb34461fb202d85ab9d088f4e1cb1bd062e959812c8bcf9b12ee` (MVID
  `7a6d0a75-f788-47a5-83c9-fed6fb14e3ff`, AssemblyRefs UnityModManager 0.31.1.0 and 0Harmony12
  1.2.0.1); the product source did not change in rounds 2 to 7, and the round-7 build reproduces
  that DLL. They are reused records, labelled as such in the index.
- The newest-stable row was selected on 2026-11-23 from the author's NuGet feed (0.33.4 published
  2026-11-19, the highest version; project URL the Nexus page) and confirmed by the author's GitHub
  HEAD `3d14bf0d` (`<Version>0.33.4</Version>`).

Earlier rounds (history): round 6 recorded `Test-LoaderBaseline.ps1` 111/111 on `dd54838` (package
`b8cb456b8061fc48bfa464bd5af66f368516ab24cbb22d3d1f2c6e376d436e81`). Its committed `build-record.json`
was a direct MSBuild compile record, not a `Build-Local.ps1` trace as this guide then said; that
record is withdrawn.

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
