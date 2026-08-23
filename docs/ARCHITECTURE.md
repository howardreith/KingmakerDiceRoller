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
`NewMainCharacter` or `Mercenary`. It normalizes wrappers through bounded
descriptor resolution. Main-character creation retains its qualified
absent/same/source-preview identity relationships. A different established
campaign main character remains rejected unless independent exact mercenary
evidence passes.

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

### Native UI

`NativeRollPanelHost` attaches code-owned Unity objects to the current exact
ability allocator. `RollPanelPresenter` renders a data-only snapshot and
`RollUiCommandRouter` forwards player commands. Neither view class rolls dice or
writes stats.

`NativeRollPanelState` owns only stable-owner-scoped expand/disclosure choices;
it has no workflow mutation surface and does not persist responsive geometry.
`ResponsiveRollPanelLayoutCalculator` uses primitive parent bounds, safe insets,
preferred body height, and prior layout state to return a data-only Wide or
Compact result. `NativeRollPanelLayoutSpec` makes preferred 620 by 760 Wide
dimensions, compact thresholds, deterministic header/footer/control sizes,
typography floors, conditional scroll policy, and raycast boundaries
executable.

Wide presentation exposes ordinary Point Buy configuration or the complete
six-row Roll workflow with current History/Saved records. Compact presentation
uses stable-owner disclosure choices. The host measures the single fitted body,
enables its masked vertical-only `ScrollRect` only for overflow, and rebuilds
layout only after meaningful geometry/profile/visibility changes. Header,
Close, and status footer remain outside the scroll body.

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

Four narrow postfixes delegate immediately:

- `LevelUpState` constructor;
- `StatsDistribution.Start(int)`;
- `StatsDistribution.IsComplete()`;
- `CharBAbilityScoresAllocator.FillData()`.

No patch contains business logic. Add/remove/cost methods, global progression,
and save serialization are not patched.

## Persistence

Only global product defaults and at most ten saved arrays are serialized by
UMM settings. Active owner, mode, history, point-buy origin, and preview objects
are process/session state and never enter a game save.

Saved schema 1 (values/rule/expression/time) migrates to identity assignment.
Schema 2 stores the source-position permutation and optional label. Unsupported
or malformed entries are isolated and skipped.

## Safety invariants

- Roll mode and spendable point buy cannot coexist.
- A valid recovery path must exist before Roll Mode is entered.
- Race modifiers are not copied into raw arrays or point-buy origins.
- Completion override applies only to the current verified Roll Mode
  distribution.
- Preview/UI rebuilds do not consume random values.
- Main-character and mercenary creation kinds cannot cross-rebind.
- A different main-character identity cannot authorize an unmarked build.
- Disabling during Roll Mode restores exact point buy before unpatching or is
  refused with hooks retained.
