# Kingmaker Dice Roller 0.1.3 - respec testing prerelease

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

**Released for in-game testing.** 316/316 C# service cases and 30/30 Python cases
pass. Exact Kingmaker 2.1.7b and installed Eddic 1.0 contracts were inspected;
eleven respec contract groups include four checks using the installed Harmony
libraries. Release compilation and package validation pass.

Live button clickability, native/Eddic completion, provider retention, catch-up
progression, and save/reload remain **NOT RUN** by the development session. Test
on a disposable copy of a campaign and verify the completed character, including
after saving and reloading. A preview or PASS log alone is not persistence evidence.

newman55 Respecialization is **not integrated**: its exact DLL was unavailable.
Bag of Tricks, Call of the Wild, remote companions, and the full mod profile still
need their focused live checks. Detailed cases are in docs/SMOKE-TEST.md, section J.

Built against DATA's existing UMM 0.33.0 and Harmony12 installation; no loader or
runtime-library downgrade is required. Version 0.1.2 remains the latest stable
release while this prerelease is tested.
