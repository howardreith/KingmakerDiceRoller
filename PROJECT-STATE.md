# Project state

## Current 0.1.7 native-theme activation release

Version `0.1.7`; branch `codex/native-theme-activation-repair`, based on
`9b69751983a6bc80d1e73999d3fbba3e3e7f8927`. Historical `77d16ef` is an ancestor;
`v0.1.6` peels to `7662a4239512edd9be7a8d3c3ca511a1d9ce763a`. Both the release
ZIP's DLL and the installed pre-repair DLL match
`e2803d3f70ac168d52de27a075b36e6562c7209e709d02c62485f7ded0b13092`.
No reset or downgrade was performed. The owner reported the literal-name donor
failure and initially authorized a repair branch and review PR. On 2026-09-13,
after the candidate handoff, the owner instructed: "Could you please merge,
push, and release this as a new release? Make it an actual release, not a test.
Thank you." This explicitly authorizes merging PR #2, pushing main, tagging
v0.1.7 and publishing an official/latest release with the unrun checks disclosed.
It supersedes the earlier repair-only publication restriction, without claiming
runtime qualification. The existing draft-build/package workflow prepared
verified assets; official publication used the owner's instruction rather
than asserting `-ConfirmRuntimeQualified`. The general qualification gate and
its tests remain unchanged.

The owner additionally authorized this machine's Kingmaker/Steam installation
for disposable runtime verification. Preserve and restore the existing Dice
Roller installation, own the test process, and use new-character creation without
loading or changing valued campaign saves. Mercenary/respec fixtures have not
been supplied. Prior release permissions below are historical.

## Qualification truth

- Implemented: **Yes** — literal-name lookup, independent styling capabilities,
  and in-place recovery bounded to initial resolution plus two FillData retries.
- Source-qualified: **Yes** — 370 C# cases, 30 Python cases, 13 release-gate
  cases and 18 source groups passed.
- Contract-qualified: **Yes** — existing contracts, 11 respec groups and
  22 native UI IL checks passed.
- Build-qualified: **Yes** — Release build, zero warnings/errors.
- Package-qualified: **Yes** — clean merged release commit `db36b3a`, six-file
  allowlist and downloaded public ZIP/checksum parity. DLL equals candidate
  `c06ef75`; ZIP differs only by the packaged official-release README.
  Exact hashes/evidence: `CODEX-NATIVE-THEME-ACTIVATION-STATE.md`.
- Installed: **No** currently — exact candidate was temporarily installed and
  loaded in owned PID 11832, then original mod/settings were hash-verified restored.
- Focused runtime test: **NOT RUN** — startup succeeded, but Windows session
  was disconnected with no foreground window; guarded input/capture refused.
