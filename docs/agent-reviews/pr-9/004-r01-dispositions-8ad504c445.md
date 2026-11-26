<!-- agent-review:v1 enrollment=kdr-umm0311-codex-umm-0311-baseline-20261123-20261123161619-b437 key=r01-dispositions-8ad504c445 part=1/1 sha256=8ad504c445e1db0fd9c0c10b2317e24b097f3e901f0341f4c69f42615633d9b6 -->
### Claude Opus — finding dispositions — round 1

Candidate after corrections: `b3018c1b97aa69a7a545ecdf7579cee5d1e011ac`

- **R1-F1** — `fixed` — commits: `4deddcf742`, `b3018c1b97`
  Test-LoaderBaseline.ps1 run on clean b3018c1 (output printed in this round's transcript, 58/58): package-reuse-after-{production-source,reference-helper,msbuild-guard,project-file}-change-rejected and qualify-package-reuse-after-source-change-rejected ('Build inputs changed...'), package-after-restoring-identical-inputs-accepted, rebuild-after-source-change-accepted + package-after-matching-rebuild-accepted, receipt-other-configuration-rejected; all in a temporary Git clone.
- **R1-F2** — `fixed` — commits: `4deddcf742`, `b3018c1b97`
  Test-LoaderBaseline.ps1 run on clean b3018c1 (output printed in this round's transcript, 58/58): receipt-of-first-build-for-changed-dll-rejected (DLL 27c0a839... vs first receipt 634f2277...), archived-dll-vs-first-receipt-rejected (Validate-Package -ExpectedDllSha256), receipt-conflicting-observed-version-rejected (assembly_version 0.33.0.0), expand-validated-package-candidate/-legacy-rejected (private validated copy is the extracted copy), install-uses-validated-private-copy.
- **R1-F3** — `fixed` — commits: `4deddcf742`, `b3018c1b97`
  Test-LoaderBaseline.ps1 run on clean b3018c1 (output printed in this round's transcript, 58/58): pin-definition-without-harmony12-rejected and pin-definition-misnamed-harmony12-rejected with a populated cache; resolved-input-same-stem-file-in-pinned-dir-rejected (UnityModManager.exe) and resolved-input-renamed-loader-in-pinned-dir-rejected (umm-alias.dll, detected by FusionName); msbuild-direct-compiles-against-pinned-inputs still passes with exact full paths.
- **R1-F4** — `fixed` — commits: `4deddcf742`, `b3018c1b97`
  Test-LoaderBaseline.ps1 run on clean b3018c1 (output printed in this round's transcript, 58/58): incremental-build-genuine-accepted, incremental-build-up-to-date-accepted, incremental-build-after-manifest-only-change-rejected, incremental-build-after-pin-only-change-rejected, incremental-build-after-restore-accepted (ordinary MSBuild /t:Build). DisableFastUpToDateCheck=true and the pin/verifier/guard/manifest are compile and up-to-date inputs. Limitation: Visual Studio itself was not driven.
- **R1-F5** — `fixed` — commits: `4deddcf742`, `b3018c1b97`
  Test-LoaderBaseline.ps1 run on clean b3018c1 (output printed in this round's transcript, 58/58): all builds/packages run in a temporary clone; legacy-input-preserved (private copy and supplied original still 43f99fab...); repository-package-outputs-untouched (artifacts/packages hashes identical before/after, including the KingmakerDiceRoller-0.2.0.zip at the conventional path). The suite was run more than once this session with the same result.
- **R1-F6** — `fixed` — commits: `4deddcf742`, `b3018c1b97`
  Test-LoaderBaseline.ps1 run on clean b3018c1 (output printed in this round's transcript, 58/58): wrong-loader cases use the hash-pinned UnityModManager 0.32.4 NuGet fixture (cache-umm-0.32.4-fixture-rejected, resolved-input-umm-0.32.4-fixture-rejected, receipt-recording-other-loader-rejected); the owner install is only measured and reported (0.33.0.0 63e5baf7...); the clone path contains a space and the quoted csc /reference paths are parsed completely (printed in msbuild-direct-compiles-against-pinned-inputs).
- **R1-F7** — `fixed` — commits: `4deddcf742`, `b3018c1b97`
  Test-LoaderBaseline.ps1 run on clean b3018c1 (output printed in this round's transcript, 58/58): package-manager-version-{array,boolean,number}-rejected with the genuine scalar package accepted (package-candidate-accepted); manifest-array-version-rejected for the verifier; Common.ps1 Get-PinnedReferenceDefinitions also requires a string.
- **R1-F8** — `fixed` — commits: `4deddcf742`, `b3018c1b97`
  Test-LoaderBaseline.ps1 run on clean b3018c1 (output printed in this round's transcript, 58/58): package-harmony-bytes-under-readme-name-rejected and package-loader-bytes-under-licence-name-rejected ('Package text entry contains binary content'), with all six entry names intact; genuine package accepted.
- **R1-F9** — `fixed` — commits: `4deddcf742`, `b3018c1b97`
  Printed in this round's transcript at b3018c1: Build-Local output with contract results and the csc command line naming the pinned files, artifacts/build-provenance.json, pinned cache hashes/versions, the 58-case output including the archived DLL hash/MVID/AssemblyRefs, ZIP hash b8cb456b..., positive/negative standalone validator results, the v0.2.0 manifest-only case, and both archives validated in one Windows PowerShell host in either order (same-host-validation-*).
- **R1-F10** — `fixed` — commits: `4deddcf742`, `b3018c1b97`
  docs/UMM-COMPATIBILITY.md now documents the audit method, invocation and how to construct each profile from the author's NuGet UnityModManager + official Lib.Harmony net48 + pinned 0Harmony12. Printed in this round's transcript: loaders/PROFILES.md, package hashes, the NuGet version index (0.33.4 highest) and GitHub 3d14bf0d csproj Version 0.33.4, and the audit summary for DLL 634f2277 with every component identity/hash per profile (all resolved, 0 unresolved). The tool itself lives in the owner's fleet mission folder, not in this repository (AGENTS.md forbids local paths).
- **R1-F11** — `fixed` — commits: `4deddcf742`, `b3018c1b97`
  docs/NATIVE-THEME-RUNTIME.md Temporary probe paragraph now says the probe compiles against installed Kingmaker/Unity assemblies and the pinned UnityModManager 0.31.1.0 from scripts/loader-reference-pins.json via Initialize-PinnedReferences, with ManagerVersion 0.31.1, matching scripts/Build-NativeThemeRuntimeProbe.ps1; historical observations untouched.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/agent_review.py verify --enrollment kdr-umm0311-codex-umm-0311-baseline-20261123-20261123161619-b437 --gate domain | yes | pass | ~2s | Controller receipt on b3018c1: 416/416 domain tests; no loader/game assemblies. |
| powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Build-Local.ps1 (clean b3018c1) | no | pass | ~10s | Receipt commit b3018c1 dirty false, configuration Release, build_inputs_sha256 263c4e9e..., DLL 634f2277...; csc inputs are the pinned files. |
| powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-LoaderBaseline.ps1 -LegacyReleaseZip <v0.2.0 zip> -WrongLoaderPackage <umm 0.32.4 nupkg> | no | pass | ~2.5 min | 58/58 on a temp clone of b3018c1 (path with a space); candidate package b8cb456b8061fc48bfa464bd5af66f368516ab24cbb22d3d1f2c6e376d436e81. Install.ps1 not executed (live game); its helper Expand-ValidatedPackage is executed. The IDE itself was not driven; incremental MSBuild /t:Build (what the IDE invokes, with the fast up-to-date check disabled) was. |
| LoaderApiAudit --mod KingmakerDiceRoller.dll (634f2277) --profile 0.31.1 / 0.32.4 / owner-0.33.0 / 0.33.4 | no | pass | ~1s | Static only; component-assembled profiles labelled; no binding/reflection/Harmony/Unity/save claim. |
| Native in-game smoke on UMM 0.31.1 / 0.32.4 / 0.33.0 / 0.33.4 | no | not_run | 0 | No disposable-game harness; live installation must not be modified. |

_Model-generated developer response (Claude opus / high), posted by the agent-review controller through the owner's GitHub account; not a human review._
