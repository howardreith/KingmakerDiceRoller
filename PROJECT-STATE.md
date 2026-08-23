# Project state

## Current candidate

`0.1.0` is the installed stable-version candidate on `main`.
`0.1.0-alpha.2` remains the published prerelease; no alpha.2 tag, release, or
asset was changed.

The `0.1.0` stable-version candidate combines two changes:

- a responsive upper-right Roll Stats drawer with Wide and Compact profiles;
- exact first-level custom mercenary recruitment support alongside existing
  new campaign main-character creation.

## Implemented behavior

- Both supported creation kinds start in ordinary Point Buy with only the
  compact **Roll Stats** tab visible. No preview, phase, allocator, geometry, or
  UI rebuild generates a roll.
- Wide geometry prefers 620 by 760 UI units and keeps the compact header,
  normal Close control, roll configuration, six assignment rows, summary,
  current History/Saved records, and status visible without ordinary scrolling.
- Compact geometry retains progressive disclosure. Its masked vertical
  `ScrollRect` is enabled only when measured body content overflows; horizontal
  scrolling is always disabled.
- Safe top/right and conservative bottom insets keep the drawer inside the
  relevant parent bounds and away from bottom navigation. Collapse deactivates
  the complete expanded hierarchy and its raycast footprint.
- The exact mercenary discriminator requires both
  `LevelUpState.IsEmployee == true` and
  `UnitHelper.IsCustomCompanion(LevelUpController.Unit) == true` for the same
  controller-owned build. It does not infer mercenary purpose from UI text,
  budget, faction, level, or a different main-character identity.
- New main-character and mercenary sessions carry distinct immutable creation
  kinds and cannot cross-rebind. Stable ownership remains the exact
  `LevelUpController` plus its source `UnitDescriptor`; preview, state,
  distribution, and allocator are replaceable generations.
- Roll or Recall captures exact current allocation, observed budget, remaining
  and total points, allocator availability, and preview base values. Reroll
  never recaptures that origin. Return to Point Buy restores the captured
  values and budget rather than assuming 20 or 25.
- Roll Mode suppresses native plus/minus controls. Completion stores ordinary
  Kingmaker base values only; no blueprint, fact, buff, component, unit part,
  hiring marker, or save-owned record is created.
- Cancellation, completion, phase exit, controller/source loss, disable, and
  unload remove transient session/UI state. Saved arrays remain intentionally
  global UMM settings; session History remains build-scoped.

## Supported integration boundary

Accepted only after all exact ownership, first-level, mode, player-facing,
non-pet, non-enemy, identity, and discriminator checks pass:

1. new custom campaign main-character creation;
2. player-initiated custom mercenary recruitment.

Mercenary discovery against exact Kingmaker 2.1.7b IL established:

```text
CreateCustomCompanion.RunAction
  -> Player.CreateCustomCompanion(...)
  -> HandleLevelUpStart(newCompanion.Descriptor, ..., CharBuildMode.CharGen)

LevelUpState.IsEmployee
  -> UnitHelper.IsCustomCompanion(LevelUpState.Unit)
```

During that path, `LevelUpController.Unit` is the stable custom-companion
descriptor; `LevelUpController.Preview` and `LevelUpState.Unit` are transient
preview descriptors. `Player.MainCharacter` is resolved and remains a different
established descriptor. No ephemeral launch token or additional Harmony patch
was required.

Ordinary level-up, existing-character or companion progression, pets, enemies,
respec, pregenerated selection, unresolved ownership/identity, unmarked
different candidates, and unknown modes remain rejected.

## Qualification truth

For the exact installed `0.1.0` artifact:

- Implemented: **Yes**.
- Source-qualified: **Yes** — repository validation, 258/258 compiled C#
  behavior cases, and 30/30 Python oracle cases pass.
- Contract-qualified: **Yes** — the exact installed Kingmaker 2.1.7b contract
  gate verifies the custom-companion discriminator and the existing four-patch
  recovery/UI surface.
- Build-qualified: **Yes** — the configured exact-game Release build completes
  with zero warnings and zero errors.
- Package-qualified: **Yes** — the deterministic six-file `0.1.0` UMM archive
  is produced and validated by repository-owned tooling.
- Installed: **Yes** — the validated archive is installed transactionally in
  the configured environment with non-target mod fingerprints preserved.
- Runtime-qualified: **Yes** — the repository owner performed and accepted
  human runtime testing of the exact installed `0.1.0` artifact.
- Compatibility-qualified: **No** — historical alpha.2 smoke does not qualify
  the `0.1.0` bytes; Bag of Tricks and Call of the Wild need focused retesting.
- Release-authorized: **Yes** — the repository owner accepted the runtime
  evidence and authorized the exact fully pushed `0.1.0` commit for publication.
- Publicly released: **No**.

Exact target assembly evidence:

```text
Assembly-CSharp MVID:    07fa1e4d-8618-41b3-9b8d-faa17d3b26f7
Assembly-CSharp SHA-256: 3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb
```

Final candidate commit, DLL/package hashes, package path, and installation
result are recorded in the engineering handoff generated from the clean final
branch. Historical hashes are not reused.

## Immediate next gate

Publish the exact fully pushed `0.1.0` commit after local `main` and
`origin/main` both resolve to this release-gate fix. Do not tag or publish a
different commit. `Publicly released` remains **No** until the GitHub release
actually exists.
