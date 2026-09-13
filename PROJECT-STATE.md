# Project state

## Current 0.1.6 native book UI local candidate

Current candidate version: `0.1.6`; branch `codex/native-book-ui-reskin`.
Baseline `504b5ae53600be99a01f437b35ed57cdff6c3535` (clean main and locally
recorded origin/main). Presentation-only paper/button/type/message reskin;
mechanic and context services unchanged. Exact donors, offline evidence and
remaining gates: `docs/NATIVE-UI-STYLE.md`, `CODEX-NATIVE-UI-RESKIN-STATE.md`.

## Qualification truth

- Implemented: **Yes** — full native style with bounded usable fallback.
- Source-qualified: **Yes** — 18 source groups, 13/13 release-gate tests,
  347/347 C# cases, 30/30 Python cases.
- Contract-qualified: **Yes** — existing Kingmaker contracts, 11 respec groups,
  16 installed/candidate UI IL checks; exact donors inspected in both PC scenes.
- Build-qualified: **Yes** — Release, zero warnings/errors.
- Package-qualified: **Pending final clean build** — candidate-only six-file
  archive; the exact final manifest and hashes are recorded under ignored
  `artifacts/candidates/<clean-commit>/` after qualification.
- Installed: **No** — no active/test game installation by this mission.
- Focused runtime test: **NOT RUN** — guarded disposable workflow not authorized.
- Runtime-qualified: **No** — interactions, workflow/finalization/save regressions NOT RUN.
- Compatibility-qualified: **No** — mercenary/respec/provider/input-method live matrix NOT RUN.
- Human visual acceptance: **NOT RUN** — all twelve references and actual native
  resources inspected; no real candidate captures or motion evidence.
- Audio-qualified: **NOT RUN** — ordinary route source-qualified only.
- Release-authorized: **No** — no push, merge, PR, tag or release authorization.
- Testing-prerelease-authorized: **No**.
- Publicly released: **No** — historical 0.1.5 release remains separate.


## Historical 0.1.5 candidate — skills-counter and forward-navigation integrity

Current candidate version: `0.1.5` on branch `z/skills-counter-next-guard`
(baseline: `main` at `f9f7cf31`). IL inspection of the installed assembly
confirmed the two follow-up defects: the native red skill-points badge repaints
only through the dirty Skills phase, and the phase-unlock cache gating every
forward route is recomputed only inside `SetupUI` — so a changed rolled
assignment left a stale badge and allowed leaving Skills with an invalid skill
allocation, yielding an empty feats/traits page. The repair adds a bounded,
idempotent native skills-page refresh after every relevant change (including
drawer close) and a veto-only prefix on `CharacterBuildController.SetPhase`
(the single forward funnel) that blocks forward movement beyond Skills while
the live `LevelUpState.IsSkillPointsComplete()` is false. Evidence and the
open live lanes: `Z-SKILLS-COUNTER-STATE.md` and
`docs/CHARACTER-BUILD-INTEGRITY.md`.

After the 0.1.5 candidate was delivered, Howie instructed: "Please commit,
merge, push, and do a release. And make it an actual release, not a test
pre-release like the last one. We can make it 0.1.5 it's fine. No one else is
using this mod yet." That was owner release authorization for an
official (latest, non-prerelease) v0.1.5.

## 0.1.5 qualification record (historical)

- Implemented: **Yes** for new-main, mercenary, and the inspected native/Eddic
  respec path, including the skills-page synchronization and forward guard.
- Source-qualified: **Yes** — repository validation 13/13, 336/336 C# cases,
  30/30 Python cases.
- Contract-qualified: **Yes** — exact native contracts plus the skills-page
  refresh/transition members; eleven respec contract groups.
- Build-qualified: **Yes** — Release, zero warnings/errors, installed
  UMM/Harmony references.
- Package-qualified: **Yes** — deterministic 0.1.5 archive passed the
  six-file allowlist and hash gate.
- Installed: **Not performed by the agent**; owner-side installation only.
- Focused runtime test: **Owner-confirmed for the 0.1.4 rolled-stat workflow**
  (the owner played it and reported only the skills-badge/Next defects); the
  0.1.5 skills-page changes were accepted by the owner for direct official
  release without a testing-prerelease stage.
- Runtime-qualified: **Yes** — owner-directed official release per the
  instruction quoted above; the agent-executed interactive/save/provider
  matrix remains unexecuted and is recorded as NOT RUN below.
- Compatibility-qualified: **No** for the provider/configuration matrix.
- Human visual acceptance: **Owner release instruction** (quoted above); a
  separate in-play confirmation of the 0.1.5 skills-page behavior is still
  welcome and will be recorded when supplied.
