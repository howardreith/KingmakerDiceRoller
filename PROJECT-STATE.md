# Project state

## Current phase

`0.1.0-alpha.2` native-panel usability repair on
`pro/kingmaker-dice-roller-mvp`.

The fixed-array integration vertical slice passed its focused human gate at
`8f78d8243ed27ed2cbb3fadafd890aba172975aa`: live preview continuity, racial
modifier separation, immediate same-page return to pristine point buy,
cancellation, fresh-session ownership, and save independence were observed.
That evidence validates the integration seam, not the new player-facing panel.

The first live player-panel gate proved that Point Buy remained initial, Roll
and Reroll worked, native point stacking stayed prevented, existing-character
progression remained isolated, and Bag of Tricks did not prevent the focused
workflow. It also exposed hard UI blockers at 1600 by 900: the panel reused an
oversized translucent allocator oval, inherited unreadable pale text, displayed
every feature at once, overflowed its curved boundary, and only hid its body
while the full graphic/raycast footprint continued blocking Skills and Next.

The alpha.2 repair keeps the proven roll workflow and replaces that presentation
with a rectangular, masked, high-contrast, progressively disclosed panel plus a
true compact collapsed state. Human visual and click-routing acceptance remains
a separate gate.

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
- Each new stable owner starts with only a compact **Roll Stats** access tab.
  The preferred anchor is beneath the exact active Racial Bonus container, with
  a bounded upper-right fallback.
- The expanded panel is a code-owned 400 by 570 solid rectangle at 0.98 opacity
  with explicit dark body text, outlined light button text, a masked vertical
  scroll viewport, and readable single-line selector values.
- The owned root carries no Graphic. Expanded and collapsed children are
  mutually exclusive, so collapse removes the complete panel layout and
  raycast surface and leaves only the 140 by 34 access tab clickable.
- Point Buy, Roll Options, Roll Mode, History, and Saved arrays use progressive
  disclosure. Irrelevant assignment/history/configuration controls are not
  shown simultaneously.
- Stable-owner UI state preserves open/disclosure choices across allocator
  replacement and navigation, while a new owner resets to collapsed.
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
objects using `m_MainLabel` for font, local native UI materials, and
`m_RaceBonusContainer` for the preferred access-tab anchor. The oval `m_Frame`
sprite is not used as the panel shape. Native
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
- The first alpha.1 product-panel gate at 1600 by 900 failed readability,
  containment, collapse, and click-routing even though Roll/Reroll and
  isolation worked. Those failures are the immediate alpha.2 human gate.

Runtime evidence is stored only in ignored `artifacts/runtime-evidence`
directories and must not be committed.

## Qualification truth

For the alpha.2 candidate:

- Implemented: **Yes** - the player-facing alpha feature set is complete.
- Product implementation: complete alpha candidate.
- Source-qualified: **Pending final clean qualification** - focused validation
  currently passes 212/212 compiled C# behavior cases and 30/30 Python oracle
  cases.
- Build-qualified: **Pending final clean qualification**.
- Installed: **Pending alpha.2 transactional installation**.
- Runtime-qualified: **No** - the new native player workflow awaits human
  acceptance.
- Compatibility-qualified: **No** - the named live matrix awaits human testing.
- Publicly released: **No**.

The qualified exact game contract is Assembly-CSharp MVID
`07fa1e4d-8618-41b3-9b8d-faa17d3b26f7` and SHA-256
`3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`.
Final alpha.2 DLL and package hashes are recorded after the clean committed
qualification gate.

## Immediate next gate

Execute the focused 1600 by 900 readability/collapse/click-routing checklist in
`docs/SMOKE-TEST.md`. Do not proceed to broader product/compatibility acceptance
or promote the alpha until the full panel can be read, collapsed, and leaves
all skill controls plus Back/Next usable.
