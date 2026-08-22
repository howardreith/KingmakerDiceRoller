# Phase 2 runtime smoke test

## Preconditions

1. Use Pathfinder: Kingmaker 2.1.7b and Unity Mod Manager 0.32.x.
2. Copy `GamePath.props.example` to `GamePath.props` and point it at the actual
   install.
3. Run:

   ```powershell
   powershell -ExecutionPolicy Bypass -File .\scripts\Qualify.ps1 -Build -Package
   ```

4. Record the contract report, DLL SHA-256, package SHA-256, branch, commit, and
   dirty status from `artifacts/`.
5. Install with `scripts/Install.ps1`. Verify it only changed
   `Mods\KingmakerDiceRoller`.
6. Preserve a copy of relevant UMM settings. Start with Bag of Tricks disabled
   for the first gate.

## Focused pristine-restoration gate

The fixed-array entry/preview-continuity seam passed live at `907f1bc1...`.
Before repeating the full vanilla matrix, use a fresh process with Call of the
Wild and Bag of Tricks disabled:

1. Start a new human and confirm `16, 15, 14, 12, 10, 8` appears.
2. Press **Return active roll session to point buy**.
3. Confirm all rolled values disappear and ordinary vanilla allocation with 25
   points returns.
4. Spend and refund points with the normal plus/minus controls.
5. Navigate backward and forward; confirm point buy remains active and the
   fixed array does not reappear.
6. Repeat with an elf. Confirm the restored base allocation is ordinary point
   buy while racial modifiers remain separate.
7. Restart the test, leave roll mode active, and disable Dice Roller. It must
   restore clean point buy before disabling or refuse to disable safely.
8. Complete a separate fresh character entirely in roll mode, save, quit,
   disable Dice Roller, and reload. Confirm its legitimate rolled values remain.
9. Test one existing-character or companion level-up and confirm the fixed
   array never activates.

Stop and collect runtime evidence if any step fails. Do not advance to Gates
A-E or compatibility testing until this focused gate passes.

## Gate A — context isolation

1. Launch the game and open an existing save.
2. Level a companion and, if available, an animal companion.
3. Confirm no fixed array appears and the log records rejected/non-target
   contexts only.
4. Open a respec UI if installed. Confirm its stat behavior is unchanged.

Any non-new-character activation is a hard failure.

## Gate B — fixed-array entry

1. Return to the main menu and start a genuinely new custom main character.
2. Reach ability scores.
3. Confirm the visible and effective values are exactly:

   ```text
   STR 16, DEX 15, CON 14, INT 12, WIS 10, CHA 8
   ```

4. Confirm creation may continue while roll mode owns completion and no live
   point-buy budget is layered onto the array.
5. Check the UMM panel: one accepted context, an active fixed-array status, and
   a resolved Assembly-CSharp MVID.

A mismatch between displayed values and preview/base values is a hard failure.

## Gate C — rebuild stability

1. Navigate backward and forward across race, class, portrait, and ability
   phases at least three times.
2. Change race between one with no ability modifier and one with a modifier.
3. Confirm the underlying assigned base array remains the same, racial modifiers
   are applied by Kingmaker, and no second array is generated.
4. Confirm diagnostic application count may increase because the preview was
   rebuilt, but the owned array never changes.
5. Cancel back to the main menu, remain there for at least one second, and
   confirm the UMM panel increments the released-session count.
6. Start a second new character and confirm it receives one fresh owned session
   without an "another unit" rejection or values from the canceled character.

## Gate D — point-buy restoration

1. Before completing creation, press **Return active roll session to point buy**
   in the UMM panel.
2. Confirm the rolled values disappear while normal point-buy values and the
   actual configured budget return. Rolled values plus a full budget are a hard
   failure.
3. Use plus/minus controls and confirm cost behavior is normal.
4. Navigate backward and forward and confirm point-buy mode remains active.
5. Repeat with a race that has ability modifiers and confirm modifiers remain
   separate from the restored base allocation.
6. Disable the mod while point-buy mode is active; confirm it does not
   reintroduce rolled values.
7. Repeat with the mod enabled and disable it directly while the fixed array is
   active. It must restore verified point buy before unpatching or refuse to
   disable while retaining recovery hooks.

## Gate E — completion and persistence

1. Re-enable/restart as needed and create a character with the fixed array.
2. Complete character creation, enter the game, and inspect base abilities.
3. Save, quit to desktop, restart, and reload.
4. Confirm the values survive as ordinary base scores with no Dice Roller-owned
   fact, buff, component, or save warning.
5. Disable or uninstall the mod, restart, and reload the save again.

## Compatibility matrix

After vanilla gates pass, repeat B–E with the finalized mod list, then with Bag
of Tricks enabled at its intended settings. Capture logs and screenshots with
`scripts/Collect-RuntimeEvidence.ps1`.

## Acceptance

Runtime qualification requires every vanilla gate. Compatibility qualification
requires the named matrix. A crash, stuck character creator, unintended level-up
activation, changed point-buy budget, reroll on navigation, or save-load
regression blocks advancement to native UI/random rolling.
