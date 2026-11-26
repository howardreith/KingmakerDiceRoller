<!-- agent-review:v1 enrollment=kdr-umm0311-codex-umm-0311-baseline-20261123-20261123161619-b437 key=r01-request part=1/1 sha256=5c6d857e1f99a6536fbba34d4c53958955bfb38af107ef18f8306a9c72000d7f -->
### Claude Opus — review request — round 1 — candidate `65c3580bcb`

| Field | Value |
|---|---|
| Request ID | `kdr-umm0311-codex-umm-0311-baseline-20261123-20261123161619-b437-r01-cc943f` |
| Checkpoint | `final` (final) |
| Candidate H | `65c3580bcb314c2e1df90ab5d20a6c48d78fb48f` |
| Target B (`main`) | `9046aefc3c1e096712e46b297b2b232ed7441a0f` |
| Merge base M | `9046aefc3c1e096712e46b297b2b232ed7441a0f` |
| Mission | `kingmaker-umm-0311-mission-20261123` sha256 `f7d990f396` |
| Workflow | `6d613defe7` |
| Owner guidance | none |
| Owner amendments | none |
| Developer observed | foreground session `e178a93b-ca53-4b6b-a4d2-a357d48eaa77`: models ['claude-opus-5-5'], effort ['high'], permission modes ['auto'] (CLI-recorded transcript; the headless permission profile does not apply to a foreground session) |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `max`, read-only sandbox |
| Round budget | 1 of 5 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

Dice Roller UMM 0.31.1 baseline, adopting PR #9 (codex/umm-0311-baseline-20261123; the earlier commits f18068a/2403541 were authored on an offline host and never built). This repository's scope only (the fleet mission's other six repositories are coordinated separately).

