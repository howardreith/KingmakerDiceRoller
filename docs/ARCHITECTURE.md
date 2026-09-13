# Architecture

## Product boundary

Kingmaker Dice Roller is one independent Unity Mod Manager assembly targeting
.NET Framework 4.7.2 and C# 7.3. It does not depend on Gunslinger, Tabletop
Expansion, Bag of Tricks, or Call of the Wild. It creates no blueprint, fact,
buff, component, unit part, or save-owned record.

## Layers

### Pure domain

`Domain` contains the bounded expression parser, deterministic roll engine,
presets, low-score policies, immutable six-score arrays, source-position
assignment, point-buy equivalent, history, and saved-record validation. It has
no Kingmaker, Unity, Harmony, or UMM references.

### Character workflow

`CharacterRollWorkflow` owns player-facing configuration, current rule
metadata, error/status text, history, saved catalog, and immutable UI snapshots.
It computes proposed state but never reflects into Kingmaker or creates Unity
objects.

`CharacterCreationCoordinator` is the command transaction boundary. A command
captures current live state, computes a candidate, stages it, invokes the exact
native refresh, verifies the current controller model and controls, and commits
the workflow only after verification. Failure restores the generation rollback
snapshot and leaves the prior workflow state intact.

### Session state

A `RollSession` separates stable build ownership from transient preview state:

```text
Stable owner
  exact LevelUpController instance
  exact controller source UnitDescriptor

Current generation
  LevelUpState
  preview UnitDescriptor
  StatsDistribution
  GenerationRollbackSnapshot

Creation purpose
  NewMainCharacter or Mercenary
```

The mode state machine is:

```text
PointBuy -> EnteringRollMode -> Roll -> RestoringPointBuy -> PointBuy
```

New sessions start in PointBuy. No roll is generated during construction or
rebind. A same-owner preview clone replaces current-generation references but
never generates another array or replaces the point-buy origin.

`PointBuyOrigin` is captured at the explicit PointBuy-to-Roll transition. It
contains the legitimate allocation, actual observed budget and provenance,
remaining/total points, and allocator availability. It is distinct from the
per-generation rollback snapshot used only to recover a failed write.

### Kingmaker integration

`KingmakerContractResolver` resolves and caches exact 2.1.7b types, members,
signatures, instance/static status, and writable properties. Unknown contracts
fail enablement closed before Roll Mode can be offered.

`CharacterCreationContextPolicy` classifies the accepted purpose explicitly as
`NewMainCharacter`, `Mercenary`, or separately owned `Respec`. It normalizes wrappers through bounded
descriptor resolution. Main-character creation retains its qualified
absent/same/source-preview identity relationships. A different established
campaign main character remains rejected unless independent exact mercenary
evidence or a qualified respec launch/copy relationship passes.

Mercenary evidence is game-owned and read-only: the owned state must report
`LevelUpState.IsEmployee`, and the stable controller source must independently
pass exact `UnitHelper.IsCustomCompanion(UnitDescriptor)`. Exact IL proves the
state property delegates to that same helper. Mercenaries additionally require
the observed `CharGen` mode, first level, player faction, non-main, non-pet,
non-enemy flags, owned state/preview/distribution, a resolved different campaign
main character, and a stable custom-companion source. No UI text or point budget
participates in classification.

`StatApplicationService`, `PointBuyRestoreService`, and
`AbilityPhasePresentationService` separately own model staging, semantic
restoration, and native page synchronization. Success is never inferred from a
detached object.

`DerivedStateRefreshService` owns the shared derived-state transaction for every
supported starting-score context. Exact 2.1.7b IL proves `LevelUpState` caches
`IntelligenceSkillPoints` (written only by the `ApplySkillPoints` action) and
`OnApplyAction()` recomputes `TotalSkillPoints` plus per-spellbook
`UpdateMaxLevelSpells` from those caches. After staging or restoring scores, the
service redoes the native bookkeeping against the live unit and invokes
`OnApplyAction()`, so UI allowances and authoritative replay agree. It captures
its previous values so command rollback covers this semantic effect too. See
`docs/CHARACTER-BUILD-INTEGRITY.md` for the verified lifecycle and rules.

`MercenaryFinalizationService` is a separate reflection-backed lifecycle
service. Exact 2.1.7b IL proves that `LevelUpState` is constructed on the stable
controller source only inside `Commit` (`UpdatePreview` only ever constructs on
a fresh preview clone), so a one-use commit ticket stages the verified rolled
assignment on that exact state before native action checks consume ability
values. The `ApplyLevelup` postfix verifies the replay without corrective late
writes and reports recorded actions dropped by native checks; the `Commit`
postfix verifies the final recipient and closes the session. Interruption
restores the exact captured pre-commit state. The new-main commit seam stages
through the same-owner rebind before replay and audits the final recipient at
the `Commit` postfix. Neither path handles `Respec`, and neither writes
modifiers.

### Native UI

`NativeRollPanelHost` attaches code-owned Unity objects to the current exact
ability allocator. `RollPanelPresenter` renders a data-only snapshot and
`RollUiCommandRouter` forwards player commands. Neither view class rolls dice or
writes stats.

