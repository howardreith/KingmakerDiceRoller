# Candidate human acceptance

Source, exact-contract, build, package, and installation qualification do not
replace this live gate. Preserve logs/screenshots outside Git and collect
evidence only after fully exiting Kingmaker.

## 0.1.14 v3 native-stat presentation (NOT RUN)

Install the exact 0.1.14 candidate from
`docs/NATIVE-STAT-UI-V3-CANDIDATE.md`. Record commit, ZIP/DLL SHA-256,
game version, 1920×1200 resolution and canvas scale; repeat the visual cases
at a smaller 16:9 layout. Capture the whole ability page, including the Human
racial selector and global bottom navigation. Keep the owner's v2 screenshots
as before evidence, not 0.1.14 after evidence.

1. Begin with partially spent Point Buy on Human. While a native cost hint is
   visible, open Roll for Stats. Check that the six base scores, budget, and
   history are unchanged; the full Value caption is over the original circle
   with a dash centered in it. Hover and keyboard-navigate every former native
   purchase/refund target, including after FillData and a race/preview refresh.
   No cost text, container, tooltip, focus ghost, or purchase/refund may appear.
2. Inspect the centered 400–440-unit formula group and visible native down
   chevron in normal/hover/pressed/focus states. Open its popup and use all six
   visible choices without scrolling. Paste a long supported draft, then an
   invalid one; opening/choosing/editing does not roll, spend, or append history.
3. Use legitimate fixtures yielding totals 6, 77, 120, and 720. Capture each
   full ability badge beside native comparison text. After Roll, Previous/Next,
   and saved Load, verify total placement and base-sum semantics with Human's
   racial destination changed. Swap each enabled adjacent position and confirm
   one exchange per click, downward centered icons after refresh, and the
   disabled Charisma endpoint.
4. Inspect a swap tooltip and the long Return tooltip at screen edges. Check
   text wrapping, complete paper/frame/scroll end, flip/clamp, pointer exit,
   keyboard selection, popup/workspace closure, owner replacement, and scale
   change. The overlay must not block neighboring clicks or survive its trigger.
5. Verify Previous, Next, and Return on one 34-unit row, with Load/Save/Roll
   above. In Roll-ready and applied Roll, the Human heading, selected ability,
   both racial arrows, and bottom navigation must remain fully separated and
   usable. Repeat with a fixed-bonus race, a nondefault policy note, a visible
   validation/operation message, page rebuild, and smaller layout.
6. Return from Roll-ready and applied Roll. Confirm the native localized Points
   caption, current remaining budget, and legal purchase/refund costs update.
   Repeat after an earlier Roll/Return with further purchases, then exercise
   new main, mercenary, editable respec and owner/allocator replacement. No
   cost suppression or overlay remains on another page or character.

Record PASS/FAIL/NOT RUN per V3-01 through V3-06 in the candidate evidence.

## 0.1.13 revised native-stat candidate (NOT RUN)

Record the candidate commit, ZIP SHA-256, game version, resolution, and UI
scale. Capture actual ordinary Point Buy, Roll-ready, applied Roll, all-six-row
preset popup, and an invalid formula at 1920×1200 and a smaller 16:9 layout.
The owner-provided 0.1.12 screenshots and mission SVGs are comparison material,
not after captures.

1. On a new supported character, verify only Roll for Stats is added; spend and
   refund points, then open Roll-ready. Confirm no score/budget/RNG change,
   Value dash, hidden and non-interactable original controls, and view-only
   Return. Repeat after an earlier roll/return and further Point Buy purchases.
2. Roll [or load] a known array; confirm Value equals its six base scores,
   including with a racial adjustment. Swap with one right-hand downward arrow
   and confirm centered hover/pressed icon, one dispatch/click sound, disabled
   Charisma, and unchanged Value. Confirm native skills badge still works.
3. Open presets and reach all six rows without scrolling. Select Custom,
   enter invalid text, verify it remains and blocks only generation. Test Enter,
   Escape, popup dismissal, and keyboard focus without triggering game Next.
