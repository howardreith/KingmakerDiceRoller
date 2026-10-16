# Mr. Bard's Kingmaker Roll For Stats UI

Mr. Bard's Kingmaker Roll For Stats UI is a standalone Unity Mod Manager mod
for Pathfinder: Kingmaker 2.1.7b. It adds an explicit rolled-ability workflow
to the native custom-character ability page through a compact native access
tab and a responsive right-side drawer.

It was previously published as **Kingmaker Dice Roller**. Its Unity Mod
Manager ID, DLL, `Mods/KingmakerDiceRoller` install folder and settings are
unchanged, so a new version installs over an earlier one.

The mod starts every supported character in ordinary Point Buy. It rolls only
when you press **Roll**, never because a preview, race, phase, or UI object was
rebuilt.

## Release status

**0.1.11 candidate — cleaner settings panel (unreleased).** The Unity Mod
Manager settings panel now shows where roll controls appear, one status line,
brief notices only when something needs attention, and **Return to Point
Buy** only while rolled scores are in use or a roll or restore did not
finish. Detailed diagnostics remain available under **Show troubleshooting**.
Rolling, assignment, Point Buy restoration, the native UI and saved settings
are unchanged. 0.1.11 is not yet released or runtime-qualified; 0.1.10 below
remains the current official release.

**Release 0.1.10 — Mr. Bard branding (2026-10-15).** Unity Mod Manager now
shows the mod as **Mr. Bard's Kingmaker Roll For Stats UI** by **Mr. Bard**,
and the UMM settings panel uses the same title. Rolling, assignment, Point Buy
restoration, the native UI and saved arrays are unchanged from 0.1.9.

0.1.10 is an owner-authorized stable release. It passed the offline
qualification below but was not installed or run in the game before
publication; it is not runtime-qualified.

**Release 0.1.9 — button caption fit.** This repairs the truncated button
captions reported in play: Store, Previous, Recall, Delete, Down, Return to
Point Buy and the "Low-score rule" caption now reserve the full width of
their actual styled text, measured through the installed TextMesh Pro
preferred-width API and re-measured when the native theme applies or falls
back. Assignment Up/Down pairs keep one aligned width, and Compact layouts
reflow the Return control onto its own row. The native theme, artwork,
typography, click routing and all rolling behavior are unchanged, and the
0.1.8 input-initialization repair is preserved.

0.1.9 was published as an official release at the owner's direction; the
owner tests in play on another computer. Rendered caption verification by
the development agent is recorded in the
[release notes](docs/RELEASE-NOTES-0.1.9.md) and
[project state](PROJECT-STATE.md); a passing build does not claim it.

The release includes the existing skills-page integration: the native red
skill-points badge and phase-completion state refresh after rolled-assignment
changes, including drawer close, and the forward guard blocks leaving Skills
while the native skill allocation is invalid. The 0.1.6 reskin preserves this
behavior and the existing roll workflow.

## Player workflow

The ability page initially shows only a compact **Roll Stats** access tab. It
is bottom-centered within verified ability/allocator geometry, above the native
bottom-navigation inset. It does not cover the Skills page or roll
automatically. Press the tab to open the paper **Rolled Ability Scores**
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
Roll workflow, History, Saved and summary; measured errors can scroll. Compact
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

Completed characters contain ordinary Kingmaker base ability values. The mod
creates no blueprint, fact, buff, component, or unit part and adds no
content to a game save. Saved-array slots use Unity Mod Manager settings, not a
character save.

For mercenaries, matching preview labels and `StatsDistribution` are not treated
as completion. After Kingmaker replays native level-up actions onto its stable
custom-companion descriptor, the mod applies the verified base-only
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

Download **KingmakerDiceRoller-0.1.10.zip** from the
[v0.1.10 release](https://github.com/howardreith/KingmakerDiceRoller/releases/tag/v0.1.10)
**Assets**. Drag that ZIP into Unity Mod Manager's Mods tab for Pathfinder:
Kingmaker and confirm UMM lists **Mr. Bard's Kingmaker Roll For Stats UI**
version **0.1.10**. Releases up to 0.1.9 appear in UMM as **Kingmaker Dice
Roller**; it is the same mod. Keep the installed UMM/Harmony libraries; no
loader downgrade is required. GitHub's Source code archives are not mod
packages.

Version `0.1.10` is the current official release, published on 2026-10-15
at the owner's direction without a new in-game test. Its code is the 0.1.9
code apart from the displayed name, author and version texts. The 0.1.9
caption fit was not verified in-game by the development agent, and live
visual, audio, runtime and compatibility acceptance remain NOT RUN.

The archive contains one `KingmakerDiceRoller` directory with exactly six
allowlisted files and does not bundle development artifacts or game assemblies.

## Upgrading

Install the new ZIP over the existing installation; do not uninstall first.
The UMM ID (`KingmakerDiceRoller`), DLL name and install folder are unchanged,
so the new version replaces the existing entry instead of adding a second
one, even though the displayed name changed in 0.1.10. Saved arrays and roll
preferences live in UMM's `Settings.xml` inside the mod folder; the package
does not contain that file and the settings format is unchanged. Removing the
mod folder (for example by uninstalling) also removes `Settings.xml`, so back
up `Mods/KingmakerDiceRoller/Settings.xml` first if you want to keep saved
arrays through a reinstall.

## Qualification

Offline qualification of 0.1.10 passes 383 deterministic C# cases, 30 Python
oracle cases, 20 release-gate tests and 18 source-validation groups. Kingmaker
2.1.7b contract checks include native derived allowances, replay/copy order,
installed Harmony scope behavior, 11 respec contract groups and 30 native UI
IL/component checks. The Release build has zero warnings/errors and the package
passes the six-file allowlist, manifest identity/branding and hash checks.
0.1.10 has not been installed or run in the game.

Live visual and press-motion acceptance, audio/volume/mute, interactive gameplay,
finalization, save/reload and the provider/input compatibility matrix are
**NOT RUN**. See `PROJECT-STATE.md`, the native book UI matrix in
`docs/SMOKE-TEST.md`, and `docs/CHARACTER-BUILD-INTEGRITY.md` for the verified
native lifecycle behind the existing workflow.

The project does not claim an exhaustive compatibility matrix. Bag of Tricks
and Call of the Wild are detected but never modified. Bag of Tricks budgets are
observed from the live allocator; Call of the Wild racial modifiers remain
separate from rolled base values.

For usage details see `docs/USER-GUIDE.md`. Build, package, checksum, tag, and
release instructions are in `docs/BUILD-AND-RELEASE.md`.