- Release-authorized: **Yes** — the owner's instruction quoted above.
- Testing-prerelease-authorized: **Yes** historically; superseded by the
  explicit request for a direct official release.
- Publicly released: **Yes** — `v0.1.5` official (latest).

## Verified 0.1.5 publication

`v0.1.5` was published on 2026-09-11 at 20:33:03 UTC as the **official latest
release** by rerunning the full qualification from clean, fully pushed `main`
release commit `3117dac9606964442b0263ab8b86f72043897dc8` (13/13 repository
gates, 30/30 Python cases, 336/336 C# cases, exact native contracts including
the skills-page members, eleven respec contract groups, zero Release
warnings/errors). GitHub reports `isDraft=false`, `isPrerelease=false`, title
`Kingmaker Dice Roller v0.1.5`, and the latest-release endpoint returns
`v0.1.5`. The annotated tag resolves to the release commit (= `origin/main`).
Asset digests match the qualified local bytes:

- ZIP SHA-256: `b0b33305274785d089fb0f148a1b9e3e2812967070b8d3a00fce4c2f6877741e`
  (`KingmakerDiceRoller-0.1.5.zip`; the packaged README carries the finalized
  release text, so the archive differs from the earlier local-only candidate
  while the DLL is unchanged).
- DLL SHA-256: `591ef6eeaa1d8074ab32f644657f0b6246120b55d637e51b1d26b5353cf042e7`.
- `SHA256SUMS.txt` carries the same ZIP digest.

`v0.1.4` remains available as the prior testing prerelease. The
agent-executed interactive/save/provider matrix remains NOT RUN; the release
standing rests on the owner's explicit instruction recorded above.

## 0.1.4 testing prerelease — rolled-stat build integrity

Current candidate version: `0.1.4`. Branch `z/rolled-stat-build-integrity`
(baseline: released 0.1.3 `main` at `f4eba6d`) repaired the reported
rolled-stat build-integrity defects. Exact
2.1.7b IL inspection confirmed three divergences: stale native derived
allowances after staging (skill points/spell slots cached from the pre-roll
Intelligence), the mercenary authoritative Commit replaying actions against
pre-roll source scores before a late six-value correction (dropping
selections), and unverified new-main completion. The repair adds a shared
`DerivedStateRefreshService` transaction for every supported creation kind and
replaces the mercenary late correction with a one-use pre-replay commit ticket
plus a new-main final-recipient audit. Details and evidence are in
`Z-ROLLED-STAT-INTEGRITY-STATE.md` and `docs/CHARACTER-BUILD-INTEGRITY.md`.

On 2026-09-11, after reviewing the delivered mission, Howie instructed:
"Please finalize, merge, push to remote, and cut a new release." That is the
current owner release authorization. Because the interactive/save/provider
lanes were never executed by the agent, the publication follows the
established 0.1.3 pattern: `v0.1.4` is published as a **testing prerelease**
(not latest) and promotion to official is a metadata-only change awaiting the
owner's in-play confirmation.

## 0.1.4 qualification record (historical)

- Implemented: **Yes** for new-main, mercenary, and the inspected native/Eddic
  respec path.
- Source-qualified: **Yes** — repository validation 13/13, 328/328 C# cases,
  30/30 Python cases.
- Contract-qualified: **Yes** — exact native contracts plus the new
  derived-allowance members; eleven respec contract groups.
- Build-qualified: **Yes** — Release, zero warnings/errors, installed
  UMM/Harmony references.
- Package-qualified: **Yes** — deterministic 0.1.4 archive passed the
  six-file allowlist and hash gate.
- Installed: **Not performed by the agent**; owner-side installation only.
- Focused runtime test: **NOT RUN** — no game launch, save operation, or
  desktop control was authorized during development.
- Runtime-qualified: **No** — interactive, final-character, and persistence
  evidence is absent by design until the guarded section K procedure runs.
- Compatibility-qualified: **No** for the provider/configuration matrix.
- Human visual acceptance: **Pending** — owner confirmation of this prerelease
  in play.
- Release-authorized: **Yes** — the owner's 2026-09-11 instruction quoted
  above.
- Testing-prerelease-authorized: **Yes** — same instruction; prerelease chosen
  because runtime lanes are unexecuted.
- Publicly released: **Yes** — `v0.1.4` testing prerelease (not latest);
  official promotion pending owner in-play confirmation.

## Verified 0.1.4 publication

