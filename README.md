# Kingmaker Dice Roller

**Release status:** `0.1.0-alpha.2` is owner-authorized for GitHub release.
Future improvements will use later versions rather than replacing the published
artifact.

Kingmaker Dice Roller is a standalone Unity Mod Manager mod for Pathfinder:
Kingmaker 2.1.7b. It adds an explicit rolled-ability workflow to the native
new-character ability page through a compact, readable native panel.

The mod starts every supported character in ordinary Point Buy. It rolls only
when you press **Roll**, never because a preview, race, phase, or UI object was
rebuilt.

## Player workflow

The ability page initially shows only a compact **Roll Stats** access tab. It
does not cover the Skills page or roll automatically. Press the tab to open the
rectangular **Rolled Ability Scores** panel. The panel provides:

- Roll and Reroll;
- `4d6, drop lowest`, `4d6, reroll ones, drop lowest`, `3d6`, `2d6 + 6`,
  `1d20`, and a custom expression;
- tabletop, per-score minimum, and whole-array minimum policies;
- position-based Move Up/Move Down assignment that handles duplicate values;
- base-score total and informational point-buy equivalent;
- a 20-entry history for the current character build;
- 10 persistent saved arrays with Store, Recall, and Delete;
- immediate Return to Point Buy.

The panel starts collapsed for each new character build. **Roll Options**,
**History**, and **Saved** are disclosed only when needed. Press **Close** to
remove the entire expanded surface and its click footprint; only the small
access tab remains.

Custom syntax examples are `4d[6]kh3`, `4d[6]r[1]kh3`, and `2d[6]+6`.
Generated scores are validated within the explicit 1-120 product boundary and
are never silently clamped.

## Roll Mode and Point Buy

Point Buy remains authoritative until Roll or Recall is pressed. Entering Roll
Mode captures the exact current allocation, remaining points, total budget, and
allocator state. Roll Mode disables Kingmaker's native plus/minus controls and
never layers spendable points on top of a rolled array.

**Return to Point Buy** restores that exact pre-roll state on the current live
preview and refreshes the open page immediately. This includes legitimate
non-default budgets supplied by another mod; the implementation does not
hard-code 25 points or six scores of 10.

Race and heritage modifiers remain Kingmaker-owned. Arrays, history, and saved
slots contain base values only.

## Persistence and safety

Completed characters contain ordinary Kingmaker base ability values. Dice
Roller creates no blueprint, fact, buff, component, or unit part and adds no
content to a game save. Saved-array slots use Unity Mod Manager settings, not a
character save.

The integration fails closed outside the exact supported new custom main
character context. Ordinary progression, companions, pets, enemies,
mercenaries, pregens, and respec are excluded.

## Installation

Download `KingmakerDiceRoller-0.1.0-alpha.2.zip` from the GitHub Release's
**Assets** section. Do not download GitHub's automatically generated source-code
archives.

Install the ZIP with Unity Mod Manager, or extract its single
`KingmakerDiceRoller` directory under the game's `Mods` directory. The package
contains exactly six allowlisted files and does not bundle development
artifacts or game assemblies.

## Qualification

This version is source- and build-qualified against the exact local Kingmaker
2.1.7b assembly. Focused live testing established Point Buy-first behavior,
Roll and Reroll, allocator isolation, exact Point Buy restoration, character
save independence, ordinary-level-up exclusion, and positive limited smoke with
Bag of Tricks and Call of the Wild. The repository owner accepted the current
native panel and click-routing behavior for release on 2026-08-23.

The release does not claim an exhaustive compatibility matrix for every mod
combination. Bag of Tricks and Call of the Wild are detected but never modified.
Bag of Tricks budgets are observed from the live allocator; Call of the Wild
racial modifiers remain separate from rolled base values.

For usage details see `docs/USER-GUIDE.md`. Build, package, checksum, tag, and
release instructions are in `docs/BUILD-AND-RELEASE.md`.
