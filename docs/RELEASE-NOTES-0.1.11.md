# Cleaner settings panel

**Owner-authorized stable release.** On 2026-10-15 the owner authorized
releasing 0.1.11 as the next version. This release is **not
runtime-qualified**: it was not installed or run in Pathfinder: Kingmaker
before publication.

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

- Offline qualification passed: 397/397 C# behavior cases (14 new
  settings-panel cases driving the real coordinator, context policy, session
  and Point Buy restoration paths), 30/30 Python oracle cases, 20/20
  release-gate cases and repository source validation; read-only Kingmaker
  2.1.7b contract checks, 11 respec contract groups and 30/30 native UI IL
  checks; a Release build with zero warnings/errors; and strict six-file
  package validation.
- Not run for 0.1.11: installation, the rendered UMM panel, in-game rolling,
  live visual/audio checks, save/reload and the compatibility matrix. The
  manual panel check is section K of `docs/SMOKE-TEST.md`.
