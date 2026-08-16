# Project state

## Current phase

Phase 2 — fixed-array runtime repair after the first live new-character gate.

## Branch and baseline

Branch: `pro/kingmaker-dice-roller-mvp`

Baseline before this repair: `bd0e5aade1cd9f52ed1f61b0fbbd8089457011bb`.

## Implemented behavior

- Pure roll engine and presets.
- Immutable six-score arrays and duplicate-safe swaps.
- Explicit low-score policies and extended point-buy equivalent.
- Single-active-session ownership model.
- Fixed diagnostic array `16, 15, 14, 12, 10, 8`.
- Fail-closed context policy for new main-character creation only.
- Captured point-buy budget and explicit restoration path.
- Three narrow Harmony postfix surfaces: state construction, distribution
  start, and completion.
- UMM diagnostics and point-buy restoration control.
- Grace-period cleanup when the live `LevelUpController.State` leaves the owned
  session, covering cancel/back-out and subsequent character creation.
- Exact package allowlist validation and transactional live installation with
  rollback.
- Exact Windows build contracts for Kingmaker 2.1.7b, UMM 0.32.x, and
  Harmony12.

## Qualification

| Level | Status |
|---|---|
| Implemented | Yes — diagnostic source candidate |
| Source-qualified | Yes — 56 C# files, 48 compiled C# behavior cases, 25 Python oracle cases |
| Build-qualified | Yes — clean Windows build against the exact installed Kingmaker assembly |
| Runtime-qualified | No — first live candidate rejected the valid new-character preview context |
| Compatibility-qualified | No |

## First live-gate evidence

The installed candidate loaded successfully, resolved the expected
`Assembly-CSharp` MVID, and detected Bag of Tricks and Call of the Wild. During
new-main-character creation it recorded zero accepted contexts and repeated
`Mode is not CharGen.` rejections.

This established that Kingmaker can construct or rebuild the first-level
main-character preview with `CharBuildMode.LevelUp`. The previous CharGen-only
test was too narrow.

## Current repair

- Accept `CharBuildMode.LevelUp` only as a possible constructor mode.
- Continue requiring `IsFirstLevel`, main-character identity, player faction,
  and exclusion of pets and enemies.
- Continue rejecting `PreGen`, `Respec`, and unknown modes.
- Deduplicate identical rejection messages while preserving the rejection count.

The enum name `LevelUp` does not grant ordinary-level-up support. Ordinary
progression still fails the required `IsFirstLevel` guard.

## Required next live test

Build, package, and install the repaired fixed-array candidate. Confirm one
accepted new-main-character session, exact application of
`16, 15, 14, 12, 10, 8`, navigation stability, cancellation cleanup, and clean
point-buy restoration.

After this seam passes, replace the diagnostic array with the actual user
workflow: Roll/Reroll, Store/Recall, total and point-buy equivalent, duplicate-
safe reassignment, and finally the native Abilities-screen panel.

## Important decisions

- Rewrote rather than ported the upstream static lifecycle.
- Uses position-based assignment so duplicate scores are never ambiguous.
- Ordinary level-ups, companions, pets, enemies, and respec remain outside the
  feature boundary.
- Does not hard-code a point-buy budget.
- Requires exact runtime contracts before installing any Harmony patch.
- Does not claim compatibility before live qualification.
