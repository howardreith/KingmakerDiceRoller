# AGENTS.md — Kingmaker Dice Roller

## Product boundary

This repository produces one independent Unity Mod Manager mod:

```text
Product:      Kingmaker Dice Roller
Repository:   KingmakerDiceRoller
Assembly:     KingmakerDiceRoller.dll
UMM ID:       KingmakerDiceRoller
Namespace:    KingmakerDiceRoller
Framework:    .NET Framework 4.7.2
Language:     C# 7.3
Game target:  Pathfinder: Kingmaker 2.1.7b
Harmony:      Harmony12
```

It must not become part of the Gunslinger/Tabletop Expansion assembly and must
not create save-owned custom blueprints, facts, unit parts, or components.
Completed characters retain ordinary Kingmaker base ability values.

## Architecture

Keep these concerns separate:

1. Pure dice and assignment domain.
2. Explicit character-creation roll-session lifecycle.
3. Cached Kingmaker reflection contracts.
4. Narrow Harmony entrypoints that delegate immediately.
5. Compatibility detection and policy.
6. UMM/native UI bindings.
7. Deterministic build, package, install, and qualification tooling.

The domain may not reference Kingmaker, Unity, Harmony, or Unity Mod Manager.
Harmony patches may not contain business logic.

## Safety

- Fail closed outside exact supported first-level custom creation:
  - a new campaign main character;
  - player-initiated mercenary recruitment.
- A different established main-character descriptor is rejected unless the
  independent exact custom-companion mercenary discriminator also passes.
- Never patch normal level-up behavior globally.
- Do not patch `StatsDistribution.Add`, `Remove`, `CanAdd`, `CanRemove`, or cost methods.
- Do not generate a new array because a preview or phase was rebuilt.
- Returning to point buy must invoke the observed allocator budget and allow
  other mods' normal point-buy patches to run.
- No game binaries, saves, logs, local paths, or extracted assets in Git.
- Do not claim build, runtime, or compatibility qualification without evidence.

## Testing

Use the project-owned deterministic runner for domain behavior. Keep production
code C# 7.3-compatible. Every runtime defect receives a regression test or a
reproducible smoke-test scenario.

## Git

Work on a mission-specific feature branch. Commit coherent checkpoints. Never
force-push or mix changes from another Kingmaker repository.