4. Roll A/B/C, navigate to A and back, then generate D from A and verify
   preserved forward history. Save an arrangement, browse Load without effect,
   recall it once, navigate back/forward, and return to the exact observed
   Point Buy allocation and budget.
5. Rebuild the same owner/allocator and switch characters, phases, and
   supported main, mercenary, and editable-respec contexts. Check native
   controls/listeners restore once and no stale popup, arrows, or badge remain.
   Exercise UMM low-score settings and active-policy note. Confirm unsupported
   later level-ups and locked-base retraining have no roll entry.

Mark each actual game observation PASS/FAIL/NOT RUN in the candidate evidence.

## 0.1.12 inline native-stat candidate (historical, NOT RUN)

Use the exact packaged candidate and record its commit, ZIP hash, game version,
screen resolution, canvas scale, and allocator/toolbar/racial-selector rects.
Check 1920×1200, 1920×1080, 1600×900, 1366×768, 1280×720, and 1152×720.
At each size capture the normal Point Buy view, Roll view, and the tallest
open Method/Options/Load menu. The six native scores, racial modifiers,
descriptions, toolbar captions, racial selector, and Back/Next must remain
visible, readable, and independently clickable.

1. On a new main character, confirm the two-row strip appears directly after
   native score rows in Point Buy. No roll occurs on attach, FillData, menu
   open, or preview rebuild. Original point-buy arrows buy/refund normally.
2. Partially spend points; choose a method directly; Roll. Confirm six native
   rows remain, original buy buttons are disabled, owned vertical arrows swap
   only adjacent base-score source positions, and STR up/CHA down are disabled.
   Check equal values, a racial bonus, Intelligence, and a casting score.
   One pointer click or keyboard Submit performs one swap and one click sound.
3. Reroll, Save the rearranged assignment, open Load, browse History and Saved
   without changing the character, then explicitly Use/Recall. Confirm exact
   saved assignment and no RNG. Return to Point Buy, edit allocation, Use a
   history or saved record directly from Point Buy, then Return again to the
   newly captured allocation and observed remaining/total budget.
4. In either mode, open Options. Check low-score policy, conditional minimum,
   custom-expression entry/help, and invalid-expression error. In Details,
   check total, informational point-buy equivalent and extended marker, and
   the applied rule after selecting a different next-roll method.
5. Repeat FillData, phase exit/reentry, same-owner preview replacement,
   allocator replacement, cancel, completion, disable/unload, and a different
   character owner. Check one control/listener set, no stale callback routing,
   no hidden focus or click blocker, native point-buy availability after
   return, and no implicit reroll.
6. Repeat the positive path for player-initiated mercenary recruitment and
   qualified six-score respec. Confirm ordinary later level-up, catch-up
   levels, locked-base retraining, pets, and unrelated units have no inline
   controls. Verify native completion and save/reload using disposable state.
7. If inline attachment fails, record the contract/layout diagnostic and
   verify the prior Roll Stats drawer is usable without score mutation. A
   fallback on the intended supported PC screen is a failed visual scenario.

Runtime screenshot and interaction results are currently NOT RUN. The
computer-use helper in this session exited unexpectedly twice before it could
list windows, so no screen state or game input was observed.

## Legacy 0.1.11 drawer checks (fallback only)

## Resolution matrix

Run the mercenary screen at 1152 by 720 first and record effective
parent/Canvas geometry from UMM diagnostics. Then repeat focused
layout/collapse checks at:

