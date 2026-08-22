# Project state

## Current phase

`0.1.0-alpha.1` player-facing candidate development on
`pro/kingmaker-dice-roller-mvp`.

The fixed-array integration vertical slice passed its focused human gate at
`8f78d8243ed27ed2cbb3fadafd890aba172975aa`: live preview continuity, racial
modifier separation, immediate same-page return to pristine point buy,
cancellation, fresh-session ownership, and save independence were observed.
That evidence validates the integration seam, not the new player-facing panel.

The alpha now replaces automatic diagnostic behavior with an explicit native
workflow. The complete source/build/package gate and transactional installation
have passed. Human product acceptance remains a separate gate.

## Implemented alpha behavior

- A valid new-main-character build opens in ordinary Point Buy.
- Session construction, `FillData`, navigation, and preview rebuilds consume no
  random values and stage no array.
- Roll and Recall capture the exact current point-buy origin before entering
  Roll Mode.
- Reroll preserves that origin and replaces the current raw array with an
  identity assignment only after live verification.
- Position-based Move Up/Move Down controls preserve duplicate score identity.
- Roll Mode suppresses all 12 native ability plus/minus buttons and the native
  allocator cannot be layered onto a roll.
- Point Buy restoration uses the observed allocator budget, remaining points,
  total points, allocator availability, distribution values, and preview base
  values from immediately before Roll Mode.
- Return to Point Buy refreshes the exact active `CharBAbilityScoresAllocator`
  page immediately and durably suppresses roll restaging.
- A 20-entry per-build roll history retains raw arrays, assignments, rules,
  expressions, sequence, total, and point-buy equivalent.
- A 10-entry UMM-settings catalog persists arrays and assignments. Schema 1
  records migrate to identity assignment; invalid records are skipped
  individually with a warning; schema 2 preserves the permutation.
- Total and point-buy equivalent exclude racial modifiers. Values outside the
  ordinary 7-18 table use an explicit extended equivalent.
- Presets and the bounded custom parser are available through the native panel.
- The panel attaches once to the exact active Skills ability allocator, follows
  allocator replacement, and detaches on phase exit, cancellation, completion,
  disable, or unload.
- Command failures preserve the prior verified model and workflow state.
- Completed characters retain ordinary Kingmaker base values. No save-owned
  mod content exists.

## Exact integration boundary

The supported owner is the active
`Game.Instance.UI.CharacterBuildController.LevelUpController` plus its stable
source `UnitDescriptor`. Preview state, descriptor, distribution, rollback
snapshot, and UI binding are generation-local.

Acceptance remains fail-closed for unresolved ownership and excludes ordinary
progression, companions, pets, enemies, mercenaries, pregens, respec, unknown
modes, and a different established campaign main character.

The native UI contract is Kingmaker 2.1.7b's
`CharBPhaseSkills.AbilityScoresAllocator` and
`CharBAbilityScoresAllocator.FillData()`. The panel uses code-owned Unity
objects styled from `m_MainLabel`, `m_Frame`, and a local native button. Native
allocator controls are the exact six `CharBScoresEntry.UpButton` and
`DownButton` fields.

## Historical live evidence

- Early candidates established that Kingmaker may construct the genuine
  preview in `LevelUp` mode and before `IsMainCharacter` is set.
- Controller ownership plus main-character descriptor relation identified the
  valid preview while excluding a different established main character.
- Stable controller/source ownership repaired transient preview A-to-B rebuilds.
- Separating transition-time point-buy origin from generation rollback removed
  the rolled-values-plus-full-budget hybrid.
- Exact post-write `FillData()` synchronization removed the stale open-page
  presentation.
- A tested heritage kept racial modifiers separate; backward/forward
  navigation retained the base array; a finished character reloaded without
  Dice Roller or missing-content warnings.
- An ordinary existing-character level-up did not activate the integration.
- Bag of Tricks and Call of the Wild have only positive limited smoke evidence;
  their full matrix is not qualified.

Runtime evidence is stored only in ignored `artifacts/runtime-evidence`
directories and must not be committed.

## Qualification truth

At the installed alpha checkpoint:

- Implemented: **Yes** - the player-facing alpha feature set is complete.
- Product implementation: complete alpha candidate.
- Source-qualified: **Yes** - the clean aggregate gate passed with 85 C#
  source files, 185/185 compiled C# behavior cases, and 30/30 Python oracle
  cases.
- Build-qualified: **Yes** - the exact Kingmaker build completed with zero
  warnings and zero errors.
- Installed: **Yes** - the validated six-file package was installed
  transactionally; the prior Dice Roller installation was backed up and all
  other mod fingerprints remained identical.
- Runtime-qualified: **No** - the new native player workflow awaits human
  acceptance.
- Compatibility-qualified: **No** - the named live matrix awaits human testing.
- Publicly released: **No**.

The qualified exact game contract is Assembly-CSharp MVID
`07fa1e4d-8618-41b3-9b8d-faa17d3b26f7` and SHA-256
`3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`.
The candidate DLL SHA-256 is
`9b0290df82c002f67dfa7cddd08442a3d2208d441a28f47d52c6fa335412f27d`.
The deterministic package is
`artifacts/packages/KingmakerDiceRoller-0.1.0-alpha.1.zip`, SHA-256
`64b43bdfc9bb1d4db674174557fb94b0a9b87589e3b97374f252c0168e496515`.

## Immediate next gate

Execute the consolidated morning checklist in `docs/SMOKE-TEST.md`, beginning
with the vanilla workflow and only then proceeding to the named compatibility
matrix. Do not promote or publicly release the alpha from build evidence alone.