`v0.1.4` was published on 2026-09-11 at 19:16:31 UTC by rerunning the full
qualification from clean, fully pushed `main` (13/13 repository gates, 30/30
Python cases, 328/328 C# cases, exact native contracts including the new
derived-allowance members, eleven respec contract groups, zero Release
warnings/errors). GitHub reports `isDraft=false`, `isPrerelease=true`, title
`Kingmaker Dice Roller v0.1.4 (testing prerelease)`, and the latest-release
endpoint still returns `v0.1.3`. The annotated tag resolves to release commit
`748350bf9317ef8cb96803a7dbaf33e08fc6166f` (= `origin/main`). Asset digests
match the qualified local bytes:

- ZIP SHA-256: `19dc3b383ff16bffe8f022049032a5dbabc05756b92309255fde7d340ed9cf76`
  (`KingmakerDiceRoller-0.1.4.zip`).
- DLL SHA-256: `68467b41eea8aa0c959457de4de71d52ea053dd7a5558a15069e28e160172e13`.
- `SHA256SUMS.txt` carries the same ZIP digest.

Official promotion is a metadata-only change that reuses these exact package
bytes and awaits the owner's in-play confirmation; until then `v0.1.3` remains
the latest official release and the runtime/compatibility lanes below stay
NOT RUN.

## Current 0.1.3 official release

Version `0.1.3` was initially published as a testing prerelease on 2026-09-07
at 02:13:13 UTC. Howie subsequently confirmed that it works and authorized its
official release. Promotion preserves the same published package and tag.
Implementation is commit `4629adf3202f6d04d547f3a7ec7c805b89a9f221` on
`codex/respec-starting-scores`; publication uses the clean, pushed `main` branch.
The clean DATA checkout began at `bbf145014df0d1b889f811a655d9d183ccc1f087`, the
reviewed historical `0.1.2` main. No newer local source was replaced. The installed
0.1.2 DLL was inspected, not overwritten; its SHA-256 is
`962d5968d5021db2868d39104b4cbfc3911fe1de00ebde85974a1a2b1b977acd`.
The unchanged source baseline built to different bytes
(`1b1c8f12335636aa4e83da06971f2ffc2ec6fbb885bb6608b6e53bdc970754d5`);
same-version metadata was not treated as byte parity.

Howie first authorized publication for testing, then reported:
"Confirmed this work. Please make this a real official release."
This is current owner acceptance of the released 0.1.3 workflow and explicit
authority to promote it to official/latest. It does not enumerate individual
provider/character cases or save/reload results; those lanes are not inferred
from the confirmation. No agent-operated game launch, installation, or save
operation is part of this promotion.

## Confirmed local causes and repair

The installed/source policy explicitly rejected Respec. The separate immutable
Mercenary completion guard also rejected it, and a main-character respec clone
could fail the different-main identity check. The repair preserves recruitment's
finalization guard and adds a distinct scoped Respec lifecycle.

Exact installed Eddic 1.0 and Kingmaker 2.1.7b inspection established the selected
original -> native rebuild source -> preview -> native serialization into the
original entity path. Eddic retains Respec mode and rebuilds story companions
from zero. Native recruitment-level story retraining does not reopen starting
allocation. The native HandleLevelUpStart postfix supplies a fully bound owner
after constructor-time state is incomplete.

The initial assignment now reaches native action checks through the fresh state
inside the exact first-level Commit invocation. Replay is verified without late
rewrites. The exact native copy callback must complete before final recipient
verification; the original entity's current descriptor is reread. The session
closes before catch-up levels and subsequent native ability increases.

A new restoration regression exposed an additional confirmed service defect:
a pre-Roll allocation identical to the rolled array with a full budget was
rejected as a hybrid. It now passes only with independently captured origin
provenance and exact live values/budget/availability/ownership. The prior invalid
hybrid regression remains protected.

The development session did not run an interactive reproduction on DATA. Local
source and installed binary contracts confirmed the admission defect. Howie later
confirmed the released workflow works. That human acceptance is recorded separately
from service tests and does not supply unspecified per-provider or persistence results.

## Baseline and implementation evidence

The unchanged baseline passed the repository gate (8 cases), Python oracle
(30 cases), deterministic C# runner (283 cases), exact native contracts, and
Release compilation with zero warnings/errors. The initial build attempt failed
because PATH resolved Python to the Windows Store alias. Selecting DATA's existing
Python 3.12 directory in the process PATH resolved it; no prerequisite or global
configuration was changed.

The candidate adds 33 respec service cases (316 total), preserving the dice domain
and existing runner. Installed-assembly checks cover eleven contract groups,
including four actual-Harmony scope cases on owned test methods. They use DATA's
UMM 0.33.0, Harmony12 1.2.0.1, and HarmonyLib 2.3.6.0 without replacing libraries.
The scope tests run on desktop .NET, not Unity/Mono. Relevant source files and
replay details are documented in `docs/ARCHITECTURE.md` and
`docs/INTEGRATION-SEAMS.md`.

