# Nexus Mods release copy

Prepared text for the Nexus Mods page of version `0.1.10`. Nothing has been
uploaded. Version 0.1.10 is not yet tagged or published on GitHub; upload the
exact `KingmakerDiceRoller-0.1.10.zip` asset from the GitHub release once it
exists, without repackaging it. Everything below is based on this repository's
code and its records (`README.md`, `docs/USER-GUIDE.md`,
`docs/COMPATIBILITY.md`, `PROJECT-STATE.md`); update it if those change.

## Title

Mr. Bard's Kingmaker Roll For Stats UI

## Short description

Roll your starting ability scores instead of using point buy. Adds a
native-styled Roll Stats drawer to Pathfinder: Kingmaker character creation,
with 4d6-drop-lowest and other methods, rerolls, minimum-score rules, history,
saved arrays, and an exact Return to Point Buy.

## Description

Mr. Bard's Kingmaker Roll For Stats UI adds an optional "roll for stats" step
to the ability-score page of Pathfinder: Kingmaker character creation. Every
character still starts in ordinary Point Buy; nothing is rolled until you press
**Roll**.

Open the small **Roll Stats** tab at the bottom of the ability page to bring up
the **Rolled Ability Scores** drawer:

- **Roll** and **Reroll** using `4d6, drop lowest`, `4d6, reroll ones, drop
  lowest`, `3d6`, `2d6 + 6`, `1d20`, or your own dice expression (for example
  `4d[6]kh3`).
- Choose a low-score rule: keep all rolls, reroll low scores, or reroll the
  whole array below a minimum.
- Move scores between STR, DEX, CON, INT, WIS and CHA with Up/Down, including
  duplicate values.
- See the raw total and an informational point-buy equivalent.
- Browse the last 20 rolls for the current character and reuse any of them.
- Store up to 10 arrays that persist between sessions, then recall or delete
  them.
- **Return to Point Buy** restores your exact previous allocation and budget,
  including non-default budgets from other mods.

Racial and heritage bonuses stay with the game and are never baked into a
rolled array. Finished characters keep ordinary Kingmaker ability scores, and
the mod adds nothing to your saves.

Rolling is available for:

- a new campaign main character;
- custom mercenaries you recruit;
- player-selected respecs (native, or Eddic's Kingmaker Respec 1.0) that
  reopen all six starting scores.

It is deliberately unavailable for ordinary level-ups, later ability
increases, pets, enemies, pregenerated characters, locked starting scores and
story-companion retraining that keeps the recruitment-level build.

Formerly published as **Kingmaker Dice Roller**.

## Requirements and supported game version

- Pathfinder: Kingmaker **2.1.7b**. The mod was built and checked against the
  2.1.7b game assemblies. It verifies the exact game contracts it relies on,
  fails closed if they differ, and is not supported on other game versions.
- Unity Mod Manager **0.32.0 or newer** (the minimum declared by the mod;
  developed with 0.33.0). Harmony 1.2 (`0Harmony12.dll`) comes with your Unity
  Mod Manager installation; the mod does not bundle it.
- No other mods are required.

## Installation

1. Install Unity Mod Manager for Pathfinder: Kingmaker if you have not already.
2. Download `KingmakerDiceRoller-0.1.10.zip`. Do not extract it.
3. In Unity Mod Manager, select Pathfinder: Kingmaker, open the **Mods** tab
   and drag the ZIP onto it (or use **Install Mod**).
4. Start the game and confirm that **Mr. Bard's Kingmaker Roll For Stats UI**
   version **0.1.10** is listed and enabled in the mod manager.
5. Create a new character. On the ability-score page, press the **Roll Stats**
   tab.

The mod installs into `Mods/KingmakerDiceRoller`. That folder name and the
`KingmakerDiceRoller` file names are the mod's technical identity and are
expected.

## Upgrading from Kingmaker Dice Roller

This is the same mod under a new name. Install 0.1.10 over your existing copy
the normal way; you do not need to uninstall first.

- The Unity Mod Manager ID (`KingmakerDiceRoller`), DLL name and install folder
  are unchanged, so the mod manager updates the existing entry. It does not add
  a second one. After updating, the entry shows the new name and author.
- Saved arrays and roll preferences are stored in
  `Mods/KingmakerDiceRoller/Settings.xml`. The download does not contain that
  file, and 0.1.10 uses the same settings format, so in-place updates keep it.
  Uninstalling or deleting the mod folder removes it, so back it up first if
  you plan to reinstall from scratch.
- 0.1.10 changes only the name and author shown in the mod manager, the
  settings-panel title and the documentation. Rolling and character creation
  behave as in 0.1.9.

These points are based on the unchanged package layout, ID and settings code.
An in-place upgrade has not been exercised in a live Unity Mod Manager
installation for 0.1.10.

## Known limitations

- **Testing status.** 0.1.10 passes the automated suite (deterministic
  behavior tests, contract checks against the 2.1.7b game assemblies, a clean
  Release build and package validation) but has not been run in the game.
  In-game testing so far is hands-on play by the author, most recently of
  0.1.8, whose feedback produced the 0.1.9 button-caption fix; that fix was not
  verified in-game before 0.1.9 was released. Structured in-game testing of
  visuals, audio, save/reload and mod combinations has not been done for
  current versions.
- **Respec mods.** Native respecs and Eddic's Kingmaker Respec 1.0 (exact
  build) are supported. newman55's Respecialization is not supported; its DLL
  was not available for inspection.
- **Other mods.** Bag of Tricks and Call of the Wild are detected and left
  untouched. Bag of Tricks point budgets are read from the live allocator, and
  Call of the Wild racial modifiers stay separate from rolled base values.
  Neither combination has been through the full compatibility test matrix.
  The mod manager settings panel shows a warning when Bag of Tricks is present.
- **History is not saved.** Roll history covers only the current character
  build. Use the saved-array slots to keep arrays between sessions.
- The mod manager settings panel provides diagnostics and an emergency
  **Return active Roll Mode to Point Buy** action. Normal rolling happens on
  the character-creation page.

## Compatibility

- Standalone mod. It has no compile-time dependency on any other gameplay mod
  and never modifies other mods.
- It hooks specific character-creation and level-up commit points and acts
  only in the supported contexts above. Ordinary level-ups are excluded,
  point-buy cost rules are not patched, and other mods' point-buy patches still
  run when you return to Point Buy.
- It creates no blueprints, facts, buffs or unit parts, and adds no content to
  game saves.

## Credits and permissions

- Released under the MIT License. The full license text is in `LICENSE` inside
  the download. Modification and redistribution are permitted under its terms,
  provided the copyright and permission notice is kept.
- Dice-expression, reroll, keep-highest/lowest, roll-array and
  point-buy-equivalent concepts were studied from FakeFriend24's
  `wotr-dice-roller` for Pathfinder: Wrath of the Righteous (MIT; notices for
  JesusLives24 and Jennifer Messerly are preserved in
  `licenses/UPSTREAM-WOTR-DICE-ROLLER-MIT.txt`). This mod is a separate,
  clean Kingmaker implementation and copies no upstream UI, patches or assets.
- Unity Mod Manager and Harmony are separate projects with their own licenses
  and are not redistributed.
- Pathfinder: Kingmaker is © Owlcat Games and other rightsholders. No game
  files are included.

## Bug reports

Report bugs at https://github.com/howardreith/KingmakerDiceRoller/issues.
Include the mod version, your game version, your mod list, what you did, and
the Unity Mod Manager log lines for `KingmakerDiceRoller`. Screenshots help.
