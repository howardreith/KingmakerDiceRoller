# Project state

## Current phase

Player-facing `0.1.0-alpha.1` implementation. The fixed-array vertical slice
has passed its focused live integration gate; explicit PointBuy-first product
workflow implementation is in progress.

## Branch and baseline

Branch: `pro/kingmaker-dice-roller-mvp`

Authoritative overnight baseline: `8f78d8243ed27ed2cbb3fadafd890aba172975aa`.

## Latest focused live gate

At commit `8f78d824...`, a fresh live test proved that **Return active roll
session to point buy** immediately refreshed the already-open native ability
page. Ordinary base tens, the real 25-point allocator, and native controls
appeared without navigation. Together with the earlier preview-continuity and
save-independence evidence, this closes the diagnostic fixed-array vertical
slice. It does not qualify the unfinished player-facing workflow.

## Implemented behavior

- Pure roll engine, presets, immutable six-score arrays, duplicate-safe swaps,
  explicit low-score policies, and extended point-buy equivalents.
- Fixed diagnostic array `16, 15, 14, 12, 10, 8` for one exact new-main-
  character build owner.
- Immutable session ownership keyed by the active `LevelUpController` and its
  source `UnitDescriptor`, with replaceable state, preview descriptor,
  distribution, and generation rollback snapshot.
- One immutable pristine point-buy origin captured before the first rolled
  write. Same-owner preview rebuilds cannot replace it with rolled values.
- Explicit `Roll`, `RestoringPointBuy`, and durable `PointBuy` modes. Roll mode
  disables the allocator; point-buy mode suppresses later fixed-array staging
  and completion overrides for the same build owner.
- Bounded preview refresh, same-owner rebind, and validation against the
  controller's actual live state, preview, distribution, base values, and
  allocator fields.
- Point-buy restoration on the newest live preview using the observed
  allocator `Start(int)` budget and the pristine allocation. An illegal rolled
  array plus full budget cannot pass verification.
- One bounded native `CharBAbilityScoresAllocator.FillData()` call after
  semantic restoration. It rebinds the currently active Skills ability page to
  the live controller state/distribution and refreshes score rows, racial
  modifiers, allocator points, and native controls without rebuilding the
  preview.
- Separate semantic-restoration and presentation observations. Presentation
  failure preserves verified, durable PointBuy mode instead of rolling back to
  the diagnostic array.
- Generation-local rollback after a failed write or restore. Disable/unload
  restores verified point buy before unpatching, or refuses to disable while
  retaining recovery hooks.
- Stable-owner liveness with eventual cleanup after actual cancellation or
  completion.
- Three narrow Harmony postfixes only: state construction, distribution start,
  and distribution completion.
- Exact six-file package validation and transactional live installation.

## Qualification

