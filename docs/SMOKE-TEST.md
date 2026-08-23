# 0.1.0 human acceptance

Source, exact-contract, build, package, and installation qualification do not
replace this live gate. Preserve logs/screenshots outside Git and collect
evidence only after fully exiting Kingmaker.

## Resolution matrix

Run 1600 by 900 first and record effective parent/Canvas geometry from UMM
diagnostics. Then repeat focused layout/collapse checks at:

```text
1920 x 1080
1536 x  960
1366 x  768
1280 x  720
```

Test both **NEW CHARACTER** and normal **NEW MERCENARY** screens. Visible title
text is only a human landmark; production classification does not read it.

## A. New-main-character regression (primary 1600 by 900)

1. Start a fresh custom campaign character and reach ability allocation.
2. Confirm ordinary Point Buy and one compact **Roll Stats** tab appear. Wait
   ten seconds; no array, value, or point total may change automatically.
3. Confirm the tab does not cover racial controls and Back/Next work while
   collapsed.
4. Open the drawer. Confirm Wide is logged, the 38-unit header is one compact
   row, Close is normally sized, and the drawer stays within parchment bounds.
5. In Point Buy, confirm roll method and low-score rule are visible without
   **Roll Options**. Select a minimum policy and Custom in turn; only applicable
   Minimum and custom-expression rows appear.
6. Roll once. Confirm exactly six ordered assignment rows, Reroll, Return to
   Point Buy, total, point-buy equivalent, `Rolled with`, current History,
   current Saved controls, and status are visible without wheel input.
7. Change the selected preset after rolling. Confirm the selector changes while
   `Rolled with` continues to report the rule that created the active array.
8. Reroll, move assignments (including duplicate values when practical), browse
   and Use History, Store/Recall/Delete Saved, and confirm no navigation or
   presentation action generates an extra roll.
9. Change race and class; navigate away/back. Confirm one session/panel, the same
   array/assignment, separate racial modifiers, and preserved open/closed choice.
10. Return to Point Buy and confirm the exact main-character allocation,
    remaining/total points, preview values, and native controls immediately.
11. Close. Confirm the complete expanded background/content/raycast footprint
    disappears and native Skills, Back, and Next receive clicks.
12. Complete a separate rolled character. Confirm selected ordinary base values,
    correct separate racial modifiers, and complete session/UI cleanup.

## B. Mercenary entry

From an established campaign, use the normal player recruitment interaction:

1. Reach the mercenary ability allocator.
2. Confirm one compact **Roll Stats** tab appears, no array is generated, and
   vanilla Point Buy remains available before an explicit command.
3. Confirm the tab does not cover racial controls and Back/Next work collapsed.
4. Open it. Confirm the same responsive Wide presentation and geometry as the
   main-character screen; no technical mercenary enum/diagnostic text appears.
5. Confirm context diagnostics report `creationKind=Mercenary`, `CharGen`, first
   level, player faction, non-main/non-pet/non-enemy, different resolved campaign
   main, matching controller state/preview, and the exact two-part custom-
   companion discriminator.

Absence of the tab or acceptance without exact discriminator facts is a hard
failure.

## C. Mercenary Point Buy and Roll

1. Record the untouched six base values, remaining points, total points, and
   allocator availability. Vanilla is expected to show 20, but use the observed
   value as evidence.
2. Roll once. Confirm one array is applied, native plus/minus controls are
   disabled, all six rows and ordinary sections fit without wheel input, and the
   point-buy-equivalent display does not masquerade as allocator budget.
3. Return to Point Buy. Confirm exact recorded values, remaining/total points,
   allocator availability, preview display, and native controls. No 25-point
   main-character budget may appear.
4. Spend several points, record the partial state, Roll, and Return. Confirm the
   exact partial allocation and remaining/total points.
5. From Point Buy, Roll and Reroll repeatedly, then Return. Confirm Reroll never
   overwrote the original origin.
