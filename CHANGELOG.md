# Changelog

## 0.0.1-alpha.1 — source candidate

- Initialized a standalone Kingmaker 2.1.7b UMM project.
- Added a pure deterministic dice-expression and roll-preset domain.
- Added immutable six-score arrays and duplicate-safe assignment swaps.
- Added explicit tabletop and Kingmaker-safe low-score policies.
- Added extended point-buy-equivalent reporting.
- Added a guarded fixed-array character-creation integration candidate.
- Added source, contract, build, package, install, and smoke-test tooling.
- Added grace-period release of stale character-creation sessions through the
  exact `LevelUpController.State` contract and UMM update callback.
- Added exact duplicate-safe package validation and transactional install
  rollback.
- Qualified the exact Windows Kingmaker 2.1.7b and UMM/Harmony12 build
  contracts.
- Corrected new-character context handling after live evidence showed that
  Kingmaker preview reconstruction can use `CharBuildMode.LevelUp` during
  first-level main-character creation.
- Kept `PreGen`, `Respec`, ordinary non-first-level progression, companions,
  pets, and enemies excluded.
- Replaced preview-time `IsMainCharacter` value gating with active
  `LevelUpController` ownership because live new-game preview descriptors are
  not yet flagged as the finished main character.
- Added a separate established-`Player.MainCharacter` boundary so mercenary,
  respec, and ordinary campaign character builds remain excluded.
- Deduplicated context-rejection diagnostics by unique reason while retaining
  the total rejection count.
- Recorded the third live failure: an otherwise valid controller-owned
  first-level candidate was rejected because `Player.MainCharacter` was already
  populated during new-game character creation.
- Verified from exact Kingmaker 2.1.7b IL that new-game setup stores the source
  unit in `Player.MainCharacter` before `LevelUpController` deserializes its
  separate preview descriptor.
- Replaced the absence-only boundary with an explicit, fail-closed identity
  relation that accepts an absent main value, a direct candidate match, or the
  controller source for the owned preview, while rejecting a different
  established campaign descriptor.
- Added stable controller/main-character relation diagnostics and executable
  fake-contract regression coverage for acceptance, exclusions, session
  opening/rebinding, and fixed-array reuse.
- Required the exact `Player.MainCharacter.Value.Descriptor` and controller
  `Unit`/`Preview` contracts during offline and runtime resolution.
- Corrected runtime evidence collection to include Kingmaker's live LocalLow
  `output_log.txt`.
- Source-qualified 58 C# files, 65 C# behavior cases, and 25 executed Python
  oracle cases.

No runtime or compatibility qualification is claimed until the repaired
new-character candidate passes the live fixed-array gate.
