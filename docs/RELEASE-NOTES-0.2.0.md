# Mr. Bard's Kingmaker Roll For Stats UI 0.2.0

Version 0.2.0 brings the native stats UI work from the testing alphas into a
stable GitHub release. The owner authorized this release and merge to the
default branch. The development agent has not completed in-game visual,
save/reload or compatibility qualification.

## What changed

- **Roll for Stats** opens a compact workspace within Kingmaker's ability
  allocator. It includes editable formula presets, Reroll, History, saved
  arrays, score assignment, Details and Return to Point Buy.
- The native **Value** badge shows the active rolled array's point-buy
  equivalent. Its owned numeral and caption keep their intended colors when
  you leave and return to the ability page.
- Roll commands guard against repeated input and incomplete recovery. The
  mod records native refresh diagnostics to help investigate any remaining
  in-game Reroll failure.
- Starting-score ownership and Point Buy restoration stay scoped to qualified
  new main-character, mercenary and supported respec workflows.

## Install

Download **KingmakerDiceRoller-0.2.0.zip** from this release's Assets and
install it with Unity Mod Manager for Pathfinder: Kingmaker 2.1.7b. Install
over an earlier version; the UMM ID and install folder remain
`KingmakerDiceRoller`. GitHub's source archives are not installable UMM
packages.

## Verification and limits

The release pipeline runs the deterministic source and behavior suites,
installed Kingmaker contract checks, a Release build and strict package
validation on the exact release commit. The generated release record includes
the ZIP SHA-256 and commit. In-game visual, save/reload and compatibility
checks have not been completed by the development agent; the testing
procedure is in `docs/SMOKE-TEST.md`.
