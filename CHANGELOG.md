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
- Recorded the fourth live gate accurately: context acceptance succeeded, but
  a reentrant same-build preview was rejected, detached values were reported as
  applied, and exact-state liveness released the session while character
  creation remained open.
- Verified from exact 2.1.7b IL that `UpdatePreview()` installs the new
  `Preview`, constructs its replacement state synchronously, and assigns
  `State` only after the patched constructor returns.
- Replaced transient preview-descriptor ownership with immutable
  controller/source ownership and generation-aware rebinding of state, preview,
  distribution, and generation-local rollback state.
- Removed preview refresh from fixed-array staging, added a bounded one-restage
  protocol after Kingmaker action replay, and require verification against the
  controller's actual live state, preview, distribution, and base values before
  counting an application.
- Changed stale-session detection to follow the stable controller/source owner,
  so replacement or temporarily null states cannot release a live character
  build.
- Made point-buy restoration refresh once, follow any same-owner replacement,
  and restore the newest live generation rather than a detached preview.
- Added concise generation/rebind/live-validation diagnostics and executable
  preview-continuity regression coverage.
- Source-qualified 61 C# files, 86 compiled C# behavior cases, and 25 executed
  Python oracle cases; the exact production build completed with zero warnings
  and zero errors.
- Recorded the successful fifth live gate: the exact fixed array was visible,
  live-preview verification and ten-second stability passed, backward/forward
  rebuilding retained one immutable assignment, and elf racial modifiers
  remained separate from the base array.
- Recorded successful cancellation/release and fresh-session creation, plus a
  save/reload without Dice Roller that retained ordinary Kingmaker ability
  values.
- Recorded only a limited new-character entry smoke with Call of the Wild and
  Bag of Tricks; this is not compatibility qualification.
- Recorded the hard restoration failure in which the fixed values remained
  while the complete point-buy budget returned, permitting an illegal
  rolled-plus-point-buy hybrid.
- Split the immutable first-generation `PristinePointBuyState` from the mutable
  per-generation `GenerationRollbackSnapshot`, so a rolled replacement preview
  cannot overwrite the user's legitimate point-buy origin.
- Added explicit Roll, RestoringPointBuy, and durable PointBuy modes. Same-owner
  rebuilds no longer reapply the fixed array after restoration, and the
  completion postfix does not override point-buy mode.
- Made roll mode disable the allocator explicitly and made restoration call the
  observed allocator budget, restore the pristine allocation on the newest live
  preview, and verify values plus allocator fields before succeeding.
- Added explicit hybrid-state rejection, generation-local rollback, and
  fail-closed disable/unload behavior when pristine restoration or rollback
  cannot be proven.
- Added exact allocator-field contracts and executable regressions for
  non-default budgets/allocations, racial-modifier separation, live-generation
  restoration, durable point-buy navigation, and recovery failure paths.
- Source-qualified 64 C# files, 101 compiled C# behavior cases, and 25 Python
  oracle cases; the exact repair build/package completed with zero warnings and
  zero errors against Assembly-CSharp MVID
  `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`.

No full runtime or compatibility qualification is claimed until pristine
point-buy restoration and the remaining vanilla gates pass live testing.
