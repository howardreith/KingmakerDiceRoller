# 0.1.12 native stat UI candidate evidence

## Identity and scope

- Branch: `codex/inline-native-stat-ui`.
- Baseline: `ee49f104fb38389cbba739b3900df401c59ec5c7`, published
  v0.1.11 record. The baseline deterministic runner passed 397/397.
- Candidate version: `0.1.12`; package identity and hash are recorded in the
  ignored `artifacts/candidates/<commit>/package-manifest.json` after clean
  candidate qualification. This document does not authorize publication.
- Changed behavior: native row swap overlays, two-row inline toolbar, direct
  preset menu, bounded Options/Load/Details menus, structured return status,
  and legacy drawer recovery on failed inline attachment.
- Unchanged mechanics: source-index assignment, exact point-buy restoration,
  session history, saved schema 2, derived allowances, authoritative replay,
  supported ownership policy, and native point-buy admission.

## Source and installed-contract evidence

| Lane | Result | Evidence |
| --- | --- | --- |
| Deterministic C# | PASS, 403/403 | `scripts/Test-Domain.ps1`; ignored `artifacts/native-inline-build-final-precommit.txt` |
| Python source oracle | PASS, 30/30 | `scripts/Test-SourceOracle.ps1`; same log |
| Repository validator | PASS, 18 groups | `artifacts/source-qualification.json` |
| Release-gate tests | PASS, 20/20 | `scripts/Test-ReleaseQualificationGate.ps1`; same log |
| Kingmaker 2.1.7b contracts | PASS | `artifacts/contracts/runtime-contracts.json`; `Verify-KingmakerContracts.ps1` checks inherited row `Type`, `m_Types`, and the allocator `Init()` IL references |
| Respec contracts | PASS, 11 groups | `artifacts/contracts/respec-contracts.json` |
| Native UI IL contracts | PASS, 30/30 | `artifacts/contracts/native-ui-contracts.json` |
| Release build | PASS, zero warnings/errors | `scripts/Build-Local.ps1`; same log |
| Candidate ZIP | Pending clean commit | `scripts/Qualify.ps1 -Build -Package -Candidate` |

`CharBAbilityScoresAllocator.Init()` writes the inherited
`CharSScoreEntry.Type` from the corresponding `m_Types` entry. The resolver
requires that exact field; `NativeScoreRowIdentity.Map` rejects missing,
duplicate, and unknown keys. The host verifies each row and button belongs
to the exact allocator's stat container before attaching. Decoded main-menu
and in-game PC scenes each show six 600×50 native rows, 34×34 arrow buttons,
and a stat container followed by the racial selector. Scene metadata is
local/ignored evidence under `artifacts/native-ui-reskin/`; no game asset is
committed or packaged.

The `NativeViewCommandBinding` tests reject changed controller, stable owner,
allocator, and inactive phase. The UI guard also requires the router's current
ownership eligibility and exact live allocator. Existing coordinator tests
cover unchanged source-position swaps, duplicate values, derived allowances,
history/recall, observed-budget restoration, and authoritative completion.
The direct preset test proves selection calls the configuration setter with
zero Roll/Reroll calls. Point Buy History visibility has a focused presenter
test. New code creates one listener per owned button and does not replace
native listeners or patch point-buy arithmetic methods.

## Acceptance status by lane

`PASS` below is confined to the named evidence lane. A scenario is fully
accepted only when its actual-game lane passes for the exact packaged
candidate.

| Mission IDs | Source/contract result | Actual-game result |
| --- | --- | --- |
| UI-01 through UI-05 | PASS for attachment code, bounds checks, donor contracts, and menu ownership | NOT RUN: screenshots, fit, focus, click-through, sound, and interaction |
| UI-06 | PASS for next-roll configuration and applied-rule presentation tests | NOT RUN |
| BEH-01 through BEH-10 | PASS for existing coordinator/domain mechanics and new direct-preset/Point Buy History tests | NOT RUN on inline controls |
| PB-01 through PB-06 | PASS for exact restoration/derived-state service tests and structured outcome access | NOT RUN on new toolbar/native page |
| LIFE-01 | PASS for installed field/IL contract and six-key mapping tests | NOT RUN for live row interaction |
| LIFE-02 through LIFE-06 | PASS for lifecycle/ownership code and focused stale-binding tests; fallback path exists | NOT RUN for object counts, click dispatch, and actual teardown |
| New main, mercenary, qualified respec, six resolutions | Existing ownership/contract source checks PASS | NOT RUN |
| Negative contexts, completion, persistence, compatibility | Existing source regressions PASS; no new adapter | NOT RUN on this UI candidate |

The computer-use helper exited unexpectedly twice before it could list any
desktop windows. No actual game launch, installation, screen capture, click,
keyboard Submit, audio, or resolution check was performed. A prior v0.1.11
desktop blocker also exists in `PROJECT-STATE.md`; neither is converted to a
runtime pass. The current installed mod was left untouched.

## Focused live evidence still required

Follow `docs/SMOKE-TEST.md` under “0.1.12 inline native-stat candidate” with
the exact candidate package. Capture Point Buy, Roll with six native rows,
open Method, Options, Load, restored partial Point Buy, and constrained
geometry. Record actual canvas/allocator/toolbar/racial-selector bounds and
one-click/one-action, keyboard, audio, native button suppression, and
teardown observations. A fallback on an intended supported PC screen is a
visual failure to investigate, not acceptance.
