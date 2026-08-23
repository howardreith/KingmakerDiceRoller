# Kingmaker Dice Roller 0.1.0-alpha.2

This is the first owner-authorized GitHub release of Kingmaker Dice Roller for
Pathfinder: Kingmaker 2.1.7b.

## Highlights

- Starts supported new custom main characters in ordinary Point Buy.
- Rolls only in response to an explicit Roll or Recall command.
- Provides Roll, Reroll, multiple presets, custom dice expressions, minimum-score
  policies, duplicate-safe assignment, history, and persistent saved arrays.
- Suppresses Kingmaker's native point-buy controls while Roll Mode is active.
- Restores the exact pre-roll point-buy allocation, budget, and current native
  presentation through **Return to Point Buy**.
- Keeps race and heritage modifiers separate from rolled base scores.
- Uses a compact collapsed **Roll Stats** tab and a bounded, masked, scrollable
  native panel that releases its complete click footprint when closed.
- Creates no save-owned blueprint, fact, buff, unit part, or component.
- Fails closed outside the supported new-main-character context.

## Installation

1. Download `KingmakerDiceRoller-0.1.0-alpha.2.zip` from **Assets** below.
2. In Unity Mod Manager, select Pathfinder: Kingmaker.
3. Drag the ZIP into the **Mods** tab.
4. Launch the game and confirm **Kingmaker Dice Roller** is enabled.

Do not download GitHub's automatically generated **Source code** archives; they
are not the Unity Mod Manager package.

## Compatibility

- Pathfinder: Kingmaker 2.1.7b
- Unity Mod Manager 0.32.x
- Harmony12
- .NET Framework 4.7.2

Bag of Tricks and Call of the Wild have positive focused smoke evidence and are
never modified by Dice Roller. This release does not claim an exhaustive
third-party compatibility matrix.

## Release policy

The repository owner authorized this exact version for release on 2026-08-23.
Any later code or presentation change will use a new version instead of replacing
this release's assets.
