# Cleaner settings panel

**Unreleased candidate.** 0.1.11 has not been authorized for release and is
**not runtime-qualified**: it has not been installed or run in Pathfinder:
Kingmaker.

## What changed

- The Unity Mod Manager settings panel is shorter. By default it shows one
  sentence on where roll controls appear and one status line, for example
  **Ready** at the main menu, or whether the current new character, mercenary
  or respec can use Roll Stats or is using rolled scores.
- Concise notices appear only when something needs attention: the mod could
  not start, rolled scores could not be applied, Return to Point Buy did not
  complete, a finished character may not have kept its rolled scores, or a
  detected mod's compatibility is unverified (for example "Compatibility with
  Bag of Tricks is unverified. See Troubleshooting."). Detection is not
  reported as a conflict, and routine rejected contexts are not warnings.
- **Return to Point Buy** (formerly "Emergency: Return active Roll Mode to
  Point Buy") appears only while rolled scores are in use or a roll or
  restore did not finish. Its eligibility and restore checks are unchanged.
  When it is blocked, the panel says why.
- The duplicate name/version heading is removed; Unity Mod Manager already
  shows both.
- Detailed diagnostics remain available under **Show troubleshooting**,
  collapsed by default. It holds the unchanged **Verbose diagnostic logging**
  toggle, workflow/session facts, history and saved counts, counters, contract
  MVID, detected mods and flags, the full compatibility explanations and
  recent diagnostics. Opening it changes nothing.
- Rolling, assignment, Point Buy restoration, the native drawer, saved arrays,
  Harmony patches and `Settings.xml` keys and defaults are unchanged.

## Upgrading

Install the ZIP over the existing installation; do not uninstall first. The
UMM ID, DLL, entry method, install folder and settings format are unchanged,
and the verbose logging preference keeps its saved value.

## Qualification and limitations

- Offline qualification is recorded in `PROJECT-STATE.md` for the exact
  candidate commit.
- Not run for 0.1.11: installation, the rendered UMM panel, in-game rolling,
  live visual/audio checks, save/reload and the compatibility matrix.
