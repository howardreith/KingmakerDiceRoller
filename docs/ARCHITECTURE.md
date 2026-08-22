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
order, stat write path, and preview-refresh path must all resolve. Any missing
or ambiguous contract disables the mod rather than applying a partial patch.

### Character-creation coordinator

The coordinator owns all stateful behavior. A `RollSession` binds immutable
stable ownership (one `LevelUpController` instance and its source
`UnitDescriptor`) separately from a replaceable preview generation (the current
`LevelUpState`, preview descriptor, `StatsDistribution`, and point-buy
baseline). Same-owner preview clones rebind the same immutable assignment; a
different controller/source owner is rejected.

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

Each preview generation captures its original budget from the actual
`Start(int)` argument and its untouched baseline before the fixed array is
staged. Returning to point buy marks the session as restoring and performs one
guarded `UpdatePreview()`. A replacement constructed inside that call rebinds
without fixed-array application. Normal `Start(original)` then runs against the
newest distribution so other mods' patches execute, and the newest baseline is
restored to the newest preview. A failed restore rolls the owned array back onto
the newest live generation and keeps recovery hooks installed (or refuses to
disable if that cannot be proven).

## Phase boundary

The current live candidate applies only the diagnostic array
`16, 15, 14, 12, 10, 8`. Native character-generation controls, random rolling,
assignment UI, history, persistence, and polished visuals are deliberately
behind the fixed-array runtime gate.
