# Changelog

## 0.1.0-alpha.1 - alpha candidate

- Recorded the successful focused live gate at `8f78d824...`: pristine point
  buy and its native ability-page presentation updated immediately without
  navigation.
- Replaced automatic diagnostic-array session startup with an explicit
  PointBuy-first lifecycle. Opening, rebinding, or rendering a valid session
  no longer generates or stages a roll.
- Added explicit PointBuy, EnteringRollMode, Roll, and RestoringPointBuy
  transitions plus transactional Roll/Reroll/reassignment/recall command
  boundaries.
- Replaced the first-generation-only baseline with a point-buy origin captured
  at each explicit PointBuy-to-Roll transition, preserving the user's current
  legitimate allocation and observed allocator budget.
- Added product roll configuration, position-safe assignment serialization,
  bounded 20-entry history, bounded 10-slot saved catalog, schema-v1 migration,
  UI snapshots, and exact native allocator-control contracts.
- Added a code-owned native ability-page panel with Roll/Reroll, preset and
  policy controls, custom expression input, duplicate-safe assignment,
  summaries, history, saved arrays, and exact Point Buy recovery.
- Added exact one-panel attach/rebind/detach lifecycle through the native
  allocator `FillData()` seam and restored every captured plus/minus control
  state on cleanup.
- Added transactional command rollback: failed Roll keeps Point Buy; failed
  Reroll/reassignment/recall preserves the prior verified roll.
- Persisted product defaults and schema-versioned saved arrays in UMM settings,
  with per-record corrupt-data isolation and warnings.
- Bumped product, package, assembly file, and informational metadata consistently
  to `0.1.0-alpha.1` and added a complete player guide and product smoke test.
- Preserved the explicit 1-120 generated-score boundary with fail-closed,
  no-clamping validation and extended point-buy-equivalent reporting.

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
- Recorded the sixth live gate: pristine restoration removed the illegal
  rolled-plus-point-buy hybrid, ordinary base tens and 25 points appeared after
  ability-page re-entry, the tested tiefling's racial modifiers remained
  separate, durable PointBuy mode survived navigation, and an ordinary
  existing-character level-up remained isolated.
- Recorded the limited Bag of Tricks restoration smoke without claiming its
  alternative budgets/settings or full compatibility matrix qualified.
- Identified the remaining defect as stale presentation on the currently open
  ability page: the verified live model was correct, but score rows, racial
  modifiers, allocator points, and controls did not update until navigation.
- Added exact 2.1.7b contracts for the active Skills phase and
  `CharBAbilityScoresAllocator.FillData()`, including its current source and
  preview binding fields.
- Added one bounded, PointBuy-only post-restoration native presentation refresh
  and verified that the allocator binds the current session state,
  distribution, source entity, and preview entity without rebuilding the
  preview or re-entering Roll mode.
- Separated semantic restoration success from presentation synchronization
  success. A native presentation failure now retains safe durable PointBuy mode
  and reports exact binding facts instead of rolling back to fixed values.
- Added executable regressions for stale presentation detection, exact native
  refresh ordering and bounds, immediate human/racial/non-default-budget
  presentation, safe failure and disable behavior, and absence of refresh
  recursion or fixed-array restaging.
- Source-qualified 66 C# files, 123 compiled C# behavior cases, and 25 Python
  oracle cases; the exact presentation repair build/package completed with zero
  warnings and zero errors against Assembly-CSharp MVID
  `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`.

No full runtime or compatibility qualification is claimed until immediate
same-page point-buy presentation and the remaining vanilla gates pass live
testing.
