# Project state

## Current phase

`0.1.0-alpha.2` is the owner-authorized first GitHub release candidate on
`main`.

On 2026-08-23, the repository owner explicitly accepted the current feature set
and presentation as release-ready. Additional improvements may be developed in
later versions; they are not blockers for publishing this version.

All repository branches have been compared against `main`. The development
branch `pro/kingmaker-dice-roller-mvp` contains no commits absent from `main`.

## Implemented release behavior

- A supported new-main-character build opens in ordinary Point Buy.
- No roll is generated because character creation, navigation, preview rebuild,
  or UI construction occurs.
- Roll and Recall capture the exact current point-buy origin before entering
  Roll Mode.
- Reroll preserves that origin and replaces the current raw array only after
  live-state verification.
- Position-based Move Up and Move Down controls preserve duplicate-score
  identity.
- Roll Mode suppresses all native ability plus/minus controls and cannot layer
  spendable point-buy points on top of a rolled array.
- Return to Point Buy restores the observed allocation, budget, remaining
  points, total points, allocator availability, distribution, and preview base
  values from immediately before Roll Mode.
- Presets, custom expressions, low-score policies, history, saved arrays, and
  assignment controls are available through the native character-creation
  panel.
- Each new stable owner starts with a compact **Roll Stats** access tab. The
  expanded panel is bounded, masked, scrollable, and fully removed from the
  raycast surface when collapsed.
- Stable-owner UI state survives allocator replacement while cancellation,
  completion, disable, and unload destroy all owned UI.
- Completed characters contain ordinary Kingmaker base ability values. The mod
  creates no save-owned blueprint, fact, buff, component, or unit part.

## Supported integration boundary

The supported owner is the active
`Game.Instance.UI.CharacterBuildController.LevelUpController` together with its
stable source `UnitDescriptor`. The implementation fails closed outside the
supported new custom main-character context and excludes ordinary progression,
companions, pets, enemies, mercenaries, pregens, respec, unknown modes, and a
different established campaign main character.

The exact native UI contract is Pathfinder: Kingmaker 2.1.7b's
`CharBPhaseSkills.AbilityScoresAllocator` and
`CharBAbilityScoresAllocator.FillData()` path. The product targets .NET
Framework 4.7.2, C# 7.3, Unity Mod Manager 0.32.x, and Harmony12.

## Qualification truth

For `0.1.0-alpha.2`:

- Implemented: **Yes**.
- Source-qualified: **Yes** — the clean aggregate gate passed with 88 C# source
  files, 212/212 compiled C# behavior cases, and 30/30 Python oracle cases.
- Build-qualified: **Yes** — the exact Kingmaker build completed with zero
  warnings and zero errors for the qualified alpha.2 implementation.
- Installed: **Yes** — the validated six-file package was installed
  transactionally and non-target mod fingerprints were preserved.
- Runtime-qualified: **Yes** — the focused live workflow evidence passed its
  mechanical gates, and the repository owner has accepted the current native
  panel and click-routing state for release.
- Compatibility-qualified: **Limited** — Bag of Tricks and Call of the Wild have
  positive focused smoke evidence, but this release does not claim an exhaustive
  third-party compatibility matrix.
- Release-authorized: **Yes** — the owner authorized tagging and GitHub release
  publication on 2026-08-23.
- Publicly released: **Pending final local rebuild and GitHub asset upload**.

Historical exact-game evidence remains:

- Assembly-CSharp MVID:
  `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`
- Assembly-CSharp SHA-256:
  `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`

The previously recorded package and DLL hashes describe an earlier clean
alpha.2 packaging commit. They must not be presented as the hashes of a later
release commit. The guarded publisher rebuilds, revalidates, and records fresh
hashes from the exact fully pushed `main` commit used for the release.

## Immediate next gate

The current gate is publication of a freshly rebuilt and package-validated UMM
archive from the exact fully pushed `main` commit. No additional feature or
presentation acceptance is required for this version.

## Release procedure

From the configured Windows Kingmaker development checkout, pull the final
`main` commit and run:

```powershell
powershell -NoLogo -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\Publish-Release.ps1 `
  -Publish `
  -ConfirmRuntimeQualified `
  -ReleaseNotesPath .\docs\RELEASE-NOTES-0.1.0-alpha.2.md
```

Because the current semantic version contains `-alpha.2`, GitHub will label it
a prerelease. It is nevertheless the owner-authorized final artifact for this
version. Any subsequent code change must use a new version rather than replacing
published bytes.
