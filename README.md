# Kingmaker Dice Roller

A standalone Pathfinder: Kingmaker 2.1.7b Unity Mod Manager project for rolled
ability-score arrays during new main-character creation.

The current `0.0.1-alpha.1` milestone contains a fully separated roll domain and
a guarded **fixed-array** Kingmaker integration candidate using:

```text
16, 15, 14, 12, 10, 8
```

It is intentionally not presented as runtime-qualified until the repaired
point-buy restoration and remaining vanilla gates pass `docs/SMOKE-TEST.md`.

## Safety model

- New main-character creation only.
- Normal level-ups, companions, pets, enemies, pregens, mercenaries, and respec
  are rejected.
- One explicit session owns a stable character-build controller/source pair and
  rebinds its immutable assignment to each transient preview state and
  distribution from that same owner.
- UMM update observation verifies the actual live controller preview and
  releases canceled/completed ownership only after the stable owner leaves a
  tested grace period; no extra Harmony lifecycle patch is added.
- No `StatsDistribution.Add`/`Remove` or point-cost patching.
- Roll mode disables the point-buy allocator; it never layers a live budget on
  top of rolled values.
- Return-to-point-buy follows the newest preview, calls the observed pristine
  allocator budget, restores the first pre-roll allocation, and enters durable
  point-buy mode for that build owner.
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
