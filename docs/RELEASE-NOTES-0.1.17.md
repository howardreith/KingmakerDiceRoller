# Mr. Bard's Kingmaker Roll For Stats UI v0.1.17 testing alpha

This alpha keeps the red **Value** badge's numeral and caption in their
intended colors when you return to the ability page after rolling. Kingmaker
rebuilds the page and grays out its native Points numeral once the allocator
is complete. The mod now uses the allocator's available-points colors for its
owned Value badge. The Value number continues to show the active array's
point-buy equivalent.

## Install

Download **KingmakerDiceRoller-0.1.17.zip** from this release's Assets and
install it through Unity Mod Manager for Pathfinder: Kingmaker. GitHub's
automatically generated source archives are not installable UMM packages.
The UMM ID and install folder remain `KingmakerDiceRoller`, so it installs
over an earlier alpha. It targets Kingmaker 2.1.7b.

## Verification and limits

The exact ZIP passed clean-commit qualification: 416/416 deterministic C#
cases, 20/20 release-gate cases, installed Kingmaker/respec/native UI contract
checks, a zero-warning Release build, and strict package validation. The ZIP
SHA-256 is `1ea0856d5440e20390ca734093c051990ef54dbf006616d64c851a87df84f213`.
The implementation commit is `2f480e0f3c55986756d761e5d793aeb04582e771`.

The in-game page-return rendering and live compatibility checks were not run.
The Windows computer-use process exited before window discovery. The visual
acceptance procedure is in `docs/SMOKE-TEST.md`.
