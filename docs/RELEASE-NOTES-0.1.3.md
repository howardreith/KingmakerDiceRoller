# Kingmaker Dice Roller 0.1.3

This release addresses the missing **Roll Stats** control in eligible native and
Eddic respecs that reopen all six starting ability scores. It adds explicit
ownership of the selected character, rebuild source, and completion callback,
then checks that the starting assignment reaches the completed recipient.

- Reuse one roll and assignment across preview and allocator replacement.
- Restore the exact pre-Roll Point Buy allocation and observed budget, including
  origins whose scores were already rolled.
- Close the initial roll session before catch-up levels and later ability increases.
- Preserve native racial modifiers and locked identity phases; ordinary level-up,
  pets, enemies, and retraining with locked starting scores stay ineligible.

**Official release following owner confirmation.** Howie confirmed that the
released workflow works and requested official release. This promotion retains
the exact ZIP, DLL, checksum, and tag from the tested prerelease.

316/316 C# service cases and 30/30 Python cases pass. Exact Kingmaker 2.1.7b and installed Eddic 1.0 contracts were inspected;
eleven respec contract groups include four checks using the installed Harmony
libraries. Release compilation and package validation pass.

Owner confirmation provides release acceptance for the tested workflow. The
confirmation did not enumerate individual provider/character cases or save/reload
results. Those detailed compatibility lanes remain unverified; no separate
agent-operated gameplay run or broad persistence qualification is claimed.

newman55 Respecialization is **not integrated**: its exact DLL was unavailable.
Bag of Tricks, Call of the Wild, remote companions, and the full mod profile still
need their focused live checks. Detailed cases are in docs/SMOKE-TEST.md, section J.

Built against DATA's existing UMM 0.33.0 and Harmony12 installation; no loader or
runtime-library downgrade is required. Version 0.1.3 is the latest official
release. The ZIP retains its original testing README to preserve the accepted
package bytes; the release page records the current status.
