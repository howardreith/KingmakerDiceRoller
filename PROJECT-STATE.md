# Project state

## Current phase

Phase 2 â€” fixed-array runtime repair after three live new-character context gates.

## Branch and baseline

Branch: `pro/kingmaker-dice-roller-mvp`

Baseline before this repair: `dcd5ae0`.

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
| Source-qualified | Yes â€” 58 C# files, 65 compiled C# behavior cases, 25 Python oracle cases |
| Build-qualified | Yes â€” Windows build against the exact installed Kingmaker assembly |
| Runtime-qualified | No â€” three live candidates rejected the valid new-game preview |
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

The third controller-owned candidate also loaded cleanly and resolved MVID
`07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`, but the new-character ability screen
remained ordinary 25-point buy with zero accepted contexts and zero array
applications. A later first-level candidate passed the supported-mode,
distribution, pet/enemy, and active-controller guards, then failed only because
`Player.MainCharacter` was already populated. This falsified the absence-only
main-character boundary.

Exact 2.1.7b IL establishes why: new-game `StartCharGen` assigns
`Player.MainCharacter` to the source `ChargenUnit.Unit` before character build,
while `LevelUpController` serializes that source, deserializes a separate
preview descriptor, and constructs `LevelUpState` from the preview. The source
therefore reports as the main character while the owned preview descriptor does
not. Custom-companion creation leaves the established campaign main character
unchanged and starts character build for a different controller source.

## Current repair

- Continue requiring first-level state, allowed constructor mode, valid
  distribution, and exclusion of pets and enemies.
- Require the candidate state or unit descriptor to be owned by the active
  `Game.Instance.UI.CharacterBuildController.LevelUpController`.
- Treat `LevelUpController.Unit` and `LevelUpController.Preview` as preview
  ownership identities rather than requiring the unfinished preview descriptor
  to report `IsMainCharacter`.
- Classify `Player.MainCharacter` as absent, the same descriptor as the
  candidate, the controller source for an owned preview, a different descriptor,
  or unresolved.
- Accept absence, direct candidate identity, and the exact new-game
  source/preview relation. Reject a different descriptor so mercenary creation
  remains outside the boundary, and fail closed when normalization is
  unresolved.
- Cache and verify the exact `Player.MainCharacter.Value.Descriptor` and
  `LevelUpController.Unit`/`Preview` contracts.
- Display each unique rejection reason once per attempted session while
  retaining the full rejection count, and report stable Boolean identity facts
  for the decisive controller/main-character relation.

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
  plus the relation between `Player.MainCharacter`, the controller source, and
  the owned preview. The unfinished preview descriptor's `IsMainCharacter`
  value is not used as a finished-character gate.
- Ordinary level-ups, companions, mercenaries, pets, enemies, and respec remain
  outside the feature boundary.
- Does not hard-code a point-buy budget.
- Requires exact runtime contracts before installing any Harmony patch.
- Does not claim compatibility before live qualification.
