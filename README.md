# Kingmaker Dice Roller

**Latest official release:** `0.1.3`. Adds scoped native/Eddic starting-score
respec integration. Howie confirmed the released workflow works and authorized
official release. Service tests, exact assembly contracts, and builds pass.

Kingmaker Dice Roller is a standalone Unity Mod Manager mod for Pathfinder:
Kingmaker 2.1.7b. It adds an explicit rolled-ability workflow to the native
custom-character ability page through a compact native access tab and a
responsive right-side drawer.

The mod starts every supported character in ordinary Point Buy. It rolls only
when you press **Roll**, never because a preview, race, phase, or UI object was
rebuilt.

## Player workflow

The ability page initially shows only a compact **Roll Stats** access tab. It
is bottom-centered within verified ability/allocator geometry, above the native
bottom-navigation inset. It does not cover the Skills page or roll
automatically. Press the tab to open the rectangular **Rolled Ability Scores**
panel. The panel provides:

- Roll and Reroll;
- `4d6, drop lowest`, `4d6, reroll ones, drop lowest`, `3d6`, `2d6 + 6`,
  `1d20`, and a custom expression;
- tabletop, per-score minimum, and whole-array minimum policies;
- position-based Move Up/Move Down assignment that handles duplicate values;
- base-score total and informational point-buy equivalent;
- a 20-entry history for the current character build;
- 10 persistent saved arrays with Store, Recall, and Delete;
- immediate Return to Point Buy.

The panel starts collapsed for every supported creation or eligible respec
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

For mercenaries, matching preview labels and `StatsDistribution` are not treated
as completion. After Kingmaker replays native level-up actions onto its stable
custom-companion descriptor, Dice Roller applies the verified base-only
assignment at that exact seam and verifies the same descriptor after the native
success callback. A mismatch is logged as a final failure.

Supported contexts are new-main creation, custom mercenary recruitment, and
player-selected native/Eddic respecs that reopen all six starting scores.
Respecs require exact original/rebuild/preview ownership and the inspected
native copy callback. Native story-companion retraining that preserves the
recruitment-level build remains ineligible. Ordinary level-up, pets, enemies,
pregens, locked starting scores, and unknown ownership remain excluded.
newman55 Respecialization has no adapter in this release: its exact DLL was
unavailable for inspection.

For eligible respecs the same panel starts in Point Buy. The starting assignment
is applied once before native first-level action checks, verified after replay
and after copying into the original character, then released before catch-up
levels. Race modifiers and later ability increases remain native.

Mercenary Point Buy uses the same observed-origin transaction as main-character
creation. The current allocation and actual live budget are captured before
Roll or Recall and restored exactly; neither 20 nor 25 is hard-coded.

## Installation

Download **KingmakerDiceRoller-0.1.3.zip** from the
[0.1.3 release](https://github.com/howardreith/KingmakerDiceRoller/releases/tag/v0.1.3),
then drag that ZIP into Unity Mod Manager's Mods tab for Pathfinder: Kingmaker.
Confirm UMM displays version **0.1.3**. Keep the installed UMM/Harmony libraries;
no loader downgrade is required. GitHub's Source code archives are not mod packages.

Version `0.1.3` is the latest official release. It uses the same ZIP and DLL as
the owner-confirmed prerelease; the archive's original testing README is retained
to preserve those published bytes.

The archive contains one `KingmakerDiceRoller` directory with exactly six
allowlisted files and does not bundle development artifacts or game assemblies.

## Qualification

The release has deterministic service coverage and exact-contract checks against
Kingmaker 2.1.7b, including native replay/copy order and installed Harmony scope
behavior. Howie's confirmation of the released 0.1.3 workflow supplies the human
release sign-off. Individual provider/character cases and save/reload results were
not specified in that confirmation, so the detailed matrix remains separately
unverified. See `PROJECT-STATE.md` and `docs/SMOKE-TEST.md` for its scope.

The project does not claim an exhaustive compatibility matrix. Bag of Tricks
and Call of the Wild are detected but never modified. Bag of Tricks budgets are
observed from the live allocator; Call of the Wild racial modifiers remain
separate from rolled base values.

For usage details see `docs/USER-GUIDE.md`. Build, package, checksum, tag, and
release instructions are in `docs/BUILD-AND-RELEASE.md`.