Native/Eddic selector ownership, negative locked/progression cases, preview
replacement without RNG, modified-budget and previously rolled origins,
pre-action authoritative staging, copy/overwrite detection, duplicate/reentered
commits, cancellation, exceptions, source rollback, disable, and owner loss have
service coverage. Existing layout/control regressions cover the reused panel;
the owner supplied overall workflow acceptance without a per-control visual record.

## 0.1.3 qualification record (historical)

- Implemented: **Yes** for the native PC selector and inspected Eddic 1.0 path.
- Source-qualified: **Yes** — repository validation, 316/316 C# cases, 30/30 Python cases.
- Contract-qualified: **Yes** — exact native contracts plus eleven respec contract groups; four Harmony scope cases.
- Build-qualified: **Yes** — Release, zero warnings/errors, installed UMM/Harmony references.
- Package-qualified: **Yes** - deterministic 0.1.3 archive passed the six-file allowlist and hash gate.
- Installed: **Not performed by the agent**; owner-reported use has no installed-byte parity record.
- Focused runtime test: **Owner-confirmed** for released 0.1.3 usage; detailed scenario results were not supplied.
- Runtime-qualified: **Yes** for the owner-confirmed workflow and release sign-off; the full matrix remains unverified.
- Compatibility-qualified: **No** for the exhaustive provider/character/configuration matrix.
- Human visual acceptance: **Owner sign-off**; no separate per-control screenshot record.
- Release-authorized: **Yes** - current owner instruction explicitly requests official release.
- Testing-prerelease-authorized: **Yes** - historical authorization for the initial 0.1.3 publication.
- Publicly released: **Yes** - `v0.1.3` is official, non-prerelease, and GitHub Latest.

The release package is `artifacts/packages/KingmakerDiceRoller-0.1.3.zip`.
Exact commit, dirty state, DLL/package hashes, contract reports and inventory are
recorded in ignored artifacts by the final qualification commands. No installed
parity or save/reload result is claimed. The installed profile, Steam state,
Cloud, and saves remain untouched.

## Verified publication

Official promotion was verified through GitHub: `isDraft=false`,
`isPrerelease=false`, title `Kingmaker Dice Roller v0.1.3`, and the latest-release
endpoint returns `v0.1.3`. Both asset IDs, SHA-256 digests, sizes, download URLs,
and the peeled annotated tag commit match the pre-promotion record. No package
was rebuilt or uploaded. Repository validation and all 13 publication-gate cases
pass; the stable publication gate accepts the scoped owner runtime sign-off.


[GitHub release v0.1.3](https://github.com/howardreith/KingmakerDiceRoller/releases/tag/v0.1.3)
was initially published as a prerelease. Its annotated tag resolves to release commit
`f9772f06b6d86338a0b22af94b86cbba1cc6d317`. Both implementation and release
finalization were fast-forwarded into `main` and pushed without rewriting history.

The publisher reran qualification from clean, fully pushed `main`: 316/316 C#
cases, 30/30 Python cases, 13/13 publication-gate cases, exact native contracts,
eleven respec contract groups, and zero Release warnings/errors. The public ZIP
and `SHA256SUMS.txt` were downloaded and compared with the qualified local bytes.

- ZIP SHA-256: `a298fa8d21879b9b45de08c8c6e321bd83056959b562801680ca00a24dcc7513`.
- DLL SHA-256: `7f1265aedab4f74c210f4181e8f0458d06710e791eebbcf1b7b2546a523e85d2`.
- DLL bytes match the tested implementation candidate; the ZIP changed because
  its packaged README now contains finalized release/install information.
- The original publication retained `v0.1.2` as latest stable. Official promotion
  changes release metadata only; no release assets or tags are replaced.
- The published ZIP keeps the original testing README. Current repository docs
  and release notes record the owner's subsequent acceptance and official status.

## Remaining provider and runtime lanes

See `docs/COMPATIBILITY.md` for the actual provider/configuration inventory and
separate service/contract/live matrix. newman55 Respecialization was absent from
the installed/test-available locations checked. Its exact launch modes,
Original-score/legacy settings, replacement callback, and final mercenary
recipient remain unresolved; no adapter was invented. Installed newman55 Cheat
Menu is a different mod. No additional installed full-respec engine was found.

Detailed development-session live lanes remain **NOT RUN**: creation/recruitment
regressions, native main and mercenary respec, native locked-base story negative control, Eddic main/mercenary/
story/remote and NPC entry, Bag of Tricks/CotW, full profile, later progression,
and save/reload including without Dice Roller. They require the specific guarded
procedure in `docs/SMOKE-TEST.md` and an owner-nominated disposable fixture. The
owner's release acceptance is complete; the unspecified detailed compatibility
and persistence lanes remain open.
