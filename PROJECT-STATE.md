# Project state

## Current phase

Phase 2 â€” fixed-array runtime repair after two live new-character context gates.

## Branch and baseline

Branch: `pro/kingmaker-dice-roller-mvp`

Baseline before this repair: `7136bc4`.

## Implemented behavior

- Pure roll engine and presets.
- Immutable six-score arrays and duplicate-safe swaps.
- Explicit low-score policies and extended point-buy equivalent.
- Single-active-session ownership model.
- Fixed diagnostic array `16, 15, 14, 12, 10, 8`.
- Captured point-buy budget and explicit restoration path.
- Three narrow Harmony postfix surfaces: state construction, distribution
  start, and completion.
- UMM diagnostics and point-buy restoration control.
- Grace-period cleanup when the live `LevelUpController.State` leaves the owned
  session.
- Exact package allowlist validation and transactional live installation with
  rollback.
- Exact Windows build contracts for Kingmaker 2.1.7b, UMM 0.32.x, and
  Harmony12.

## Qualification

| Level | Status |
|---|---|
| Implemented | Yes â€” diagnostic source candidate |
| Source-qualified | Yes â€” 56 C# files, 48 compiled C# behavior cases, 25 Python oracle cases |
| Build-qualified | Yes â€” Windows build against the exact installed Kingmaker assembly |
| Runtime-qualified | No â€” two live candidates rejected the valid new-game preview |
| Compatibility-qualified | No |

## Live-gate evidence

The first installed candidate rejected every constructor because it required
`CharBuildMode.CharGen`; Kingmaker uses `CharBuildMode.LevelUp` for legitimate
new-game preview reconstruction.

The second candidate accepted that mode but still recorded zero accepted
contexts. Its unique rejection reasons established that:

- some constructor states are not first-level states and must remain excluded;
- the first-level preview descriptor is not yet marked `IsMainCharacter`;
- using the finished-unit `IsMainCharacter` value during preview construction
  is therefore too early and rejects the intended new-game character.

Both runs loaded cleanly, resolved the exact expected Assembly-CSharp MVID, and
left ordinary point buy unchanged.

## Current repair

- Continue requiring first-level state, allowed constructor mode, valid
  distribution, and exclusion of pets and enemies.
- Require the candidate state or unit descriptor to be owned by the active
  `Game.Instance.UI.CharacterBuildController.LevelUpController`.
- Treat `LevelUpController.Unit` and `LevelUpController.Preview` as preview
  ownership identities rather than requiring the unfinished preview descriptor
  to report `IsMainCharacter`.
- Require that `Game.Instance.Player.MainCharacter` has no established live
  descriptor. This keeps mercenary creation, respec, and ordinary campaign
  level-up contexts outside the feature boundary.
- Fail closed if either controller ownership or the established-main-character
  boundary cannot be resolved.
- Display each unique rejection reason once per attempted session while
  retaining the full rejection count.

## Required next live test

Build, package, and install this controller-owned fixed-array candidate. Confirm:

1. at least one accepted new-game first-level preview context;
2. exact application of `16, 15, 14, 12, 10, 8`;
3. no activation during an existing-character level-up;
4. navigation stability and cancellation cleanup;
5. clean point-buy restoration.

After this seam passes, replace the diagnostic array with the actual user
workflow: Roll/Reroll, Store/Recall, total and point-buy equivalent,
duplicate-safe reassignment, and the native Abilities-screen panel.

## Important decisions

- New-character identity is determined by active character-builder ownership
  and absence of an established campaign main character, not by the unfinished
  preview descriptor's `IsMainCharacter` value.
- Ordinary level-ups, companions, mercenaries, pets, enemies, and respec remain
  outside the feature boundary.
- Does not hard-code a point-buy budget.
- Requires exact runtime contracts before installing any Harmony patch.
- Does not claim compatibility before live qualification.
