# User guide

## Getting started

Enable Kingmaker Dice Roller in Unity Mod Manager, start a genuinely new custom
main character, and open the ability-score page. Ordinary Point Buy appears
first. The mod does not roll automatically.

The page initially shows a compact **Roll Stats** tab beneath the Racial Bonus
area when that exact anchor is active, or at a bounded upper-right fallback.
Press it to open the rectangular **Rolled Ability Scores** panel. Opening or
closing the panel never rolls or changes your character. Press **Close** to
remove the full panel and expose every native Skills, Back, and Next control;
only the compact tab remains. The UMM panel is for diagnostics and emergency
recovery, not normal rolling.

## Presets and minimum policies

Open **Roll Options** when you need the low-score or custom-expression
settings. It begins collapsed to keep the Point Buy view simple. Choose a
preset under **Roll method** with the arrow controls:

- `4d6, drop lowest` (default);
- `4d6, reroll ones, drop lowest`;
- `3d6`;
- `2d6 + 6`;
- `1d20`;
- Custom expression.

Choose one player-facing **Low-score rule**:

- **Keep all rolls** keeps every generated score. The Minimum row is hidden.
- **Reroll low scores** rerolls only a score below the minimum.
- **Reroll whole array** discards all six when any score is too
  low.

Generation attempts are bounded. If the chosen requirement cannot be met, the
panel reports an error and preserves your prior verified state.

## Custom expressions

Select Custom expression and open **Roll Options** to enter a bounded dice
expression. The input appears only for that method. Examples:

```text
4d[6]kh3
4d[6]r[1]kh3
2d[6]+6
```

`kh3` keeps the highest three dice and `r[1]` rerolls ones. Parentheses and
ordinary integer arithmetic supported by the parser may be used. Division,
invalid keep counts, unbounded dice counts, overflow, and results outside 1-120
are rejected. Results are never clamped.

## Roll and Reroll

Press **Roll** from Point Buy. The mod first captures your exact current
point-buy state, then generates six base values and enters Roll Mode only after
the live preview and native controls verify.

Press **Reroll** for a new raw array. A reroll does not change the captured
point-buy origin and does not occur during navigation or preview rebuilding.

In Roll Mode, Kingmaker's point-buy plus/minus buttons are disabled. A spendable
point pool cannot be layered onto the array.

## Assigning scores

Each STR, DEX, CON, INT, WIS, and CHA row shows its assigned base value. Use
**Up** and **Down** to move the source position. Equal values remain separate
positions, so duplicate scores can be rearranged safely.

Assignment never rolls again. The native ability page updates immediately.
Race and heritage bonuses remain in Kingmaker's modifier column and are not
baked into the array.

## Summary

The panel displays raw score total, rule/expression, and informational
point-buy equivalent. Scores outside Kingmaker's ordinary 7-18 cost table are
marked `(extended)`. Race modifiers are excluded from both numbers.

## History

The current character-build session keeps the latest 20 generated arrays.
Open **History (n)**, then use **Previous** and **Next** to browse and **Use**
to select an entry. Using an entry restores its array and assignment without
rolling or replacing your point-buy origin. History is discarded when that
character-build owner ends.

## Saved arrays

Open **Saved (n)** to manage up to ten arrays persisted in UMM settings:

- **Store** saves the current raw array and assignment;
- saved Previous/Next browses slots;
- **Recall** applies the selected slot;
- **Delete** removes it.

Recall from Point Buy captures that character's current point-buy origin before
entering Roll Mode. Recall from Roll Mode preserves the existing origin. Saved
slots are global mod settings, not game-save content.

## Return to Point Buy

Press **Return to Point Buy** to restore the exact allocation, remaining
points, total budget, and allocator state captured before the current Roll Mode transition.
The open page should update immediately, and native plus/minus controls should
work at once.

After returning, navigation remains in Point Buy. A later explicit Roll or
Recall captures a new origin. If safe restoration cannot be verified, the mod
fails closed rather than leaving rolled values with a spendable budget.

## Completion and saves

A character completed in Roll Mode stores ordinary Kingmaker base values. It
does not receive Dice Roller facts or components. The save remains valid after
the mod is disabled or uninstalled. History and active session state are not
saved with the character.

## Compatibility status

Bag of Tricks budgets are observed from the live allocator rather than assumed
to be 25. Call of the Wild racial modifiers remain external to raw arrays. Both
still require the documented live compatibility matrix before compatibility is
claimed.

## Troubleshooting

- If the panel is absent, confirm this is a new custom main character and the
  Skills/ability page is active.
- If Roll fails, read the inline error and leave the current state unchanged.
- If Point Buy recovery fails, use the UMM emergency action; do not complete a
  character showing rolled values plus spendable points.
- Check UMM for exact contract/MVID status and the first failed invariant.
- Fully exit Kingmaker before collecting evidence.

```powershell
powershell -NoLogo -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\Collect-RuntimeEvidence.ps1
```

Keep collected logs, screenshots, saves, local paths, and game files out of
Git.
