# Native UI design

## Host and ownership

The primary interface is a code-owned panel titled **Rolled Ability Scores** on
the exact active Kingmaker Skills/ability allocator page. It is parented within
the allocator's local phase hierarchy, anchored at the upper right, bounded to
470 by 670 layout units, and collapsible.

The host clones no shared prefab and changes no shared asset. It creates clean
`GameObject`, `RectTransform`, layout, image, TMP label/input, and button
components. Local native `m_MainLabel`, `m_Frame`, and one score-row button
provide font, material, sprite, transition, and color styling.

The root uses the unique name `KingmakerDiceRoller.NativeRollPanel`. The host
tracks its own reference and destroys only that owned object.

## Layout

Top to bottom:

1. title and Hide/Show control;
2. mode and status;
3. preset selector;
4. low-score policy selector;
5. minimum selector (inactive for Tabletop);
6. custom expression input and syntax hint when Custom is selected;
7. Roll, Reroll, and Point Buy actions;
8. six STR-CHA rows with assigned base value and Up/Down controls;
9. total, point-buy equivalent, extended marker, and rule;
10. history position with Previous/Next/Use;
11. saved position with Store/Previous/Next/Recall/Delete;
12. inline validation error and concise status.

Racial modifiers remain in Kingmaker's existing modifier presentation. The
panel intentionally labels base assignments only.

## Presentation separation

`RollUiSnapshot` contains immutable display data. `RollPanelPresenter` formats
it without side effects. `RollUiCommandRouter` maps clicks to coordinator
commands. Rendering cannot roll dice, mutate a session, write a stat, or save
settings.

All button callbacks catch command failures. Player-facing text stays concise;
technical controller/generation facts remain in UMM diagnostics.

## Native control behavior

PointBuy mode leaves Kingmaker's plus/minus controls authoritative. Roll Mode
captures and sets all exact row buttons non-interactable. The original states
are restored on cleanup, while a successful native point-buy FillData refresh
becomes authoritative after Return to Point Buy.

Dedicated Up/Down assignment controls never reuse Kingmaker's point-buy click
handlers.

## Lifecycle

The exact allocator `FillData()` postfix requests attach/refresh. A UMM Update
observer provides bounded lifecycle cleanup and allocator-replacement handling.

- First eligible allocator: attach once.
- Repeated FillData on the same allocator: render only.
- Replacement allocator for the same owner: detach/rebind one panel.
- Invalid phase/context, cancel, completion, disable, or unload: restore native
  controls and detach.
- Contract or construction failure: fail the panel closed and leave vanilla
  Point Buy untouched.

Attachment and render never generate random values.

## Human layout gate

Exact compilation and contract fixtures cannot prove real resolution, scaling,
clipping, navigation-button clearance, focus, or click routing. The first live
alpha gate must check common supported resolution(s), expanded/collapsed modes,
custom input focus, all controls, and phase navigation. Any overlap or clipped
essential control remains a runtime blocker.
