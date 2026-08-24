# Project state

## Current repair candidate

The active work is the unreleased mercenary-finalization and collapsed-tab
placement repair on `codex/mercenary-roll-persistence-repair`.
The stable version metadata remains `0.1.1`.

```text
Starting origin/main commit: cff17351d3282d8a509c928355ee92754759351a
Product/UMM ID:             KingmakerDiceRoller
Version metadata:           0.1.1 (unchanged; no release bump authorized)
Target:                     Pathfinder: Kingmaker 2.1.7b
```

No public version, tag, release, PR, or `main` change is authorized by this
candidate. Historical tags, artifacts, and evidence remain immutable.

## Confirmed defects and evidence boundary

The reported main-campaign mercenary failure had two parts:

- at approximately 1152 by 720, the collapsed **Roll Stats** control used the
  upper-right fallback instead of safe bottom-center ability geometry;
- rolled values matched the creation preview but the hired unit retained its
  original `10/12/8/12/10/10` base allocation.

No safe interactive pre-fix Kingmaker session was available during this mission,
so those observations remain reporter-provided rather than independently
reproduced. The configured installation was available for exact 2.1.7b
reflection/IL inspection. No runtime values, screenshot, hired party unit, or
save/reload result has been invented.

## Root cause and authoritative seam

Exact 2.1.7b IL proves this lifecycle:

```text
CreateCustomCompanion.RunAction
  -> Player.CreateCustomCompanion(success callback, xp, importable)
  -> HandleLevelUpStart(newCompanion.Descriptor, null, callback, CharGen)
  -> LevelUpController.Start(...)

LevelUpController.UpdatePreview
  -> ApplyLevelup(Preview)
       -> fresh LevelUpState and StatsDistribution
       -> replay native ILevelUpAction instances on Preview

LevelUpController.Commit
  -> dispose Preview.Unit
  -> ApplyLevelup(LevelUpController.Unit)
       -> fresh LevelUpState and StatsDistribution
       -> replay native ILevelUpAction instances on the stable source
  -> SetupNewCharacher
       -> native player companion ownership
  -> success callback
```

The old implementation wrote and verified `StatsDistribution` plus the
transient Preview descriptor. It did not add a native action representing the
rolled assignment. On commit, Kingmaker discarded Preview and replayed actions
against `LevelUpController.Unit`; the stable mercenary's original allocation
therefore superseded the visual roll.

The repair adds a postfix after `ApplyLevelup(Unit)` native replay and before
first-level setup/callback. It applies the six verified base values only when
the target is the exact active controller/source owner of an immutable
`Mercenary` session and the fresh state independently passes first-level,
`CharGen`, `IsEmployee`, and `IsCustomCompanion` checks. Preview calls, different
controllers/sources, cancellation, owner loss, main-character creation, level
up, respec, and other contexts are ignored.

A `Commit()` postfix reads the same stable descriptor after the success callback
and produces one final PASS or FAIL record with controller/source/preview/final
identities and expected/observed arrays. It is idempotent and clears transient
session state even on mismatch. Race modifiers remain native modifiers; only
base values are written. No save-owned Dice Roller content is created.

New-main-character creation uses the same native controller replay mechanics,
but the repair's final write is restricted to `Mercenary`; the established main
path is unchanged. Bag of Tricks is not part of the finalization seam. Its known
Dice Roller interaction is the live allocator budget used for Point Buy
round-trip, which still requires focused runtime confirmation.

## Collapsed access-tab repair

The upper-right defect came from treating active
`m_RaceBonusContainer` as the only usable anchor and otherwise assigning fixed
top/right anchors and offsets. The repair computes local Canvas geometry and
always places the 140 by 34 tab above the 92-unit bottom-navigation inset plus an
8-unit gap. Horizontal geometry is selected from:

1. active usable racial-bonus container;
2. allocator frame;
3. allocator region;
4. ability-phase root.

The result is centered within the selected ability region and clamped to safe
bounds. There is no upper-right fallback and the owned root remains non-graphic,
so collapsed mode has only the tab's raycast footprint.

