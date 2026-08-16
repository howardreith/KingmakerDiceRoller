# Kingmaker Dice Roller

A standalone Pathfinder: Kingmaker 2.1.7b Unity Mod Manager project for rolled
ability-score arrays during new main-character creation.

The current `0.0.1-alpha.1` milestone contains a fully separated roll domain and
a guarded **fixed-array** Kingmaker integration candidate using:

```text
16, 15, 14, 12, 10, 8
```

It is intentionally not presented as runtime-qualified until it is built
against Howie's exact game assemblies and passes `docs/SMOKE-TEST.md`.

## Safety model

- New main-character creation only.
- Normal level-ups, companions, pets, enemies, pregens, mercenaries, and respec
  are rejected.
- One explicit session owns one `LevelUpState` and one `StatsDistribution`.
- UMM update observation releases canceled/completed ownership only after a
  tested grace period; no extra Harmony lifecycle patch is added.
- No `StatsDistribution.Add`/`Remove` or point-cost patching.
- Return-to-point-buy calls the captured allocator budget, allowing other mods'
  point-buy patches to resume.
- No save-owned custom content.
- The install script validates an exact six-file package and transactionally
  restores the previous live directory if replacement fails.

## Build

```powershell
Copy-Item GamePath.props.example GamePath.props
# Edit GamePath.props, then:
powershell -ExecutionPolicy Bypass -File .\scripts\Qualify.ps1 -Build -Package
```

See `docs/BUILD-AND-RELEASE.md`, `docs/ARCHITECTURE.md`, and
`PROJECT-STATE.md` for exact qualification status.

## Attribution

The expression and dice-mechanic concepts were studied from the MIT-licensed
`FakeFriend24/wotr-dice-roller`. The implementation here is rewritten around a
Kingmaker-specific, fail-closed lifecycle. See `THIRD-PARTY-NOTICES.md`.
