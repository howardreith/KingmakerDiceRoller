# Native UI design

## Host and ownership

The primary interface is code-owned UI on the exact active Kingmaker
Skills/ability allocator page. Its top-level object is named
`KingmakerDiceRoller.NativeRollPanel`, contains no `Graphic` or layout, and
therefore has no full-screen raycast footprint.

It owns two mutually exclusive children:

```text
OwnedRoot (no Graphic, no raycast target)
  ExpandedSurface
  CollapsedAccessTab
```

New stable character-build owners start collapsed. Opening or closing is a
presentation-only action. The choice survives same-owner allocator and preview
replacement, and resets for a genuinely new owner.

## Collapsed access

Collapsed mode deactivates the complete expanded surface, background, layout,
mask, and content. Only a 140 by 34 **Roll Stats** button remains raycastable.
The preferred exact anchor is immediately beneath
`CharBAbilityScoresAllocator.m_RaceBonusContainer`. When that container is not
active or cannot supply a safe local `RectTransform`, a bounded upper-right
anchor with an 18-unit inset is used.

The tab must never overlap the racial selector arrows, skill controls, Back,
or Next. Because the owned root has no `Graphic`, every point outside the tab
routes directly to Kingmaker while collapsed.

## Expanded surface

The expanded UI is a 400 by 570 rectangular surface with 16-unit internal
padding. It uses a code-owned solid parchment `Image` at 0.98 opacity and the
native local material. It deliberately does not copy the allocator's
`m_Frame` sprite: live evidence showed that sprite is a large curved oval and
cannot contain this panel's rectangular content.

The surface has a `RectMask2D`. Its content is placed inside a vertical,
clamped `ScrollRect` with a second masked viewport and no horizontal scrolling.
No child may render beyond the visible rectangle. A normal compact **Close**
button in the header returns to the true collapsed state.

## Typography and contrast

The local `m_MainLabel` supplies the Kingmaker font and font material only.
Body labels use explicit dark brown text on the opaque parchment; headings use
a darker red-brown. Essential sizes are 20 for the title, 16 for headings, 14
for selectors/body, and 13 for compact status text.

Code-owned dark button surfaces use light text with a subtle dark TMP outline.
Selector values are single-line, bounded to 14-16 point auto-sizing, and use
ellipsis only as a last resort. Noninteractive TMP labels always have
`raycastTarget = false`; button and input backgrounds are the intentional
raycast targets.

## Progressive disclosure

Point Buy initially shows:

1. title, Close, and **Point Buy** heading;
2. captioned **Roll method** selector;
3. Roll;
4. collapsed **Roll Options**;
5. **Saved (n)** only when a saved array can be recalled;
6. one concise status or validation line.

**Roll Options** contains the captioned **Low-score rule** selector. Minimum is
shown only for a minimum-based rule. Custom input and `Example: 4d[6]kh3` are
shown only for Custom expression.

Roll Mode replaces the irrelevant Point Buy controls with Reroll, Return to
Point Buy, six aligned assignment rows, total/equivalent/rule summary, and
collapsed **History (n)** and **Saved (n)** sections. Their navigation buttons
appear only when the player expands that section.

Racial modifiers remain in Kingmaker's existing modifier presentation. The
panel labels base assignments only.

## Presentation separation

`RollUiSnapshot` contains immutable workflow display data.
`RollPanelPresenter` maps it plus local disclosure state into a data-only
`RollPanelModel`, including player-facing names such as **Keep all rolls**,
**Reroll low scores**, and **Reroll whole array**.
`RollUiCommandRouter` maps workflow clicks to coordinator commands.

Disclosure actions update only `NativeRollPanelState`; rendering and
open/close never generate dice, mutate the roll session, write a stat, or save
settings. Technical controller/generation facts remain in UMM diagnostics.

## Native control behavior

PointBuy mode leaves Kingmaker's plus/minus controls authoritative. Roll Mode
captures and sets all exact row buttons non-interactable. The original states
are restored on cleanup, while a successful native point-buy `FillData()`
refresh becomes authoritative after Return to Point Buy.

Dedicated Up/Down assignment controls never reuse Kingmaker's point-buy click
handlers.

## Lifecycle

The exact allocator `FillData()` postfix requests attach/refresh. A UMM Update
observer provides bounded cleanup and allocator-replacement handling.

- First eligible allocator: attach one collapsed view.
- Repeated FillData on the same allocator: render only.
- Replacement allocator for the same owner: recreate one view and preserve its
  open/disclosure choice.
- New stable owner: reset to collapsed with disclosures closed.
- Phase exit: destroy all owned view objects while preserving same-owner view
  choice for navigation.
- Cancel, completion, disable, or unload: restore native controls, destroy all
  owned objects, and clear view ownership.
- Contract or construction failure: fail the panel closed and leave vanilla
  Point Buy untouched.

## Human layout gate

Exact compilation and contract fixtures cannot prove real resolution scaling,
prefab offsets, focus, visual clipping, or click routing. The first live
`0.1.0-alpha.2` gate is 1600 by 900 and must prove readable expanded content,
true collapse, access to every skill control plus Back/Next, one panel through
navigation, and complete cleanup. Any obstruction remains a runtime blocker.