6. From Point Buy, Recall a saved array, then Return. Confirm Recall captured and
   restored that mercenary's exact current origin.
7. If a supported point-budget mod is available, repeat with a nonstandard
   mercenary budget and prove the actual observed budget returns.

Any rolled values combined with spendable allocator points is a hard failure.

## D. Mercenary lifecycle

1. In Roll Mode, Reroll and exercise duplicate-score assignment.
2. Change race/class and navigate away/back. Confirm preview/state/allocator
   replacement causes no surprise reroll, duplicate panel, or new session.
3. Collapse/reopen and confirm the current assignment remains.
4. Cancel recruitment. Confirm panel and session disappear.
5. Reopen normal recruitment. Confirm a fresh collapsed session with no active
   array or point-buy origin inherited from the canceled candidate; intentionally
   persistent Saved arrays remain available.
6. Recruit a second mercenary and confirm it does not inherit the first
   mercenary's active assignment, History, or origin.
7. Move between new-main-character and mercenary creation when reproducible;
   confirm their session owners never rebind to each other.

## E. Mercenary completion and save independence

1. Complete recruitment in Roll Mode.
2. Confirm the hired mercenary has the selected ordinary base values and racial
   modifiers remain correct/separate.
3. Confirm campaign main-character and other companion scores are unchanged.
4. Confirm hiring price/payment behavior is vanilla.
5. Confirm cleanup after completion and no Dice Roller fact, buff, component,
   unit part, blueprint, or persistent marker exists on the mercenary.
6. Save, quit, disable/uninstall Dice Roller, reload, and confirm ordinary values
   remain with no missing-content warning.

## F. Rules, errors, History, and Saved

In each supported context, exercise:

```text
4d6, drop lowest
4d6, reroll ones, drop lowest
3d6
2d6 + 6
1d20
valid Custom:   4d[6]r[1]kh3
invalid Custom: 4d[
```

Test Keep all rolls, Reroll low scores, and Reroll whole array. Invalid commands
must report an inline/fixed-footer error and preserve prior verified state.
History remains current-build scoped. Saved arrays persist through a full
process restart and Recall from Point Buy captures the current owner's origin.

## G. Unsupported-context regression

Confirm no **Roll Stats** tab appears during:

- ordinary campaign-main level-up;
- ordinary companion progression;
- respecialization;
- pregenerated-character selection;
- animal companion or pet creation;
- enemy/NPC construction or unrelated first-level controller paths when
  reproducible;
- unknown build mode;
- a different player unit lacking the exact custom-companion discriminator.

Any activation in these paths is a hard failure.

## H. Compact fallback and raycasts

At constrained effective dimensions on both supported screens:

1. Confirm Compact activates predictably without profile/scroll flicker.
2. Confirm fixed header and Close remain visible.
3. Confirm Roll Options, History, and Saved disclosures work.
4. Confirm vertical scrolling and narrow scrollbar appear only for measured
   overflow; fitting content ignores wheel input and no horizontal scroll exists.
5. Confirm the masked body clips every child.
6. Collapse and prove racial controls, Skills, Back, and Next are accessible.

## I. Focused compatibility

After vanilla passes, repeat supported entry, Roll/Reroll, race change, exact
Point Buy restoration, collapse, completion, and unsupported-context isolation
with supported versions of:

```text
Dice Roller + Bag of Tricks
Dice Roller + Call of the Wild
Dice Roller + both
the intended final mod list
```

Record Bag of Tricks' actual configured main and mercenary budgets. With Call
of the Wild, test an added race/heritage and keep modifiers separate. This is a
focused matrix, not an exhaustive compatibility claim.

## Evidence

After each attempt, fully exit and run:

```powershell
powershell -NoLogo -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\Collect-RuntimeEvidence.ps1
```

Preserve evidence outside Git. Report the first failed invariant. Do not mark
the exact installed `0.1.0` artifact runtime-qualified until all vanilla
sections pass; do not mark it compatibility-qualified until the named focused
matrix passes.
