# 0.1.0-alpha.1 human acceptance

Source/build/package qualification does not replace this live gate. Preserve
logs and screenshots outside Git and collect evidence after testing.

## A. Vanilla product workflow

Start from a fully fresh Kingmaker process with Bag of Tricks and Call of the
Wild disabled.

1. Start a genuinely new custom human and reach ability scores.
2. Confirm ordinary point buy appears initially with the real configured
   budget.
3. Confirm one native **Rolled Ability Scores** panel appears.
4. Remain on the page for ten seconds. Confirm no array is generated and no
   points or values change automatically.
5. Press **Roll** once. Confirm exactly one six-score array appears, point buy
   is unavailable, and all native plus/minus controls are disabled.
6. Confirm the panel shows base assignments, rule, total, and point-buy
   equivalent.
7. Press **Reroll**. Confirm a newly generated array replaces the current one,
   no point budget appears, and history increments by one.
8. Move values Up and Down. Include an array with duplicate values when
   practical and confirm each source position moves independently.
9. Navigate backward and forward. Confirm the current array and assignment
   return unchanged and history does not grow.
10. Change race. Confirm Kingmaker's racial modifiers remain separate from the
    panel's base values and are never accumulated into a reroll.
11. Select Previous/Next history, Use an earlier entry, and confirm this does
    not generate another roll.
12. Store the current array, browse saved slots, Recall it, and confirm values
    and assignment are restored without changing the point-buy origin.

## B. Exact Point Buy origin

1. In ordinary Point Buy, spend several points and note allocation, remaining
   points, and total budget.
2. Press Roll.
3. Press **Point Buy** in the native panel without navigating away.
4. Confirm the exact pre-roll allocation and remaining/total budget appear on
   the same page immediately.
5. Confirm native plus/minus controls work immediately.
6. Navigate backward and forward. Confirm Point Buy remains active and no roll
   returns.
7. Modify the allocation again, enter Roll Mode a second time, and return.
8. Confirm the second, newly captured point-buy origin is restored.

Any rolled values combined with spendable allocator points is a hard failure.

## C. Rules and error handling

Test each preset:

```text
4d6, drop lowest
4d6, reroll ones, drop lowest
3d6
2d6 + 6
1d20
```

Then test one valid custom expression and one invalid expression, for example:

```text
valid:   4d[6]r[1]kh3
invalid: 4d[
```

Test tabletop, individual-minimum, and whole-array-minimum policies. An invalid
command must show an inline error and preserve the prior verified state.

## D. Saved-array persistence

1. Store an assigned array.
2. Quit fully to desktop.
3. Restart, begin a new custom character, and remain in Point Buy.
4. Recall the saved slot. Confirm this explicit command captures the current
   point-buy origin and enters Roll Mode.
5. Confirm raw values and assignment permutation.
6. Delete the slot, restart again, and confirm it remains deleted.

## E. Completion, save independence, disable

1. Complete a character in Roll Mode.
2. Enter the game and inspect base ability values.
3. Save and quit fully to desktop.
4. Disable or uninstall Dice Roller, restart, and reload.
5. Confirm identical ordinary values and no missing-content warning.
6. In a separate fresh character, disable Dice Roller while Roll Mode is
   active on the ability page.
7. Confirm exact point buy appears immediately before/as disable succeeds. If
   safe restoration cannot be proven, disable must be refused.

## F. Context isolation

Confirm neither panel nor roll activation appears in every available path:

- ordinary existing-character level-up;
- companion level-up;
- animal companion level-up;
- mercenary creation;
- pregen selection;
- respec.

Any non-new-main-character activation is a hard failure.

## G. Compatibility matrix

Only after vanilla passes, repeat fresh-process entry, Roll/Reroll,
reassignment, race change, and exact Point Buy restoration with:

```text
Dice Roller + Call of the Wild
Dice Roller + Bag of Tricks
Dice Roller + Call of the Wild + Bag of Tricks
final intended mod list
```

For Bag of Tricks, record its actual configured point-buy budget and prove that
the exact allocation and budget return. For Call of the Wild, exercise an added
race/heritage and confirm all modifiers remain separate.

## Evidence

After the attempt, fully exit and run:

```powershell
Set-Location 'C:\Dev\KingmakerDiceRollerLab\repo\KingmakerDiceRoller'

powershell -NoLogo -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\Collect-RuntimeEvidence.ps1
```

Report the first failed invariant and preserve evidence outside Git. Do not
call the alpha runtime-qualified until all vanilla sections pass. Do not call
it compatibility-qualified until the named matrix passes.