- Native themed path verified: **NOT RUN** — no genuine Skills entry or live Resolve.
- Rendered appearance/interactions inspected: **NOT RUN**.
- Runtime-qualified: **No**.
- Compatibility-qualified: **No** — mercenary/respec fixtures unavailable.
- Human visual acceptance: **NOT RUN**.
- Audio-qualified: **NOT RUN**.
- Release-authorized: **Yes** — explicit owner instruction above; official/latest.
- Testing-prerelease-authorized: **No** — the owner requested an actual release.
- Publicly released: **Yes** — [v0.1.7 official/latest](https://github.com/howardreith/KingmakerDiceRoller/releases/tag/v0.1.7),
  published 2026-09-13 15:07:07 UTC; draft=false, prerelease=false, latest verified.
  No new runtime evidence is implied by publication.

## Verified v0.1.7 publication

[PR #2](https://github.com/howardreith/KingmakerDiceRoller/pull/2) merged to
`main` at `db36b3a55da66751478fa5847fb3cf92ee3eb7f0`. Annotated `v0.1.7` tag
object `3f9dac9d3e76e83ba9280227f03e6d964f4e8aec` peels to that exact clean,
fully pushed build commit. The release was prepared through the existing
publisher, downloaded/verified as a draft, then published official/latest under
the explicit owner instruction. The tag and assets were not retargeted/replaced.
Post-publication documentation may advance main without changing release bytes.

Release ID `387935603`; ZIP asset `561406947` (120289 bytes); checksum asset
`561406946` (97 bytes). DLL SHA-256:
`9bd269d9f54f937323ef48285781e4ad67896c06117a4449f8b099fc04ad852d`.
ZIP SHA-256:
`1b3a5ec3be5085359f87c8da5732f8148573b0792072ba567fc995d45ed07cbc`.
Downloaded draft and published bytes, all six package inputs, GitHub asset
digests and checksum contents match. Native themed-path, rendered, audio,
runtime and compatibility statuses above remain unchanged.

Default-checkout evidence is under ignored `artifacts/release-0.1.7/`:
`draft-preparation.txt`, `draft-verification.json`, `published-verification.json`,
GitHub metadata and both download snapshots. The full build again passed all
370 C#/30 Python/13 gate cases, 18 source groups, existing native contracts,
11 respec groups and 22 native UI checks, with zero warnings/errors. No game
installation, launch or saved-game access was performed during publication.

## Historical 0.1.6 native book UI release

Version `0.1.6`; implementation branch `codex/native-book-ui-reskin`.
Baseline `504b5ae53600be99a01f437b35ed57cdff6c3535`. Presentation-only
paper/button/type/message reskin; mechanic and context services unchanged.
Exact donors, offline evidence and remaining gates: `docs/NATIVE-UI-STYLE.md`
and `CODEX-NATIVE-UI-RESKIN-STATE.md`.

On 2026-09-13, after the local candidate handoff, the owner instructed:
"Could you please commit, merge to the default branch, push to remote, and
cut a release? Thank you." This authorizes publication of 0.1.6 and supersedes
the original mission's local-only publication restriction. The owner then
selected "Official/latest, with the unrun checks disclosed" for v0.1.6. The
existing release was promoted accordingly; this explicit publication decision
does not establish runtime qualification. Live acceptance remains NOT RUN.
Installation, desktop/game testing and save modification remain unauthorized.

## Historical 0.1.6 qualification record

- Implemented: **Yes** — full native style with bounded usable fallback.
- Source-qualified: **Yes** — 18 source groups, 13/13 release-gate tests,
  347/347 C# cases, 30/30 Python cases.
- Contract-qualified: **Yes** — existing Kingmaker contracts, 11 respec groups,
  16 installed/candidate UI IL checks; exact donors inspected in both PC scenes.
- Build-qualified: **Yes** — Release, zero warnings/errors.
- Package-qualified: **Yes** — release rebuilt from clean, pushed main and
  validated against the six-file allowlist. Downloaded GitHub ZIP/checksum match
  local bytes and GitHub asset digests; all entries match source/build inputs.
  Released DLL matches the original candidate exactly. Historical candidate
  provenance remains under ignored `artifacts/candidates/<clean-commit>/`.
- Installed: **No** — no active/test game installation by this mission.
- Focused runtime test: **NOT RUN** — guarded disposable workflow not authorized.
- Runtime-qualified: **No** — interactions, workflow/finalization/save regressions NOT RUN.
- Compatibility-qualified: **No** — mercenary/respec/provider/input-method live matrix NOT RUN.
- Human visual acceptance: **NOT RUN** — all twelve references and actual native
  resources inspected; no real candidate captures or motion evidence.
- Audio-qualified: **NOT RUN** — ordinary route source-qualified only.
- Release-authorized: **Yes** — owner's 2026-09-13 release instruction and
  subsequent explicit official/latest selection quoted above.
- Testing-prerelease-authorized: **No** — superseded by the owner's selection
  of official/latest; the initial prerelease publication is historical below.
- Publicly released: **Yes** — `v0.1.6` official/latest (not a prerelease);
  exact artifact hashes and verified promotion below.


## Verified 0.1.6 publication

[v0.1.6](https://github.com/howardreith/KingmakerDiceRoller/releases/tag/v0.1.6) was initially published on
2026-09-13 at 12:56:46 UTC as a testing prerelease and subsequently promoted to
**official/latest** as recorded below. Release ID `387900346`. Implementation and release
preparation were fast-forward merged from `codex/native-book-ui-reskin` into
`main`, then both branches were pushed. The guarded publisher reran the complete
offline qualification from clean, fully pushed main at release commit
`7662a4239512edd9be7a8d3c3ca511a1d9ce763a`. Annotated tag `v0.1.6` is
`4016542b39111fada3e777962d55d40e4092f3d0` and peels to that commit.

All 347 C# cases, 30 Python cases, 13 release-gate tests, 18 source-validation
groups, existing native contracts, 11 respec groups and 16 UI IL checks passed.
Release build: zero warnings/errors. Visual, audio, interactive workflow,
finalization/save and compatibility acceptance remain **NOT RUN**.

- ZIP: `KingmakerDiceRoller-0.1.6.zip`, 113,947 bytes,
  GitHub asset `561218651`.
- ZIP SHA-256: `6e81dde5773b1d23706f57ed96f1f738a28a9cabc955295d5625a21a8911d1f1`.
- DLL SHA-256: `e2803d3f70ac168d52de27a075b36e6562c7209e709d02c62485f7ded0b13092`.
- Checksum asset: `SHA256SUMS.txt`, 97 bytes, GitHub asset `561218652`;
  SHA-256 `bd83cfa1b9888ddf312d8ba757ea7ab8032246940cd86d092e53907092fa761f`.

The published ZIP and checksum were downloaded and compared byte-for-byte with
the locally qualified files and GitHub SHA-256 digests. Every one of the six ZIP
entries matched its source/build input; Info version is 0.1.6 and the DLL is
identical to the original local candidate. The ZIP differs because its README
now describes the release. Historical candidate files and the v0.1.5 ZIP remain
unchanged. The initial publication audit identified v0.1.5 as latest; the
subsequent promotion audit below now identifies v0.1.6.

Evidence remains under ignored `artifacts/release-0.1.6/`: `publication.txt`,
`github-release.json`, `github-latest.json`, `independent-publication-audit.json`,
copied build/package/contract reports and the downloaded assets. This publication
record is a later documentation commit; the release tag and asset bytes remain
fixed at the release commit above. No PR, installation, game/desktop session,
other-mod change or save modification was performed.


## Verified 0.1.6 official/latest promotion

The owner selected "Official/latest, with the unrun checks disclosed" on
2026-09-13. The existing release was promoted using `gh release edit`, retaining
the original build, ZIP, checksum asset and annotated version tag. This explicit
owner decision supersedes the normal requirement to finish live qualification
before official publication for this release; it does not mark those lanes as
passed or change the automated new-release gate.

At 13:19:54 UTC on 2026-09-13, GitHub's latest-release response and the v0.1.6
release response both identified release `387900346`, with `prerelease=false`
and `draft=false`. The title is `Kingmaker Dice Roller v0.1.6`. Both asset IDs,
names, sizes, SHA-256 digests and upload timestamps matched the records above;
the annotated tag object and peeled release commit also matched exactly.

Release notes prominently disclose that visual, audio, runtime and compatibility
acceptance remain **NOT RUN**, and preserve the original release checksum and
commit. The unchanged ZIP's README still says testing prerelease; the release
page and current repository documentation record the promotion. Runtime-qualified
remains **No**. Promotion involved no rebuild, repackaging, installation, game
session or save modification. Repository/source validation and all 13 release-gate
tests pass for the documentation update; the original artifact's complete offline
qualification remains the evidence for its unchanged DLL/package bytes.

Before/after GitHub responses, tag refs, exact posted notes and
`promotion-audit.json` are preserved under ignored
`artifacts/release-0.1.6/promotion/`. Promotion documentation is newer than the
immutable tagged build; no published asset or tag was replaced.


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
