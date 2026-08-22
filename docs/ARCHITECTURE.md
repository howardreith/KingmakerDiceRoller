# Architecture

## Purpose

Kingmaker Dice Roller is a standalone Unity Mod Manager assembly. It changes
only the ability-allocation mode of one owned new-main-character creation
session. It creates no blueprint, fact, unit part, component, or custom save
payload.

## Layers

### Pure domain

`Domain/` contains the bounded expression parser, deterministic random-source
abstraction, presets, immutable six-score arrays, position-based assignment,
explicit low-score policies, saved-array validation, point-buy-equivalent
reporting, a lifecycle state machine, and the deterministic session-liveness
grace policy. It has no Kingmaker, Unity, Harmony,
or UMM dependency.

### Reflection contracts

`Integration/KingmakerContractResolver.cs` resolves exact 2.1.7b seams before a
patch is installed. The required constructor, allocator methods, six-ability
order, stat write path, preview-refresh path, and active Skills ability-page
refresh/binding path must all resolve. Any missing or ambiguous contract
disables the mod rather than applying a partial patch.

### Character-creation coordinator

The coordinator owns all stateful behavior. A `RollSession` binds immutable
stable ownership (one `LevelUpController` instance and its source
`UnitDescriptor`) separately from a replaceable preview generation (the current
`LevelUpState`, preview descriptor, `StatsDistribution`, and generation rollback
snapshot). Same-owner preview clones rebind the same immutable assignment; a
different controller/source owner is rejected. The legitimate point-buy origin
is captured once, before the first rolled write, and is never replaced by a
later preview generation.

The constructor postfix stages the array without requesting a second preview
refresh. On a later UMM update, after Kingmaker has assigned the replacement
state and replayed its actions, the coordinator verifies the actual controller
state, preview, distribution values, and preview base values. A generation may
receive only its constructor-stage write and one bounded live restage. UMM
updates release ownership only after the stable controller/source relation has
left the domain-tested grace period; exact state replacement is not an exit.

### Harmony boundary

There are three postfix entrypoints only:

1. `LevelUpState(UnitDescriptor, CharBuildMode)` construction.
2. `StatsDistribution.Start(int)`.
3. `StatsDistribution.IsComplete()`.

The bridge catches exceptions and delegates immediately. The project does not
patch increment/decrement/cost controls, race selection, class selection,
level-up commit, respec, or save serialization.

### Point-buy restoration

`PristinePointBuyState` captures the first generation's actual `Start(int)`
budget and provenance, legitimate distribution/unit values, and allocator
fields before roll mode writes anything. It is immutable for the stable build
owner. `GenerationRollbackSnapshot` captures the current generation immediately
before staging and may be replaced on each same-owner rebind; it repairs a
failed transactional write but can never become the user-facing point-buy
origin.

Roll mode explicitly makes the allocator unavailable and owns completion.
Returning to point buy enters `RestoringPointBuy` and performs one guarded
`UpdatePreview()`. A replacement constructed inside that call rebinds without
fixed-array application. Normal `Start(pristineBudget)` then runs against the
newest distribution so other mods' patches execute, after which the pristine
allocation and allocator fields are restored and verified against the live
controller state/preview. Success enters durable `PointBuy` mode: later
same-owner rebuilds do not reapply the roll and the completion postfix leaves
ordinary allocator behavior alone. A rolled-array-plus-full-budget hybrid is an
explicit verification failure. A failed restore returns the isolated roll to
the newest live generation and keeps recovery hooks installed, or refuses to
disable if that rollback cannot be proven.

Semantic restoration and native presentation synchronization are deliberately
separate. After the live model is verified and durable PointBuy mode is active,
`AbilityPhasePresentationService` invokes exactly one
`CharBAbilityScoresAllocator.FillData()` for that session generation. Exact
2.1.7b IL shows that this method rereads the controller's current state,
distribution, source/preview units, racial modifiers, allocator points, costs,
and button availability. The service then verifies the allocator's native
source and preview bindings against the live session. It never calls
`UpdatePreview()`, cannot stage the fixed assignment, and refuses nested or
repeat refreshes for the same generation. A presentation failure leaves the
semantically safe PointBuy model in place and is reported independently.

## Phase boundary

The current live candidate applies only the diagnostic array
`16, 15, 14, 12, 10, 8`. Native character-generation controls, random rolling,
assignment UI, history, persistence, and polished visuals are deliberately
behind the immediate point-buy presentation runtime gate.
