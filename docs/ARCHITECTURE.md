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
reporting, and a lifecycle state machine. It has no Kingmaker, Unity, Harmony,
or UMM dependency.

### Reflection contracts

`Integration/KingmakerContractResolver.cs` resolves exact 2.1.7b seams before a
patch is installed. The required constructor, allocator methods, six-ability
order, stat write path, and preview-refresh path must all resolve. Any missing
or ambiguous contract disables the mod rather than applying a partial patch.

### Character-creation coordinator

The coordinator owns all stateful behavior. A `RollSession` binds one unit, one
current `LevelUpState`, one current `StatsDistribution`, the original point-buy
budget, the baseline values, and one immutable assignment. Preview rebuilds for
the same unit rebind the same assignment; a second unit is rejected.

### Harmony boundary

There are three postfix entrypoints only:

1. `LevelUpState(UnitDescriptor, CharBuildMode)` construction.
2. `StatsDistribution.Start(int)`.
3. `StatsDistribution.IsComplete()`.

The bridge catches exceptions and delegates immediately. The project does not
patch increment/decrement/cost controls, race selection, class selection,
level-up commit, respec, or save serialization.

### Point-buy restoration

The original budget is captured from the actual `Start(int)` argument. Returning
to point buy marks the session as restoring and invokes normal `Start(original)`;
other mods' patches therefore execute. The returned distribution values are
then synchronized to the preview unit. A failed restore rolls back to the owned
array and keeps recovery hooks installed.

## Phase boundary

The current live candidate applies only the diagnostic array
`16, 15, 14, 12, 10, 8`. Native character-generation controls, random rolling,
assignment UI, history, persistence, and polished visuals are deliberately
behind the fixed-array runtime gate.