## Implemented and automated evidence

- Mercenary authoritative application and post-callback verification services.
- Exact cached `LevelUpState.Mode`, private `ApplyLevelup(Unit)`, and `Commit()`
  contracts plus native token-order validation.
- Six narrow Harmony postfixes; patch methods still delegate immediately.
- Bounded final PASS/FAIL diagnostics with expected and observed six-value base
  arrays and object identities.
- Pure bottom-center access-tab geometry shared by main and mercenary UI hosts.
- 283 deterministic C# behavior cases and 30 Python oracle cases, including
  preview-only false success, final mismatch, kind/owner isolation, same-owner
  preview replacement without another roll, duplicate callbacks, cancellation,
  ownership loss, post-callback verification, modifier separation, absent or
  inactive preferred UI geometry, constrained bounds, bottom navigation, no
  upper-right fallback, and all five required resolutions.
- Standalone exact-contract verification of launch callbacks, preview replay,
  authoritative replay, native companion insertion, and success callback order.

## Supported boundary

Only these exact first-level custom creation kinds may expose Roll Stats:

1. a new campaign main character;
2. player-initiated custom mercenary recruitment.

Ordinary level-up, respec, existing companions, pets, animal companions,
enemies, pregenerated characters, unresolved ownership, unknown modes, and a
different unmarked player descriptor remain fail-closed.

## Qualification truth

For this unreleased repair candidate:

- Implemented: **Yes**.
- Source-qualified: **Yes** — repository validation, 283/283 compiled C# cases,
  and 30/30 Python oracle cases pass on the repair source.
- Contract-qualified: **Yes** — exact Kingmaker 2.1.7b verification passes
  against Assembly-CSharp MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7` and
  SHA-256 `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`.
- Build-qualified: **Yes** — the repository-owned clean-commit qualification at
  `041c10c88d12b235a9d150d07026685541560273` passed with zero warnings/errors;
  the Release DLL SHA-256 is
  `173ee7f2c1c0cee1047bf1a0c75b5fd9ead4f42acc82c6c4d53854bebe72a2d2`.
- Package-qualified: **Yes** — the deterministic six-file package validated at
  that commit with SHA-256
  `76f87f8e3a03fe16fdb5f67b8c3102edd66e032e422e88979cc4c0e93728e26f`.
- Installed: **Yes** — the transactional `-WhatIf` gate and real repository-owned
  install both passed. The installed DLL SHA-256 is
  `173ee7f2c1c0cee1047bf1a0c75b5fd9ead4f42acc82c6c4d53854bebe72a2d2`,
  byte-identical to the qualified build.
- Runtime-qualified: **No** — no post-repair live hired-unit or save/reload
  evidence exists.
- Compatibility-qualified: **No** — the focused Bag of Tricks completion and
  save/reload case has not run.
- Human visual acceptance: **No** — the required corrected-placement screenshot
  does not exist.
- Release-authorized: **No**.
- Publicly released: **No new repair artifact**.

Historical qualification is not transferable to this candidate and, after the
reported defect, cannot qualify mercenary persistence or constrained-resolution
tab placement for older bytes.

The repository-owned collector wrote ignored evidence under
`artifacts/runtime-evidence/20260823-231321`. Its only copied game log predates
this installation and contains no repair finalization or access-anchor record;
it is retained as an explicit negative evidence boundary, not a post-fix runtime
test.

## Remaining runtime gate

Run the exact installed final artifact through `docs/SMOKE-TEST.md`. At minimum,
complete the vanilla mercenary matrix at 1152x720, 1280x720, 1366x768,
1600x900, and 1920x1080; regress new-main-character creation; verify unsupported
contexts; inspect the actual hired party unit; then save, exit, restart, reload,
and inspect it again. Repeat the focused mercenary completion/origin test with
Bag of Tricks and record its live budget. Preserve the required screenshot,
logs, hashes, and value tables outside Git.
