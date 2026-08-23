# Kingmaker Dice Roller

**Current release line:** `0.1.1` is the corrective stable version of the
published `0.1.0` feature set. It advances version metadata so Unity Mod Manager
sorts the stable package above the older `0.1.0-alpha.2` prerelease. No gameplay,
UI, point-buy, persistence, or character-context behavior is intentionally
changed.

Kingmaker Dice Roller is a standalone Unity Mod Manager mod for Pathfinder:
Kingmaker 2.1.7b. It adds an explicit rolled-ability workflow to the native
custom-character ability page through a compact native access tab and a
responsive right-side drawer.

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

The panel starts collapsed for every supported new main-character or mercenary
build. Wide layouts show ordinary Point Buy options and the complete six-score
Roll workflow, History, Saved, summary, and status without wheel input. Compact
layouts retain **Roll Options**, **History**, and **Saved** disclosures and add
bounded vertical scrolling only for measured overflow. Press **Close** to
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

The supported contexts are exact first-level custom creation of a new campaign
main character and player-initiated mercenary recruitment. Mercenaries are
accepted only when Kingmaker's exact custom-companion markers agree for the
owned state and stable controller source. Ordinary level-up, companion
progression, pets, enemies, pregens, respec, unresolved ownership, and unknown
build modes remain excluded. A merely different campaign main character is not
an acceptance signal.

Mercenary Point Buy uses the same observed-origin transaction as main-character
creation. The current allocation and actual live budget are captured before
Roll or Recall and restored exactly; neither 20 nor 25 is hard-coded.

## Installation

Download `KingmakerDiceRoller-0.1.1.zip` from the GitHub release **Assets**
section and install that ZIP with Unity Mod Manager. Do not download GitHub's
automatically generated source archives.

The earlier `0.1.0-alpha.2` package may appear newer than `0.1.0` because Unity
Mod Manager interprets the prerelease text as the numeric version `0.1.0.2`.
Version `0.1.1` supersedes both. After installation, UMM should display `0.1.1`
and should not offer alpha.2 as an update.

The archive contains one `KingmakerDiceRoller` directory with exactly six
allowlisted files and does not bundle development artifacts or game assemblies.

## Qualification

The published `0.1.0` behavior was source-, exact-contract-, build-, package-,
installation-, and human-runtime-qualified against the configured Kingmaker
2.1.7b environment. Because `0.1.1` produces new package and DLL bytes, the exact
corrective artifact must still be rebuilt, package-validated, installed, and
given a brief in-game smoke before publication.

The project does not claim an exhaustive compatibility matrix. Bag of Tricks
and Call of the Wild are detected but never modified. Bag of Tricks budgets are
observed from the live allocator; Call of the Wild racial modifiers remain
separate from rolled base values.

For usage details see `docs/USER-GUIDE.md`. Build, package, checksum, tag, and
release instructions are in `docs/BUILD-AND-RELEASE.md`.
