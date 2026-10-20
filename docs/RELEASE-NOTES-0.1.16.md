# Mr. Bard's Kingmaker Roll For Stats UI v0.1.16 testing alpha

This alpha corrects the native **Value** badge in Roll Mode. It now displays
the active rolled array's **point-buy equivalent**, calculated from the six
base scores. The six-score total remains available in Details. The badge
updates after Reroll, History, Recall, and assignment changes; Roll-ready
shows a dash, and Return to Point Buy restores the game's native Points
caption and remaining budget.

## Install

Download **KingmakerDiceRoller-0.1.16.zip** from this release's Assets and
install it through Unity Mod Manager for Pathfinder: Kingmaker. GitHub's
automatically generated source archives are not installable UMM packages.
The UMM ID and install folder remain `KingmakerDiceRoller`, so it installs
over an earlier alpha. It targets Kingmaker 2.1.7b.

## Verification and limits

The exact ZIP passed clean-commit qualification: 416/416 deterministic C#
cases, 20/20 release-gate cases, installed Kingmaker/respec/native UI contract
checks, a zero-warning Release build, and strict package validation. The ZIP
SHA-256 is `174e21f2c2ef0f65f0dbe66a8619dc3873f37daf49b504adce94e0ec57eb03ca`.
The implementation commit is `8e9d60d00a232e327c4207317ce4cdee8b7ee49a`.

The in-game badge rendering and live compatibility checks were not run. The
Windows computer-use process exited before window discovery. The visual
acceptance procedure is in `docs/SMOKE-TEST.md`.