```text
1280 x  720
1366 x  768
1600 x  900
1920 x 1080
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
3. Confirm the tab is horizontally centered within the ability/allocator region,
   above bottom navigation, fully visible/clickable, and clear of ability arrows,
   racial controls, skill controls, Back, and Next. An upper-right placement is
   a hard failure.
4. Open it. Confirm the same responsive Wide presentation and geometry as the
   main-character screen; no technical mercenary enum/diagnostic text appears.
5. Confirm context diagnostics report `creationKind=Mercenary`, `CharGen`, first
   level, player faction, non-main/non-pet/non-enemy, different resolved campaign
   main, matching controller state/preview, and the exact two-part custom-
   companion discriminator.

Absence of the tab or acceptance without exact discriminator facts is a hard
failure.

## C. Mercenary Point Buy and Roll

1. Record separately the untouched six base values, racial modifiers, displayed
   totals, `StatsDistribution.StatValues`, preview-descriptor base values,
   remaining points, total points, and allocator availability. Vanilla may show
   20 points, but use the observed value as evidence.
2. Roll a deliberately distinctive array. Record the assigned base values,
   racial modifiers, displayed totals, distribution values, and current preview
   descriptor base values as distinct fields. Confirm native plus/minus controls
   are disabled and the point-buy-equivalent display does not masquerade as
   allocator budget.
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

1. Before pressing the final native action, record the exact
   `LevelUpController`, `Unit`, `Preview`, `State`, `LevelUpState.Unit`,
   distribution identities, and distinctive expected base values.
2. Complete the entire recruitment workflow in Roll Mode. A closing UI or
   matching preview is not proof of persistence.
3. Require one final diagnostic record with `creationKind=Mercenary`, exact
   controller/source/preview/final identities, expected and observed six-value
   base arrays, and `passed=true`. Any missing record or `FINAL FAIL` is a hard
   failure.
4. Inspect the actual hired party unit, not the creation preview. Record its six
   base values, racial modifiers, and displayed totals separately. The base
   values must equal the distinctive assignment and modifiers must remain
   native/separate.
5. Confirm campaign main-character and every other companion remain unchanged,
   and hiring price/payment behavior is vanilla.
6. Confirm cleanup after completion and no Dice Roller fact, buff, component,
   unit part, blueprint, persistent marker, or surviving transient session exists
   on the mercenary.
7. Save, exit the process, restart, and reload. Inspect that same party unit and
   record reloaded base values, modifiers, and displayed totals. The six base
   values must still equal the assignment.
8. When safe, repeat reload with Dice Roller disabled/uninstalled and confirm no
   missing-content warning. Ordinary Kingmaker character data must be sufficient.

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
must report an measured inline error and preserve prior verified state.
History remains current-build scoped. Saved arrays persist through a full
process restart and Recall from Point Buy captures the current owner's origin.

## G. Unsupported-context regression

Confirm no **Roll Stats** tab appears during:

- ordinary campaign-main level-up;
- ordinary companion progression;
- retraining that locks/preserves starting scores;
- pregenerated-character selection;
- animal companion or pet creation;
- enemy/NPC construction or unrelated first-level controller paths when
  reproducible;
- unknown build mode;
- a different player unit lacking the exact custom-companion discriminator.

Any activation in these paths is a hard failure.

## H. Compact placement and raycasts

At constrained effective dimensions on both supported screens:

1. Confirm Compact activates predictably without profile/scroll flicker.
2. Confirm fixed header and Close remain visible.
3. Confirm Roll Options, History, and Saved disclosures work.
4. Confirm vertical scrolling and narrow scrollbar appear only for measured
   overflow; fitting content ignores wheel input and no horizontal scroll exists.
5. Confirm the masked body clips every child.
6. Collapse and prove the access tab remains bottom-centered even when the
   racial-bonus container is absent, inactive, or rebuilt. Prove racial controls,
   Skills, Back, and Next are accessible and no full-screen raycast exists.

## I. Focused compatibility

After vanilla passes, repeat supported entry, Roll/Reroll, race change, exact
Point Buy restoration, collapse, completion, final party-unit inspection, and
save/exit/restart/reload with supported versions of:

```text
Dice Roller + Bag of Tricks
Dice Roller + Call of the Wild
Dice Roller + both
the intended final mod list
```

For Bag of Tricks, record the actual live mercenary budget and original
allocation, prove Return to Point Buy restores both, and record the final and
reloaded rolled base values with native racial modifiers. Do not infer a broad
compatibility claim from that focused case. With Call of the Wild, test an added
race/heritage and keep modifiers separate.

## Evidence

After each attempt, fully exit and run:

```powershell
powershell -NoLogo -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\Collect-RuntimeEvidence.ps1
```

Preserve evidence outside Git. Include a screenshot of corrected collapsed-tab
placement at the constrained mercenary gate, but never use a screenshot as
proof of persistence. Report the first failed invariant. Do not mark the exact
repair artifact runtime-qualified until all vanilla sections pass; do not mark
it compatibility-qualified until the named focused matrix passes.

## J. Guarded 0.1.3 respec matrix (NOT RUN)

DATA's current machine policy does not authorize game/Steam launch, desktop
input, Cloud changes, profile changes, or valued-save use. There is no nominated
disposable fixture in this mission. No fixture name or prior runtime token is
authorization. All steps below are a reproducible remaining procedure, not run
results.

1. Obtain explicit authorization for one owner-nominated disposable campaign,
   exact fixture/save paths, a guarded launch, and each temporary mod profile.
   Any Cloud isolation requires its own approved action and recorded original
   setting. Keep valued saves out of scope; do not select Continue arbitrarily.
2. With Kingmaker stopped, inventory and back up the authorized save/profile
   scope outside it; verify hashes/readability and reject reparse points or stale
   deployment state. Record all active UMM IDs/settings. Validate the 0.1.3 package
   and run `Install.ps1 -WhatIf -PackagePath <candidate>`; after install approval,
   use that exact package and verify installed DLL parity. Save authorization
   does not follow merely from installation.
3. Use a fresh process for each approved profile. Run Dice-only new-main and
   mercenary sections A-E first. Then native respec with Eddic/newman55 disabled:
   main and mercenary full rebuilds, plus story-companion locked-base negative
   control. A level-4 single-point increase is not a starting-score allocator.
4. In a separate fresh Eddic 1.0 profile, run main, mercenary, and story companion
   through the selector, then representative NPC entry with Eddic enabled. Add
   a remote companion when the disposable fixture supports it. Preserve story
   identity/locked phases; do not change race/portrait simply to expose the tab.
5. For each positive core path: confirm one clickable bottom-center Roll Stats
   tab, PointBuy-first entry, no auto RNG, Roll/Reroll/reassignment, back/next and
   one genuine preview rebuild, then complete. Use a fixed-racial case and a
   floating bonus case. Confirm native Skills and navigation stay clickable when
   collapsed and native spending is disabled only in the owned Roll allocator.
6. Across representative cases, use History/Recall, return to the exact partial
   Point Buy origin with its actual remaining/total budget, cancel before and
   after rolling, respec again, and switch characters. Include a character whose
   previous starting scores were rolled. Preserve identities and permanent
   modifiers; compare disputed provider cancellation/retention behavior with the
   same fixture and profile with Dice disabled.
7. Record the six assigned starting scores, native racial/permanent modifiers,
   live preview/distribution, source after first-level action replay, original
   recipient after callback, and final party/remote descriptor separately.
   Require one copy-observed final diagnostic, then inspect the real character.
   A PASS log alone is insufficient. Capture concise local screenshots of the
   active/collapsed panel and values; retain evidence outside Git.
8. Use a leveled fixture crossing at least levels 4 and 8. Verify native ability
   increases persist through catch-up and a subsequent ordinary level-up, with
   no reopened rolling panel or initial-array rewrite. Save to the authorized
   disposable destination, exit, restart and reload. When separately safe,
   repeat reload without Dice Roller and verify ordinary persisted stats.
9. Run a focused nondefault Bag of Tricks budget and CotW race/class case, then
   the intended full profile in fresh processes. Simultaneous respec engines
   are a separate conflict lane, not a prerequisite for individual coverage.
10. newman55 is blocked until its exact DLL/version is supplied. First establish
    launch modes, Original-score behavior, disabled legacy Light behavior, and
    the actual replacement mercenary/callback. Implement/verify that adapter
    before running its main/mercenary/story matrix; do not reinterpret the native
    adapter's tests as newman55 evidence.
11. After each approved run, exit normally, collect evidence, restore authorized
    profile/Cloud changes transactionally, and reverify protected manifests.
    Separate any provider baseline defect from Dice-added effects. Report the
    first failed invariant and the exact candidate hash.

## K. Rolled-stat build-integrity matrix (NOT RUN)

Guarded exactly like section J; same authorization requirements. These lanes
verify the shared derived-allowance refresh and the pre-replay commit
integration from `docs/CHARACTER-BUILD-INTEGRITY.md`.

1. New-main noncaster: roll, then on the Skills page confirm the skill-point
   counter equals the native allowance for the rolled Intelligence (compare
   with an identical point-buy allocation as control when representable).
   Allocate all ranks, navigate Back/Next, and complete; the final character's
   ranks must match the allocation actually made.
2. New-main/mercenary qualifying Intelligence caster: roll with a high casting
   score, select every offered starting spell including the final bonus-slot
   picks, complete, and verify each selected spell identity exists in the
   final spellbook. Any FINAL FAIL record naming dropped actions is a defect.
3. Intelligence decrease after allocation: roll an INT-lowering array after
   spending ranks, confirm the allowance display follows the new score, and
   that native gating (not silent truncation) resolves any excess before
   completion.
4. Feat prerequisites across ability scores (for example an INT-13 or DEX-15
   prerequisite feat): confirm eligibility tracks the rolled score, selected
   results persist, and invalid choices are corrected through native UI.
5. Return to Point Buy after rolling: confirm the allowance returns to the
   point-buy Intelligence, then change race/class (one genuine preview
   rebuild) and confirm the restored point-buy allocation survives the
   rebuild with its exact remaining/total budget.
6. Mercenary completion: watch for the one-line mercenary final verification
   record; it must be a PASS with the expected/observed base arrays equal and
   no dropped-action failure. Repeat with a reroll and a history recall.
7. The previously reported trait-provider scenario: confirm the feat/trait
   page populates after rolling and valid selections persist; if it still
   fails, capture the provider's own log and report it as a separate
   provider defect with reproduction steps.
8. Save/exit/reload and reload-without-Dice-Roller lanes from section J
   remain the persistence gates for this matrix.

## L. Skills badge and forward-navigation matrix (NOT RUN)

Guarded exactly like section J; same authorization requirements. These lanes
verify the skills-page synchronization and the SetPhase veto.

1. Owner reproduction: recall/assign Intelligence 10, spend the whole budget
   until the badge reads 0, reopen the drawer, apply an Intelligence 16 array,
   close the drawer, and touch nothing else. The badge must already show the
   true remaining amount; immediate Next must stay on Skills with the native
   attention marks and a spend/remove reason.
2. Spend the newly available points through normal controls: the badge and
   Next update immediately, and the following feats/traits page is populated
   and functional; complete the character and verify ranks/feats/spells.
3. Reverse: spend the larger budget, then apply a lower-Intelligence array.
   Overspending must be visible (negative remainder), Next must be blocked,
   and removing the excess through normal controls must restore progression.
4. Repeated open/close without changes, multiple changes on one preview,
   unchanged-budget score changes, and Close/Next in immediate succession: no
   stale state, duplicate grants, extra RNG, or refresh loop.
5. Try every supported forward route (mouse, keyboard submit, gamepad, any
   visible later-phase control): none may bypass the veto while invalid. Back
   and correction controls must remain usable throughout.
6. Reassignment, History, Recall, partial Return to Point Buy, and Back/Next
   across race/class changes: counters and navigation stay coherent with the
   current owner and allocation, including the respected native edge case of
   unspent points with every skill rank at its cap.


## Native book UI candidate matrix (all live lanes NOT RUN)

The reskin mission authorizes offline work only. A desktop connection, existing
game process or old release permission is not authority to launch, install or
control a game. Obtain an explicitly authorized guarded workflow with a disposable
fixture before this matrix. Do not use valued saves, change Cloud/global profiles,
or install into the active game as part of offline qualification.

Pin the exact clean commit, version, ZIP/DLL SHA-256 and game MVID from the
candidate manifest. Every capture/recording must identify that DLL. Capture real
before/after game frames and a short recording with audio; supplied owner stills,
resource extracts or composited mockups cannot establish candidate acceptance.

| Scenario | Required observation | Candidate status |
| --- | --- | --- |
| Fresh launch -> new-character Skills | Native paper, all font roles and frames before visiting other menus; no fallback diagnostic | NOT RUN |
| Collapsed/open/close | Bottom-center tab above Back/Next; held press; smaller right paper; full collapse; focus restored, no stuck state/click-through | NOT RUN |
| Every control class | Normal, hover, held and disabled states; cancel by dragging away; one command on accepted release; Up/Down means assignment | NOT RUN |
| Audio and Submit | One click on pointer/keyboard/controller activation as supported; compare native Back; master AudioLevel slider/mute respected; disabled/canceled controls silent; no double playback | NOT RUN |
| Point Buy and Roll | Every existing control readable/reachable; ordinary Wide workflow comfortably fits; actual applied rule distinct from selected preset | NOT RUN |
| History/Saved/custom/long values | Disclosures, two Saved action rows, field focus/caret/selection, long selector wrap, scrollbar and score 120; errors untruncated and scrollable; no empty success strip | NOT RUN |
| Skill counter and forward guard | Allocate skills, change INT in both directions using existing operations, Close, verify badge/rows/remaining points and Next/later-phase veto; Back still works | NOT RUN |
| Owner and phase lifecycle | Open/close repeatedly; Back/Next/rebuild; cancel/new owner; no duplicate roots, material/listener/emitter growth or orphan hit targets | NOT RUN |
| Mercenary and supported respec | Same supported entry points; point-buy origin/budget and preview continuity intact; unsupported later/locked contexts still rejected | NOT RUN |
| Theme failure | In authorized fixture only, unavailable donor yields bounded diagnostic and usable fallback; mode/assignment/history unchanged; fallback is aesthetic failure | NOT RUN |
| Donor integrity | Original native menus, text, sprites, clicks, Back/Next and name input unchanged after repeated use | NOT RUN |
| Finalization | Existing disposable-fixture final-character/reload checks, creation kinds/providers scoped to what is exercised; never valued saves | NOT RUN |

Run 1920x1200, 1920x1080, 1600x900, 1366x768, 1280x720 and 1152x720,
including supported UI scaling/effective constrained layouts. Record screen size,
actual parent/canvas dimensions, canvas scale, panel/body viewport dimensions,
creation kind, provider, input method, fallback diagnostics and artifact hash.
The automated geometry cases use these dimensions as synthetic inputs; they do
not prove the game's actual CanvasScaler values.

Visual review must explicitly judge paper texture/irregular edges, restrained
shadow/ornament, typography beside native controls, readable compact rows and
convincing held press. Screenshot similarity alone proves neither animation nor
audio. Keep offline tests, live interactions, visuals, audio, workflow/finalization
regressions and compatibility as separate statuses.

## K. Unity Mod Manager settings panel (0.1.11, NOT RUN)

Use the exact candidate ZIP, a temporary installation with the existing
backup/restore procedure, and only a disposable new-character creation. Never
use Return to Point Buy only to obtain a screenshot.

1. At the main menu, open the mod's UMM settings. Confirm there is no
   duplicate name/version heading. The panel should show only the
   roll-controls sentence, `Status: Ready.`, any concise compatibility notice
   (with Bag of Tricks installed: "Compatibility with Bag of Tricks is
   unverified. See Troubleshooting.") and **Show troubleshooting**. It must not
   show a disabled Return to Point Buy button. It should fit without
   scrolling at the ordinary UMM window width, with readable wrapping and a
   legible amber notice.
2. Press **Show troubleshooting**. Confirm the verbose toggle keeps its saved
   value and that the workflow, counters, contract MVID, detected mods with
   flags, the full compatibility text and (with verbose on) recent entries
   appear. Hide it again. Confirm nothing in the game changed and the verbose
   preference is unchanged after closing UMM.
3. In a disposable new-character creation, confirm the status reads "This new
   character can use Roll Stats." with no recovery action. After **Roll** on
   the ability page, confirm "is using rolled scores." and an enabled
   **Return to Point Buy** with its one-line effect.
4. During an ordinary level-up of a loaded disposable character, confirm the
   panel stays `Status: Ready.` with no warning even though rejected contexts
   increase under Troubleshooting.
