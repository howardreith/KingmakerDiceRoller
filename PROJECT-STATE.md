# Project state

## Current candidate

`0.1.1` is the corrective stable-version candidate derived from the published
and runtime-qualified `0.1.0` feature set. The published
`0.1.0-alpha.2` prerelease remains immutable.

Unity Mod Manager does not apply Semantic Versioning precedence to prerelease
suffixes. It parses `0.1.0-alpha.2` as `0.1.0.2`, which sorts above `0.1.0`.
The corrective candidate advances every active version surface to `0.1.1`,
which sorts above both earlier archives.

The candidate intentionally changes only:

- UMM, runtime product, assembly, package, and release-note metadata;
- package validation so its expected version comes from root `Info.json`;
- source qualification with an executable UMM-version-ordering regression.

No dice mechanics, assignment rules, point-buy restoration, responsive UI,
character-context policy, persistence behavior, compatibility integration, or
save boundary is intentionally changed.

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

For the exact `0.1.1` corrective artifact:

- Implemented: **Yes**.
- Source-qualified: **Yes** — repository validation, 258/258 compiled C#
  behavior cases, and 30/30 Python oracle cases pass.
- Contract-qualified: **Yes** — exact Kingmaker 2.1.7b contract verification
  passed against the configured installation.
- Build-qualified: **Yes** — the Release build completed with zero warnings and
  zero errors. DLL SHA-256:
  `b8e99c74b378e088de2b79120b95624131a6c53ad7ddf6e87df1fca41096cd89`.
- Package-qualified: **Yes** — the deterministic six-file `0.1.1` package
  validated successfully. Package SHA-256:
  `94aaefd7ba0724e0d2ce8c793529bd6b1bc471179d8d76544cb2c537b2be43f8`.
- Installed: **Yes** — the validated package was transactionally installed into
  the configured Kingmaker Mods directory and its DLL hash matched the
  qualified artifact.
- Runtime-qualified: **Yes** — the repository owner performed and accepted the
  focused human runtime smoke of the exact installed `0.1.1` artifact,
  including UMM version ordering and Roll -> Return to Point Buy.
- Compatibility-qualified: **No** — the broader Bag of Tricks and Call of the
  Wild matrix remains incomplete; no new compatibility claim is introduced.
- Release-authorized: **Yes** — the repository owner accepted the exact
  qualified and runtime-tested `0.1.1` artifact for publication.
- Publicly released: **No**.

Historical `0.1.0` evidence remains valid only for that immutable artifact:
repository validation, 258/258 compiled C# behavior cases, 30/30 Python oracle
cases, exact Kingmaker 2.1.7b contract verification, clean Release build,
deterministic package validation, transactional installation, and owner-accepted
human runtime testing all passed.

Exact target assembly evidence used by `0.1.0`:

```text
Assembly-CSharp MVID:    07fa1e4d-8618-41b3-9b8d-faa17d3b26f7
Assembly-CSharp SHA-256: 3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb
```

## Immediate next gate

From a clean, fully pushed `main`, run the full `0.1.1` qualification and
transactional install. Confirm that Unity Mod Manager displays `0.1.1`, does
not offer `0.1.0-alpha.2` as an update, and that Kingmaker loads the mod without
a red indicator. Then perform one focused Roll -> Return to Point Buy smoke.

Only after recording that exact evidence may this section mark `0.1.1`
source-, contract-, build-, package-, installed-, and runtime-qualified and
release-authorized. The guarded publisher must remain blocked until then.
