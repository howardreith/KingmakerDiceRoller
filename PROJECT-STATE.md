# Project state

## Current phase

Phase 2 - fixed-array preview-continuity repair after the fourth live
new-character gate.

## Branch and baseline

Branch: `pro/kingmaker-dice-roller-mvp`

Baseline before this repair: `4c1bf277580b32f265be36cb4c4ae7c3e6a51c9f`.

## Implemented behavior

- Pure roll engine and presets.
- Immutable six-score arrays and duplicate-safe swaps.
- Explicit low-score policies and extended point-buy equivalent.
- Single-active-session ownership keyed by the stable character-build
  controller and its source `UnitDescriptor`.
- Replaceable preview generations for the current `LevelUpState`, preview
  descriptor, `StatsDistribution`, and captured point-buy baseline.
- Fixed diagnostic array `16, 15, 14, 12, 10, 8`.
- Captured point-buy budget and explicit restoration path for the newest live
  preview.
- Three narrow Harmony postfix surfaces: state construction, distribution
  start, and completion.
- Bounded constructor-stage application followed by validation against the
  controller's actual live state, preview, distribution, and unit base values.
- Grace-period cleanup only when the stable controller/source owner leaves the
  character build; transient state/preview replacement does not release it.
- UMM diagnostics and point-buy restoration control.
- Exact package allowlist validation and transactional live installation with
  rollback.
- Exact Windows build contracts for Kingmaker 2.1.7b, UMM 0.32.x, and
  Harmony12.

## Qualification

| Level | Status |
|---|---|
| Implemented | Yes - stable-owner/generation-aware diagnostic candidate |
| Source-qualified | Yes - 61 C# files, 86 compiled C# behavior cases, 25 Python oracle cases |
| Build-qualified | Yes - zero-warning exact build/package against Assembly-CSharp MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7` |
| Runtime-qualified | No - the fourth candidate accepted the context but lost the live replacement preview |
| Compatibility-qualified | No |

## Live-gate evidence

The first installed candidate rejected every constructor because it required
`CharBuildMode.CharGen`; Kingmaker uses `CharBuildMode.LevelUp` for legitimate
new-game preview reconstruction.

The second candidate accepted that mode but still recorded zero accepted
contexts. Its unique rejection reasons established that the first-level preview
descriptor is not yet marked `IsMainCharacter`; using that finished-unit value
during preview construction was too early.

The third controller-owned candidate loaded cleanly and resolved MVID
`07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`, but the new-character screen remained
ordinary 25-point buy. A first-level candidate passed every earlier guard and
failed only because `Player.MainCharacter` was populated. Exact 2.1.7b IL then
showed that new-game setup stores the controller source in
`Player.MainCharacter` before a separate preview descriptor is deserialized.
The fail-closed relation was repaired to recognize that exact source/preview
identity while retaining the different-established-main boundary.

The fourth candidate proved that context repair: it accepted one genuine
controller-owned `CharGen` preview and staged the fixed array. The live ability
screen nevertheless remained ordinary 25-point buy with all six scores at 10.
Chronological diagnostics then showed a second same-build preview rejected as
`Another unit already owns the active roll session`, an `APPLY` reported
against the first descriptor, and a stale-session `RELEASE` while character
creation was still open.

Exact 2.1.7b IL and the complete runtime log explain that order.
`LevelUpController.UpdatePreview()` assigns a newly deserialized descriptor to
`Preview`, constructs its replacement state synchronously, and assigns that
state only after its constructor returns. The constructor postfix therefore
observes the new preview inside the outer refresh while `controller.State`
still names the old generation. The previous implementation rejected the new
descriptor by preview identity, validated detached objects, and later released
the session by exact-state identity.

## Current repair

- Preserve every context-policy guard from the successful fourth candidate.
- Keep the active `LevelUpController` instance and its source `Unit` descriptor
  as immutable session ownership; treat preview descriptors as transient.
- Rebind same-owner replacement states, preview units, distributions, and
  baselines while retaining the exact same immutable assignment.
- Stage values in the constructor postfix without requesting another preview
  refresh. After Kingmaker's constructor stack and action replay finish, verify
  the controller's actual state/preview and both live value stores on UMM
  update. One live restage is permitted if action replay overwrites
  constructor-stage values.
- Guard explicit preview refreshes against nesting. Point-buy restoration uses
  one guarded refresh, accepts its same-owner replacement while restoration is
  active, and restores the newest generation with its captured budget and
  baseline.
- Base liveness on the stable controller/source owner. A replaced or temporarily
  null `State` is not evidence that character creation ended.
- Report generation, pending replacement, stable-owner, controller-state,
  controller-preview, distribution-value, and unit-value relations without raw
  object dumps or repeated event spam.

## Required next live test

Build, package, and install the stable-owner fixed-array candidate. From a fresh
process with only Dice Roller enabled, confirm the live ability screen shows
`16, 15, 14, 12, 10, 8`, the live controller state/preview is verified, and no
same-owner replacement reports `Another unit`. Remain on the screen for ten
seconds, navigate backward/forward once, confirm the same assignment is rebound,
then cancel and confirm release occurs only after the build ends.

Do not advance to the full vanilla Gates A-E or the real Roll/Reroll UI until
this focused entry/rebuild gate passes.

## Important decisions

- New-character identity is determined by active character-builder ownership
  plus the relation between `Player.MainCharacter`, the controller source, and
  the owned preview. The unfinished preview descriptor's `IsMainCharacter`
  value is not used as a finished-character gate.
- Ordinary level-ups, companions, mercenaries, pets, enemies, pregens, and
  respec remain outside the feature boundary.
- Session continuity uses the stable controller/source relation; reference
  identity of a deserialized preview is generation-local.
- Does not hard-code a point-buy budget.
- Requires exact runtime contracts before installing any Harmony patch.
- Does not claim runtime or compatibility qualification before live evidence.
