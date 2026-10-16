# User guide

## Getting started

Enable **Mr. Bard's Kingmaker Roll For Stats UI** (UMM ID `KingmakerDiceRoller`;
published as Kingmaker Dice Roller before 0.1.10) in Unity Mod Manager, then
either start a genuinely new custom campaign main character, recruit a
mercenary, or select an eligible native/Eddic respec that reopens all six
starting scores. On the ability page, ordinary Point Buy appears first. The mod
does not roll automatically.

The page initially shows a compact **Roll Stats** tab at the safe bottom center
of the ability region, above Back/Next navigation. An active Racial Bonus
container supplies preferred horizontal geometry; if it is absent or inactive,
the allocator frame, allocator region, or ability-page root supplies the same
bounded bottom-center placement. There is no upper-right fallback. Press the tab
to open the responsive right-side **Rolled Ability Scores** drawer.
Wide layouts show ordinary controls directly and keep the six-score Roll
workflow visible without scrolling. Constrained layouts use Compact
disclosures and scroll only when content actually overflows. Opening, closing,
or switching profile never rolls or changes your character. Press **Close** to
remove the full panel and expose native Skills, Back, and Next controls; only
the compact tab remains. Rolling happens on the ability page, not in Unity
Mod Manager; see [Unity Mod Manager settings panel](#unity-mod-manager-settings-panel).

## Presets and minimum policies

In Wide, low-score and custom-expression settings are visible directly when
relevant. In Compact, open **Roll Options**; it begins collapsed. Choose a
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

Select Custom expression to enter a bounded dice expression. In Compact, first
open **Roll Options**. The input appears only for that method. Examples:

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

The panel displays raw score total, the actual rule/expression used to create
the active array under **Rolled with**, and informational point-buy equivalent.
The selected preset may differ after a roll. Scores outside Kingmaker's
ordinary 7-18 cost table are marked `(extended)`. Race modifiers are excluded
from both numbers.

## History

The current character-build session keeps the latest 20 generated arrays. Wide
shows the current History record directly; in Compact, open **History (n)**.
Use **Previous**, **Next**, and **Use**. Using an entry restores its array and
assignment without rolling or replacing your point-buy origin. History is
discarded when that main-character or mercenary build owner ends.

## Saved arrays

Wide shows the current Saved record when relevant; in Compact, open
**Saved (n)**. Manage up to ten arrays persisted in UMM settings:

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

Mercenary budgets are observed exactly like main-character budgets. Vanilla's
commonly observed 20-point mercenary allocator is not hard-coded, so a compatible
modded budget and partial allocation should restore exactly.

## Completion and saves

A character completed in Roll Mode stores ordinary Kingmaker base values. For a
mercenary, the mod does not equate a matching preview with completion: it feeds
the verified base assignment to the exact stable custom-companion descriptor
after native action replay and verifies that descriptor after the success
callback. Racial modifiers remain separate. A final mismatch is an error, not a
successful commit claim. The character receives no facts or components from
this mod; History and active session state are not saved with the character.

## Unity Mod Manager settings panel

The mod's Unity Mod Manager panel is short. Unity Mod Manager already shows
the name and version, so the panel itself shows:

- one sentence on where roll controls appear;
- one status line. **Ready** means the game contracts resolved and the hooks
  are installed. It is normal at the main menu, outside character creation
  and during ordinary level-ups. During a supported build the line says
  whether the new character, mercenary or respec can use Roll Stats or is
  using rolled scores;
- brief notices only when something needs attention: the mod could not start,
  rolled scores could not be applied, Return to Point Buy did not complete,
  a finished character may not have kept its rolled scores, or a detected mod
  such as Bag of Tricks has unverified compatibility. Detecting a mod is not
  a conflict;
- **Return to Point Buy**, only while rolled scores are in use or a roll or
  restore did not finish. It does the same as the ability page's control. It
  restores the point-buy scores you had before rolling. If it is blocked, the
  panel says why instead of hiding it;
- **Show troubleshooting**, collapsed by default. It holds the **Verbose
  diagnostic logging** toggle, workflow and session facts, history and saved
  counts, context and finalization counters, contract/MVID status, detected
  mods, the full compatibility explanation and recent timestamped diagnostics
  (while verbose logging is on). Opening it changes nothing in your game or
  settings.

## Compatibility status

Bag of Tricks budgets are observed from the live allocator rather than assumed
to be 25. Call of the Wild racial modifiers remain external to raw arrays. Both
still require the documented live compatibility matrix before compatibility is
claimed.

## Troubleshooting

- If the panel is absent, confirm the starting-ability allocator is active.
  Native/Eddic respecs require a player-selected full starting-score rebuild.
  Recruitment-level story retraining, later ability increases, pregens, pets,
  enemies, locked starting scores, and unrecognized provider paths stay ineligible.
  The 0.1.3 release has owner confirmation for the tested workflow; the detailed
  compatibility matrix remains open. newman55 Respecialization is not integrated.
- If Roll fails, read the inline error and leave the current state unchanged.
- If Point Buy recovery fails, use **Return to Point Buy** in the Unity Mod
  Manager settings panel; do not complete a character showing rolled values
  plus spendable points.
- Open **Show troubleshooting** in the settings panel for exact contract/MVID
  status, recent diagnostics and the first failed invariant.
- Fully exit Kingmaker before collecting evidence.

```powershell
powershell -NoLogo -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\Collect-RuntimeEvidence.ps1
```

Keep collected logs, screenshots, saves, local paths, and game files out of
Git.
