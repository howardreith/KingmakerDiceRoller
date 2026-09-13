# Native book UI reskin state

## Release follow-up

On 2026-09-13 the owner authorized committing, merging to the default branch,
pushing and cutting a release. The mission branch was fast-forward merged into
main and both branches pushed. [v0.1.6](https://github.com/howardreith/KingmakerDiceRoller/releases/tag/v0.1.6)
is the official/latest release after the owner selected "Official/latest, with
the unrun checks disclosed." Live visual, audio, runtime and compatibility lanes
remain NOT RUN, and Runtime-qualified remains No. No game installation, desktop
control or save modification occurred.

Release source: `7662a4239512edd9be7a8d3c3ca511a1d9ce763a`.
ZIP SHA-256: `6e81dde5773b1d23706f57ed96f1f738a28a9cabc955295d5625a21a8911d1f1`.
DLL SHA-256: `e2803d3f70ac168d52de27a075b36e6562c7209e709d02c62485f7ded0b13092`.
The full qualification passed again at that clean, pushed main commit and the
published assets were downloaded and independently audited. Exact tag/asset IDs,
qualification statuses and publication time are in `PROJECT-STATE.md`; local
reports and downloaded assets are in ignored `artifacts/release-0.1.6/`.

Official/latest promotion was verified on 2026-09-13 at 13:19:54 UTC. GitHub's
latest-release response identifies v0.1.6 with prerelease/draft false. Both asset
IDs, upload timestamps, sizes and SHA-256 digests, plus the annotated tag object
and peeled commit, are unchanged. The released ZIP retains its original README;
the release page and current repository document promotion and the unrun checks.
No rebuild or repackaging occurred. Before/after records and the promotion audit
are preserved in ignored `artifacts/release-0.1.6/promotion/`.

The local mission handoff below is preserved as historical evidence. Its final
clean source is `bc01fde7e5e37471ea95d793e1fe5891bf6e979c`; the exact candidate
and its reports remain under ignored `artifacts/candidates/` at that source ID.
Release preparation changed documentation only. The release ZIP includes the
updated README, so its ZIP hash differs from the historical candidate. The
compiled DLL was verified identical. The publication record is a later
documentation commit and does not retarget the tag or replace published assets.

## Original local mission handoff

Mission branch: `codex/native-book-ui-reskin`.
Actual clean baseline: `504b5ae53600be99a01f437b35ed57cdff6c3535`
(main and locally recorded origin/main). Existing checkout and worktrees were
inspected; no unknown work/reset, remote fetch or other-mod edits.
First slice commit: `ba0d075250f28fd2fa8a997e637537f2f9fb29f9`.
Full implementation commit: `938feb34366df3d708e15ab6ca4ea04a812c2317`,
following that source-qualified paper/title/Close/opener slice. Exact final source head is recorded by the ignored clean-build candidate
manifest; tracked documentation cannot contain its own commit hash.

## Completed implementation

- Full mission, AGENTS and required project/integrity/skill-counter records read;
  all twelve owner references inspected individually. Original stills remain
  outside Git and are not candidate or audio evidence.
- Native paper pixels, borders, PPU, fonts/materials, all button states, input,
  scrollbar and ornament inspected; exact donors present in both serialized PC
  main-menu and in-game scenes. Native activation/submit/audio and unsafe donor
  callbacks inspected read-only. Complete reusable reference:
  [docs/NATIVE-UI-STYLE.md](docs/NATIVE-UI-STYLE.md).
- Full inset paper UI; role fonts; gray SpriteSwap controls; single ordinary
  click route; readable wrapped selectors; two compact Saved action rows;
  measured inline errors and no applied-success footer; fixed title/Close.
- One owned root with existing responsive placement, command router, context
  admission, point-buy restoration, skill-counter Close synchronization and
  forward guards. Bounded theme/audio failures preserve the working session.
- Numeric candidate version 0.1.6, matching assembly metadata. Candidate-only
  packaging refuses dirty/mismatched source, overwrite and Install; no official
  artifact replacement or new mod dependency.

## Offline evidence

Baseline: 336/336 C#, 30/30 Python, 13/13 release-gate tests, source and native
contracts, 11 respec groups, Release zero warnings/errors. Baseline DLL matches
historical 0.1.5. `artifacts/native-ui-reskin/baseline-build.txt`.
Slice build: `artifacts/native-ui-reskin/slice-build.txt`.
Full implementation: 347/347 C#, 30/30 Python, 13/13 release gates, 18 source
validation groups, existing Kingmaker contracts, 11 respec groups and 16 native
UI IL checks; Release zero warnings/errors. Logs:
`artifacts/native-ui-reskin/implementation-build.txt`, `candidate-build.txt`.
Eleven new cases cover Close once, activation/feedback failure, empty success and
long errors, theme fallback, real session/RNG/assignment preservation, all six
requested resolutions and constrained geometry. Existing lifecycle, restoration,
roll/history/saved, skills and forward-guard cases remain in the runner.

Clean implementation qualification passed at `938feb34366df3d708e15ab6ca4ea04a812c2317`:
`artifacts/native-ui-reskin/clean-qualification-938feb3.txt`. Its candidate archive
and six entries were independently checked against source/build inputs; metadata
is 0.1.6 / assembly 0.1.6.0, and copied, packaged and built DLL hashes agree.
The native contract report records 40 signatures plus existing IL assertions;
the separate respec/UI counts above are checked independently.

Qualified candidate bytes:

- ZIP SHA-256: `04f518847f0a9a02d25db8be3a705ef6ec39c5dc85c32aa8bd498a164521ce9a`.
- DLL SHA-256: `e2803d3f70ac168d52de27a075b36e6562c7209e709d02c62485f7ded0b13092`.

After the handoff documentation commit, the final clean-head qualification uses
`Qualify.ps1 -Build -Package -Candidate` again. The exact final source head,
ZIP/DLL paths and independent hashes are authoritative in ignored
`artifacts/build-provenance.json` and `artifacts/candidates/<full-clean-commit>/`:
`package-manifest.json`, `independent-package-audit.json`, `HANDOFF.md` and
`qualification.txt`. No generated provenance or machine paths enter Git.
Next safe action is owner review of that candidate and, only if explicitly
authorized, the guarded disposable-fixture smoke procedure below.

## Separate qualification status and blockers

| Lane | Status |
| --- | --- |
| Offline tests / source / native contracts / Release build | PASS, scoped to recorded checks |
| Candidate packaging | PASS, six-file allowlist and independent byte/hash audit |
| Live interactions and ownership behavior | NOT RUN |
| Native aesthetic / press-motion acceptance | NOT RUN |
| Audible click, Submit, master volume/mute | NOT RUN |
| In-game workflow / finalization / save regressions | NOT RUN |
| Mercenary / supported-respec / provider / input compatibility | NOT RUN |

No already authorized guarded runtime workflow or disposable fixture is present.
Do not launch Steam/game, control a desktop or install this DLL without that
separate authorization. Availability of a desktop/process is not authorization.
Unresolved individually: fresh-launch initialization; actual paper/typography
fit at real canvas scales; hover/held/disabled/canceled press; pointer/Submit
focus and single audible cue; Wwise mix/mute behavior; long-content thumb/input
layout; root cleanup/donor integrity; skill badge/Next/final-character behavior
and existing supported entry points with the exact candidate.

Owner procedure and all twelve scenarios/six resolutions are in
[docs/SMOKE-TEST.md](docs/SMOKE-TEST.md#native-book-ui-candidate-matrix-all-live-lanes-not-run).
Use only an explicitly authorized isolated disposable fixture, pin its DLL hash,
compare with native Back and capture real before/after frames plus audio/motion.
No fallback or green build qualifies the aesthetic goal.

At the original local handoff, no push, merge, PR, tag, release, active install,
game launch, desktop control, other-mod modification, Cloud/profile change or
valued-save modification had occurred. The release follow-up above records the
subsequent publication authorization separately.
