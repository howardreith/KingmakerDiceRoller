# Mr. Bard's Kingmaker Roll For Stats UI v0.1.15 testing alpha

This alpha refines the native ability page's Value badge, notices, racial
clearance, and formula tooltip. It removes the Previous, Next, and Return
tooltips. Reroll now admits one command before drawing dice, records the
native failure stage and inner exception, verifies skill and spell allowances
after rollback, and blocks further rolling if recovery is incomplete.

## Install

Download **KingmakerDiceRoller-0.1.15.zip** from this release's Assets, then
install it through Unity Mod Manager for Pathfinder: Kingmaker. GitHub's
automatically generated source archives are not installable UMM packages.

The UMM ID and install folder remain `KingmakerDiceRoller`, so this version
installs over an earlier alpha. It targets Kingmaker 2.1.7b.

## Verification and limits

The exact ZIP passed clean-commit qualification: 415/415 deterministic C#
cases, 20/20 release-gate cases, installed Kingmaker/respec/native UI contract
checks, a zero-warning Release build, and strict package validation. The
package SHA-256 is
`e1c06ad23f290d735285227a487483dcf8a0aa5d33be738c46f48b039c63eac6`.
The implementation commit is
`b3fa9d07042c9946af71eb8aeaa562ff94905944`.

The original intermittent in-game Reroll exception remains unlocated without
a live log. In-game visuals, input, audio, and compatibility checks were not
run for this alpha. The v4 live acceptance procedure is in
`docs/SMOKE-TEST.md`.