State reconciled: Info.json ManagerVersion 0.31.1; production csproj references UnityModManager and 0Harmony12 only from the pinned artifacts/references cache (UnityModManager 0.31.1.0 from the author's NuGet package, package sha 05a82b66..., DLL sha 2e038ff2...; 0Harmony12 1.2.0.1 sha aa1cd483... from newman55 commit 10cfb1cc). Get-PinnedLoaderReferences.ps1 downloads/verifies (SHA-256 + assembly identity), never falls back to the installed loader, and enforces ManagerVersion == pin == declared minimum.

Changes added in this round (4ca3280, 65c3580):
1. LoaderReferences.targets: new AssertPinnedLoaderReferencesResolved target after ResolveAssemblyReferences/before CoreCompile requires exactly one UnityModManager and one 0Harmony12 in @(ReferencePath), both inside the verified pinned directory, so the compiler's resolved input (not only the selected HintPath) is the baseline on direct MSBuild/IDE builds too.
2. Validate-Package.ps1 / Common.ps1: the standalone pre-install validator's report now records the packaged DLL's SHA-256, MVID and observed loader AssemblyRefs (Assert-PinnedAssemblyReferences returns them).
3. scripts/Test-LoaderBaseline.ps1 (new): 23 integration checks, each in its own powershell.exe process (as Install.ps1 runs the validator; avoids ReflectionOnlyLoad identity collisions): genuine/corrupt/missing/installed-0.33.0 cache bytes; direct MSBuild of the production csproj with a corrupt cache (refused) and the genuine cache (csc command line names exactly the pinned UnityModManager.dll and 0Harmony12.dll); probe projects whose UnityModManager resolves to an identical copy elsewhere or to the installed loader (refused by the new target); ManagerVersion raised above the pin and pin raised above the minimum on copies of the real scripts (refused) with an unmodified control (accepted); build receipts vs other DLL bytes, without loader refs, or recording another loader (refused) and the matching build (accepted); the real candidate package via Package.ps1 (accepted by the standalone validator) and copies with an added 0Harmony12.dll, swapped-in UnityModManager.dll and swapped-in Assembly-CSharp-firstpass.dll (refused); and the required regression: the genuine published v0.2.0 release ZIP (sha 43f99fab..., its DLL references UnityModManager 0.33.0.0, checked in the test) with only Info.json lowered to 0.31.1 is refused by the standalone Validate-Package.ps1 with 'must reference pinned UnityModManager 0.31.1.0'. The summary records the build receipt's commit/dirty state and warns if artifacts are stale.
4. Docs: docs/UMM-COMPATIBILITY.md rewritten with the contract, reproduction commands, measured evidence and four separated observations; PROJECT-STATE current truth list updated to measured facts (historical 0.2.0 sections untouched; an accidental deletion of the '0.2.0 stable release' record in 4ca3280 was restored byte-exact in 65c3580); README and BUILD-AND-RELEASE no longer say the build is pending.

Newer loaders: static API audit (fleet tool C:/Dev/missions/kingmaker-umm-0311/tools/LoaderApiAudit, System.Reflection.Metadata; resolves every TypeRef/MemberRef into UnityModManager/0Harmony12/0Harmony by name + decoded signature with base-type walk and forwarders) on the candidate DLL 634f2277...: all 7 types / 25 members resolved on 0.31.1, 0.32.4, owner-install 0.33.0 and 0.33.4 (newest stable: author's NuGet 2026-11-19, GitHub HEAD 3d14bf0d csproj Version 0.33.4). Profiles are component-assembled (author NuGet UnityModManager + official Lib.Harmony net48 matching each UMM's own 0Harmony reference + pinned 0Harmony12) and labelled so; a negative control (profile without UnityModManager) reported all 25 unresolved. Static only; no compatibility claim.

Native runtime: NOT RUN on all loader versions. Dice has no disposable-game harness; its install/evidence route targets the live game, which this mission forbids changing. The candidate is not runtime-verified, including on 0.31.1.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/agent_review.py verify --enrollment kdr-umm0311-codex-umm-0311-baseline-20261123-20261123161619-b437 --gate domain | yes | pass | ~2s (incremental MSBuild)  | Controller gate receipt on 65c3580: Test-Domain.ps1, 416/416 domain tests. Pure domain only; no loader or game assemblies. |
| powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Build-Local.ps1 (clean 65c3580) | no | pass | ~10s | Fetch/verify pins, Validate-Repository, source oracle, release-gate 20/20, 416/416 domain tests, Kingmaker/respec(11)/native-UI(30) contract checks against installed Kingmaker 2.1.7b, Release build. DLL sha256 634f22775c62bb34461fb202d85ab9d088f4e1cb1bd062e959812c8bcf9b12ee, MVID 7a6d0a75-f788-47a5-83c9-fed6fb14e3ff, AssemblyRefs UnityModManager 0.31.1.0 + 0Harmony12 1.2.0.1; csc command line shows both from artifacts/references. Receipt commit 65c3580, dirty false. |
| powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-LoaderBaseline.ps1 -ReportPath <evidence> | no | pass | ~60s | 23/23 failure-boundary checks on the clean 65c3580 build (receipt commit 65c3580, not dirty); candidate package sha256 b8cb456b8061fc48bfa464bd5af66f368516ab24cbb22d3d1f2c6e376d436e81 (README differs from main so the package hash is specific to this commit). Install.ps1 itself was not executed (it installs into the live game); the check that it runs the validator before extracting is a source-order check. |
| LoaderApiAudit --mod KingmakerDiceRoller.dll (634f2277) --profile 0.31.1/0.32.4/owner-0.33.0/0.33.4 | no | pass | ~1s | Static metadata resolution only; does not prove binding, reflection by name, Harmony patch behaviour, Unity integration or save compatibility. |
| Native in-game smoke on UMM 0.31.1 / 0.32.4 / 0.33.0 / 0.33.4 | no | not_run | 0 | No disposable-game harness in this repository; the live installation must not be modified. Recorded as NOT RUN in docs/UMM-COMPATIBILITY.md and PROJECT-STATE.md. |

**Evidence references:**
- docs/UMM-COMPATIBILITY.md
- scripts/Test-LoaderBaseline.ps1
- LoaderReferences.targets
- C:/Dev/missions/kingmaker-umm-0311/evidence/dice-65c3580-build-local.log
- C:/Dev/missions/kingmaker-umm-0311/evidence/dice-65c3580-loader-baseline.json
- C:/Dev/missions/kingmaker-umm-0311/evidence/dice-api-audit-634f2277.json
- C:/Dev/missions/kingmaker-umm-0311/loaders/PROFILES.md

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
