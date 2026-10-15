# Mr. Bard branding

**Owner-authorized stable release.** On 2026-10-15 the owner authorized
publishing 0.1.10 as an official release without an additional in-game test.
This release is **not runtime-qualified**: it was not installed or run in
Pathfinder: Kingmaker before publication.

## What changed

- Unity Mod Manager now lists the mod as **Mr. Bard's Kingmaker Roll For
  Stats UI** by **Mr. Bard**. Releases up to 0.1.9 were listed as
  **Kingmaker Dice Roller**; it is the same mod.
- The UMM settings panel shows the new name, and two of its status/warning
  messages no longer call the mod "Dice Roller". The Point Buy status message
  now names the on-screen **Roll Stats** panel.
- The DLL's title, product and company metadata carry the new name and
  **Mr. Bard**. The copyright line keeps its holder and year; only a
  mis-encoded copyright sign was repaired.
- Rolling, assignment, Point Buy restoration, the native drawer, saved arrays
  and every Harmony patch are unchanged from 0.1.9.

## Upgrading

Install the ZIP over the existing installation; do not uninstall first. The
UMM ID `KingmakerDiceRoller`, `KingmakerDiceRoller.dll`, its entry method and
the `Mods/KingmakerDiceRoller` folder are unchanged, so the new version
replaces the existing entry instead of adding a second one. Saved arrays and
roll preferences stay in `Mods/KingmakerDiceRoller/Settings.xml`; the package
does not contain that file and its format is unchanged. These statements
follow from the unchanged identity, package layout and settings code; an
in-place upgrade was not exercised in a live Unity Mod Manager installation.

## Qualification and limitations

- Offline qualification passed: 383/383 C# behavior cases, 30/30 Python oracle
  cases, 20/20 release-gate cases and repository source validation;
  read-only Kingmaker 2.1.7b contract checks, 11 respec contract groups and
  30/30 native UI IL checks; a Release build with zero warnings/errors; and
  strict six-file package validation including the branded name/author and the
  unchanged ID, assembly name and entry method.
- Not run for 0.1.10: installation, in-game rolling, live visual/audio checks,
  save/reload and the provider/input compatibility matrix.
- The 0.1.9 button-caption fit has not been verified in-game by the
  development agent.
- Diagnostic log lines keep the technical name "Kingmaker Dice Roller".
