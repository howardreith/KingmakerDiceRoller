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
- Direct MSBuild and IDE builds import `LoaderReferences.targets`. Before reference
  resolution it re-verifies the cached bytes; after resolution it requires that the
  compiler's actual inputs (`@(ReferencePath)`) contain exactly one UnityModManager and
  one 0Harmony12, both inside the pinned directory. A stray HintPath or the installed
  loader is refused even when its bytes are identical to the pin.
- `Info.json` `ManagerVersion` must equal the pinned UnityModManager version (minus the
  revision), and the pin must equal the declared minimum.
- `scripts/Package.ps1` refuses a DLL whose build receipt has different bytes or
  different loader identities. The standalone `scripts/Validate-Package.ps1`, which
  `scripts/Install.ps1` runs on any supplied ZIP before extracting it, reads the
  packaged DLL's AssemblyRef table, requires exactly UnityModManager 0.31.1.0 and
  0Harmony12 1.2.0.1, requires `ManagerVersion` 0.31.1, and accepts only the six
  allow-listed files (no loader, Harmony, game or Unity DLL can be packaged). Its
  report records the packaged DLL's SHA-256, MVID and observed loader references.
- `UnityModManagerDir` still identifies the installed runtime for contract checks and
  installation; upgrading that installation does not change the production build.

The previous source manifest required 0.32.0 and earlier releases were compiled
against the installed UMM 0.33.0 (for example the published v0.2.0 DLL references
UnityModManager 0.33.0.0). Historical release records keep their original versions
and evidence; existing published ZIPs are unchanged.

## Reproduce the checks

```powershell
.\scripts\Build-Local.ps1                 # fetch/verify pins, gates, build, receipt
.\scripts\Test-LoaderBaseline.ps1         # failure-boundary tests (needs network once for the v0.2.0 release ZIP,
                                          # or -LegacyReleaseZip <path>; ZIP SHA-256 43f99fab...6dadaad)
.\scripts\Validate-Package.ps1 -PackagePath <zip>   # the standalone pre-install validator
```

`Test-LoaderBaseline.ps1` runs each check in its own PowerShell process, using real
files: genuine/corrupt/missing/installed-loader cache bytes; direct MSBuild of the
production project with a corrupt cache and with the genuine cache (the `csc` command
line must name exactly the pinned files); probe projects whose UnityModManager resolves
outside the pinned directory; manifest/pin disagreement on copies of the real scripts;
build receipts against other DLL bytes or other loader identities; packages with an
added or swapped-in Harmony, UnityModManager or game DLL; and the genuine v0.2.0 release
(compiled against UnityModManager 0.33.0.0) with only its packaged manifest lowered to
0.31.1, which the standalone validator must reject.

## Evidence for this change (2026-11-23)

Candidate DLL (Release, built by `Build-Local.ps1` on Windows against the pinned
cache, Kingmaker 2.1.7b managed assemblies):
SHA-256 `634f22775c62bb34461fb202d85ab9d088f4e1cb1bd062e959812c8bcf9b12ee`,
MVID `7a6d0a75-f788-47a5-83c9-fed6fb14e3ff`; AssemblyRefs UnityModManager 0.31.1.0 and
0Harmony12 1.2.0.1. The build ran the repository validator, source oracle, 416/416
domain tests, Kingmaker/respec/native-UI contract checks, and the build gate.
`Test-LoaderBaseline.ps1`: 23/23 checks passed, including the v0.2.0 regression.
The candidate commit and package hash are recorded in the pull request.

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