`NativeRollPanelState` owns only stable-owner-scoped expand/disclosure choices;
it has no workflow mutation surface and does not persist responsive geometry.
`CollapsedAccessTabLayoutCalculator` uses primitive local bounds to select
active racial-bonus, allocator-frame, allocator-region, or ability-root
geometry and returns a bottom-center position above native navigation. It has no
upper-right fallback.
`ResponsiveRollPanelLayoutCalculator` uses primitive parent bounds, safe insets,
preferred body height, and prior layout state to return a data-only Wide or
Compact result. `NativeRollPanelLayoutSpec` makes preferred 600 by 728 Wide
dimensions, compact thresholds, fixed header, no reserved footer, control sizes,
typography floors, conditional scroll policy, and raycast boundaries
executable.

Wide presentation exposes ordinary Point Buy configuration or the complete
six-row Roll workflow with current History/Saved records. Compact presentation
uses stable-owner disclosure choices. The host measures the single fitted body,
enables its masked vertical-only `ScrollRect` only for overflow, and rebuilds
layout only after meaningful geometry/profile/visibility changes. Header
and Close remain outside the scroll body; conditional messages use measured body
space. Theme donors and artwork are resolved once per attachment through
`NativeBookTheme`; `NativeUiPresentation` isolates cosmetic fallback and click
feedback from workflow commands. Fresh buttons receive a new event with one
listener, never copied native callbacks. The paper and shadow lie outside the
inner body mask. See `NATIVE-UI-STYLE.md` for version-qualified resource paths.

The host's top-level object has no Graphic. Its expanded rectangular surface
and compact collapsed access tab are mutually exclusive, and all noninteractive
TMP labels reject raycasts.

The panel is driven by the narrow `CharBAbilityScoresAllocator.FillData()`
postfix plus a bounded UMM-update lifecycle observer. Repeated FillData calls
refresh one panel; allocator replacement rebinds it; invalid context detaches
it. Same-owner allocator replacement preserves the presentation choice; a new
stable owner resets to collapsed. Only the exact root created by this host is
destroyed.

In Roll Mode, `NativeAbilityControlService` records and suppresses the exact 12
`CharBScoresEntry` Up/Down `interactable` states. Point-buy FillData restores
native authoritative state; disable/phase cleanup restores any still-owned
states.

### Harmony

Six narrow postfixes delegate immediately:

- `LevelUpState` constructor;
- `StatsDistribution.Start(int)`;
- `StatsDistribution.IsComplete()`;
- `CharBAbilityScoresAllocator.FillData()`;
- `LevelUpController.ApplyLevelup(UnitDescriptor)` (captures the surviving
  action list for dropped-selection evidence);
- `LevelUpController.Commit()`.

No patch contains business logic. Add/remove/cost methods, global progression,
and save serialization are not patched.

## Persistence

Only global product defaults and at most ten saved arrays are serialized by
UMM settings. Active owner, mode, history, point-buy origin, and preview objects
are process/session state and never enter a game save.

Completed mercenary base values are ordinary fields on the same native
`LevelUpController.Unit` descriptor that `SetupNewCharacher` inserts into player
companion ownership. Dice Roller adds no character-owned persistence record.

Saved schema 1 (values/rule/expression/time) migrates to identity assignment.
Schema 2 stores the source-position permutation and optional label. Unsupported
or malformed entries are isolated and skipped.

## Safety invariants

- Roll mode and spendable point buy cannot coexist.
- A valid recovery path must exist before Roll Mode is entered.
- Race modifiers are not copied into raw arrays or point-buy origins.
- A matching transient distribution/preview cannot qualify mercenary
  completion; the exact stable descriptor must be verified through the
  pre-replay commit ticket.
- Mercenary completion stages before native replay and never corrects six base
  values after checks have already consumed different scores.
- Preview/UI rebuilds do not consume random values.
- Main-character and mercenary creation kinds cannot cross-rebind.
- A different main-character identity cannot authorize an unmarked build.
- Disabling during Roll Mode restores exact point buy before unpatching or is
  refused with hooks retained.

## Respec extension

`NativeRespecContracts` caches optional game contracts. `NativeRespecEntryService`
qualifies the selected original, native callback, rebuild source, current provider,
and party/remote membership. `RespecOwnership` carries those immutable identities;
`RespecLifecycleService` owns the first-level commit ticket and post-copy checks.
Harmony bridges delegate to these services. The pure dice domain is unchanged.
The shared derived-state refresh is not respec-specific: it runs for every
supported creation kind from the coordinator.

Respec is admitted only when starting allocation is editable. Sessions remain
PointBuy-first and preserve their assignment/origin across owned preview
replacement. The ticket stages before native first-level action checks, verifies
replay, observes the exact copy callback, rereads the original entity's current
descriptor, and closes before ordinary catch-up. The recruitment-only
finalization seam and its immutable Mercenary guard are retained.

The point-buy hybrid check accepts coincidentally identical rolled/origin values
only when independent pre-Roll provenance and all live origin fields match.
No point-buy equivalent or fixed budget is used for restoration.
