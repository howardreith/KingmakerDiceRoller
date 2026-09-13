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
mask, and content. Only a 164 by 38 **Roll Stats** button remains raycastable.
The tab is always placed at the safe bottom center of verified ability-page
geometry, above the 92-unit bottom-navigation inset plus an 8-unit gap. Its
horizontal geometry source is selected in this order:

```text
active, usable CharBAbilityScoresAllocator.m_RaceBonusContainer
CharBAbilityScoresAllocator.m_Frame RectTransform
ability allocator RectTransform
ability-phase root RectTransform
```

Every candidate is converted into the owned root's local Canvas coordinates.
The tab is horizontally centered within that candidate, then clamped inside the
18-unit left/right and top safe bounds. An absent, inactive, rebuilt, or
off-region racial container cannot send the tab to the screen corner. There is
no upper-right fallback.

The tab must never overlap the racial selector arrows, skill controls, Back,
or Next. Because the owned root has no `Graphic`, every point outside the tab
routes directly to Kingmaker while collapsed.

## Responsive expanded surface

The expanded UI is an upper-right inset paper page. Wide prefers 600 by 728
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

The surface uses the verified native `dialogue_backsheet` sprite as separately
owned sliced Paper and PaperShadow layers. Mottling, irregular edges and depth
come from real native artwork. Decoration remains outside the content mask;
the transparent rectangular interactive surface shields only the bounded page.
See [NATIVE-UI-STYLE.md](NATIVE-UI-STYLE.md) for exact paths and contracts.

The 52-unit header is fixed outside the scroll body and contains a 24-unit title,
16-unit mode and a 76 by 34 Close button. Closing invokes the existing drawer
synchronization once and returns focus to Roll Stats. No footer is reserved.
“Array applied” and idle success boilerplate are suppressed only in presentation.
Actionable validation/command errors remain intact in a conditional, measured
message at the top of the body; new errors return the scroll to the top and long
messages can scroll. The header and Close remain reachable.

The body has a `RectMask2D`, one measurement `ContentSizeFitter`, and a clamped
vertical-only `ScrollRect`. After meaningful geometry or visibility changes,
the host measures `LayoutUtility.GetPreferredHeight(body)` against the returned
viewport. Overflow beyond a 2-unit tolerance enables scrolling and the narrow
scrollbar. When content fits, scrolling and scrollbar are disabled and the
position resets to the top. No child renders beyond the mask and horizontal
scrolling is never enabled.

## Typography and contrast

Heading, body/status, button and selector fonts/materials come from distinct
verified native roles. Sizes are 24 for title, 20 for section headings, 18 for
body/selectors/buttons and 16 for messages. Brown body text and reddish headings
sit on textured paper; gray framed buttons use native pale gold lettering and
normal/hover/pressed/disabled sprite swaps. Shared materials are not edited.

Selectors wrap and measure their row height at narrow widths. Summary, History,
Saved and errors also measure text. Noninteractive TMP labels reject raycasts;
button/input backgrounds and scroll controls are explicit targets. Native click
feedback uses the ordinary UI sound route through one owned activation listener.
All live appearance, motion, audio and input-method acceptance remains NOT RUN
for this candidate until the guarded smoke workflow is authorized.

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
with Store/Previous/Next then Recall/Delete on two compact rows. Ordinary
Wide Roll Mode is intended to fit without wheel input; this remains a live gate.

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
- Theme resolution/construction failure: log a bounded diagnostic and use the
  previous usable presentation without changing the roll session. Base context
  or mechanic safety failures retain the existing fail-closed behavior.

## Human layout gate

Exact compilation and contract fixtures cannot prove real resolution scaling,
prefab offsets, focus, visual clipping, or click routing. The candidate requires
1920 by 1200, 1920 by 1080, 1600 by 900, 1366 by 768, 1280 by 720 and 1152 by
720 plus effective constrained geometry, recording actual canvas/viewport sizes. New-main-character creation must also be regressed.
The gate must prove safe bottom-center tab placement, true collapse, Back/Next
access, one panel through navigation/rebuild, and complete cleanup. Constrained
effective geometry must remain fully visible and clickable. Any obstruction or
upper-right placement remains a runtime blocker and requires a screenshot for
human visual acceptance.
