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

## Responsive expanded surface

The expanded UI is an upper-right rectangular drawer. Wide prefers 620 by 760
UI units with 18-unit top/right safe insets and a conservative 92-unit bottom
inset for native navigation. Compact prefers 460 by 650 and clamps to the
actual relevant parent `RectTransform`. These are Canvas-space values, not raw
screen pixels.

`ResponsiveRollPanelLayoutCalculator` is pure and data-only. Its primitive
inputs are available parent width/height, safe insets, measured preferred body
height, prior profile, and prior scroll state. It returns:

```text
Wide or Compact
safe width and height
panel width and height
header and footer height
body viewport height
scroll required
safe anchored insets
```

Wide requires meaningful safe geometry (normally at least 560 by 680). An
8-unit geometry hysteresis avoids profile chatter. All results clamp to safe
bounds and body space cannot become negative. Responsive profile is not
persisted as character state and is identical on main-character and mercenary
screens.

The surface uses a code-owned solid parchment `Image` at 0.98 opacity and the
native local material. It deliberately does not copy the allocator's `m_Frame`
sprite: that sprite is a curved oval and cannot contain the drawer.

The 38-unit header is fixed outside the scroll body. It contains the title,
compact current mode, and a 76 by 30 **Close** button; no
`ContentSizeFitter` controls its height. A compact 38-unit status/error footer
is also fixed. Ordinary messages wrap within that footer without reserving a
large empty error area.

The body has a `RectMask2D`, one measurement `ContentSizeFitter`, and a clamped
vertical-only `ScrollRect`. After meaningful geometry or visibility changes,
the host measures `LayoutUtility.GetPreferredHeight(body)` against the returned
viewport. Overflow beyond a 2-unit tolerance enables scrolling and the narrow
scrollbar. When content fits, scrolling and scrollbar are disabled and the
position resets to the top. No child renders beyond the mask and horizontal
scrolling is never enabled.

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

## Wide presentation

Wide Point Buy displays relevant configuration without a **Roll Options**
disclosure:

```text
Roll method       [<]  4d6, drop lowest       [>]
Low-score rule    [<]  Keep all rolls         [>]
Minimum           [-]  9                      [+]  (minimum rules only)
Custom expression input + example                 (Custom only)
Roll
selected Saved record and Recall controls          (when relevant)
```

Inactive conditional rows reserve no blank space.

Wide Roll Mode keeps assignments in one ordered vertical sequence:

```text
STR  value                                      [Up] [Down]
DEX  value                                      [Up] [Down]
CON  value                                      [Up] [Down]
INT  value                                      [Up] [Down]
WIS  value                                      [Up] [Down]
CHA  value                                      [Up] [Down]
```

It also shows the current roll-method selector, Reroll, Return to Point Buy,
total, point-buy equivalent, actual applied generation rule (`Rolled with:`),
one current History record with Previous/Next/Use, one current Saved record
with Store/Previous/Next/Recall/Delete as applicable, and status. Ordinary
Wide Roll Mode fits without wheel input at the primary desktop gate.

The selected preset remains separate from the actual applied rule because the
player may change the selector after rolling.

## Compact presentation

Compact preserves stable-owner-scoped progressive disclosure. **Roll
Options**, **History**, and **Saved** can begin collapsed, with conditional
Minimum/Custom rows following the same semantic rules as Wide. Only measured
overflow activates scrolling. Header and Close never scroll.

Racial modifiers remain in Kingmaker's existing modifier presentation. The
panel labels base assignments only.

## Presentation separation

`RollUiSnapshot` contains immutable workflow display data.
`RollPanelPresenter` maps it plus local disclosure state into a data-only
`RollPanelModel`, including player-facing names such as **Keep all rolls**,
**Reroll low scores**, and **Reroll whole array**.
`RollUiCommandRouter` maps workflow clicks to coordinator commands.

Disclosure actions update only `NativeRollPanelState`; responsive profile is
derived geometry. Rendering, profile switching, open/close, and disclosure
changes never generate dice, mutate the roll session, write a stat, or save
settings. Technical controller/generation/layout facts remain in UMM
diagnostics.

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
`0.1.0-alpha.3` gate is 1600 by 900 on both new-character and mercenary screens.
It must prove Wide content fits without ordinary scrolling, true collapse,
Back/Next access, one panel through navigation, and complete cleanup. Repeat at
1920 by 1080, 1536 by 960, 1366 by 768, and 1280 by 720; constrained effective
geometry must select a usable Compact profile. Any obstruction remains a
runtime blocker.