| Level | Status |
|---|---|
| Implemented | Yes - bounded native ability-page presentation synchronization implemented |
| Source-qualified | Yes - 66 C# files, 123 compiled behavior cases, and 25 Python oracle cases |
| Build-qualified | Yes - zero-warning exact build/package against MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7` |
| Runtime-qualified | No - semantic restoration passed, but immediate same-page presentation awaits live evidence |
| Compatibility-qualified | No - only limited entry/restoration smokes have been observed |

## Live-gate evidence

Earlier candidates established that Kingmaker may use preview-time `LevelUp`
mode, that the unfinished descriptor is not yet marked `IsMainCharacter`, and
that `Player.MainCharacter` may identify the controller source rather than the
deserialized preview. The fourth candidate accepted the genuine context but
lost its array across preview replacement. Commit `907f1bc1...` repaired that
stable-owner/generation boundary.

The fifth live gate at `907f1bc1...` passed the fixed-array entry and continuity
seam:

- A new custom human visibly received base values `16, 15, 14, 12, 10, 8`.
- Diagnostics verified the controller's live state, preview, distribution, and
  unit values.
- The session remained active for more than ten seconds.
- Backward/forward navigation rebuilt the preview without changing or
  regenerating the immutable array.
- Switching to elf displayed `16, 17, 12, 14, 10, 8`, proving Kingmaker applied
  racial modifiers separately from the stored base array.
- Canceling released the session, and a later character build opened a fresh
  session without an ownership conflict.
- A roll-mode character was saved, the game exited, Dice Roller was disabled,
  and the save reloaded with its scores intact. This is live evidence that the
  result uses ordinary Kingmaker ability values rather than mod-owned save
  content.
- New-character entry also worked with Call of the Wild and Bag of Tricks
  installed. This was only a limited entry smoke, not compatibility
  qualification.

The same gate exposed a hard restoration failure. **Return active roll session
to point buy** restored the full 25-point pool while leaving the rolled base
array in place. The user could spend the pool on top of the roll, creating an
illegal hybrid character. Commit `c5ea92b2...` repaired that semantic model
failure.

The sixth live gate at `c5ea92b2...`, performed with Bag of Tricks enabled,
proved the pristine model and durable-mode repair but exposed a presentation
defect:

- A human began with the visible fixed array and a disabled zero-point
  allocator. Restoration left that old presentation visible on the open page,
  but leaving and re-entering immediately showed ordinary base tens, 25 points,
  and working native plus/minus controls.
- The tested tiefling heritage likewise retained the old rolled presentation
  until navigation. Re-entry showed ordinary point-buy base values with its
  `+2 STR`, `+2 WIS`, and `-2 CHA` modifiers still separate.
- Backward/forward navigation did not reapply the fixed array, confirming that
  durable PointBuy mode remained active.
- An ordinary existing-character level-up did not activate the array.
- Bag of Tricks did not prevent semantic restoration in this limited smoke;
  alternative budgets/settings and the full compatibility matrix remain
  unqualified.

The live controller model was therefore safe, while the currently open native
ability page remained bound to stale presentation data until its normal phase
re-entry refresh. Runtime qualification remains blocked on immediate
same-page synchronization.

## Root cause and current repair

Exact Kingmaker 2.1.7b IL shows that `StatsDistribution.Start(int)` sets only
allocator availability and point fields; it does not reset the six score
values. The old session captured a new point-buy baseline on every accepted
preview generation and replaced `RollSession.Baseline` during rebind. A rebuilt
generation already carrying the fixed array could therefore become the
purported baseline. Restoration then called `Start(fullBudget)` and restored
those contaminated rolled values.

The repair separates two lifetimes:

- `PristinePointBuyState` is captured only for generation 1, before roll
  ownership writes. It records the real allocator budget and provenance,
  legitimate distribution/unit values, and allocator fields.
- `GenerationRollbackSnapshot` is replaced on each preview generation and is
  used only for transactional rollback of that generation.

Restoration enters `RestoringPointBuy`, performs one guarded preview refresh,
follows a same-owner replacement, calls the allocator with the pristine
observed budget, restores the pristine values and allocator fields on the
newest live preview, and verifies all live relations. Success enters durable
`PointBuy` mode for the stable owner. Failure restores the isolated fixed array
with point buy disabled; if that rollback cannot be verified, disable/unload is
refused.

Exact Kingmaker 2.1.7b IL shows that the active Skills phase owns a
`CharBAbilityScoresAllocator`. Its `FillData()` method rereads the current
controller state, source and preview units, `StatsDistribution`, racial
modifiers, remaining points, costs, and control availability. The current
repair calls that exact method once after semantic restoration and verifies its
private source/preview bindings against the session's live generation. It does
not call `UpdatePreview()`, mutate labels directly, or re-enter Roll mode.
Presentation failure leaves the already-verified PointBuy model safe and
reports the failed binding instead of restoring rolled values.

## Required next live test

From a fresh vanilla process, verify that the fixed array appears, then use
**Return active roll session to point buy without leaving the ability page**.
The same page must immediately show ordinary values and the real budget, and
plus/minus controls must work. Repeat with the tested tiefling heritage, then
test disabling while roll mode is active. Do not advance to the final
Roll/Reroll UI or compatibility qualification until this immediate
presentation gate passes.

## Important decisions

- The successful context identity and stable preview-continuity repairs are
  preserved unchanged.
- Ordinary progression, companions, mercenaries, pets, enemies, pregens, and
  respec remain outside the feature boundary.
- Roll mode and point-buy mode are mutually exclusive; no rolled-plus-budget
  hybrid is permitted.
- The point-buy budget and initial allocation are observed, not hard-coded.
- Racial modifiers remain Kingmaker-owned and separate from stored base values.
- No allocator increment/decrement/cost method or save serialization is
  patched.
- Runtime and compatibility qualification require new live evidence.
