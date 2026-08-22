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

## Focused preview-continuity gate

Before repeating the full vanilla matrix, start a genuinely new custom main
character from a fresh process with only Dice Roller enabled:

1. Reach ability scores and confirm the live values are exactly `16, 15, 14,
   12, 10, 8`.
2. In UMM, confirm at least one accepted context and application, zero releases,
   active fixed-session status, and a diagnostic saying the live controller
   state/preview was verified.
3. Confirm no same-owner preview reports `Another unit already owns`.
4. Remain on the screen for ten seconds; confirm values and session remain.
5. Navigate backward and forward once; confirm the same immutable array is
   rebound to the replacement preview.
6. Cancel to the main menu; confirm release increments only after the character
   build ends.

Stop and collect runtime evidence if any step fails. Do not advance to Gates
A-E until this focused gate passes.

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

4. Confirm creation may continue even though normal point-buy points are not
   exhausted.
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
2. Confirm normal point-buy values and the actual configured budget return.
3. Use plus/minus controls and confirm cost behavior is normal.
4. Disable the mod while still in character creation; confirm disable succeeds
   without changing another mod directory.
5. Repeat with the mod enabled and disable it directly while the fixed array is
   active. It must restore point buy before unpatching or refuse to disable.

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
