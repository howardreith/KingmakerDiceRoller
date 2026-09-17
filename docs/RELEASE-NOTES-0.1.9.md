# Button caption fit

**Official release at the owner's direction.** The owner supplied in-play
screenshots of the 0.1.8 drawer and authorized a full official release for
this repair, planning to test on another computer after publication. Rendered
visual verification by the development agent is recorded separately below;
publication does not claim it.

## The defect

The owner's screenshots showed the drawer working and the native theme
active, but multiple button captions truncated with ellipses: "STO…",
"PREVI…", "REC…", "DEL…", "DO…", and "RETURN TO POINT B…". The "Low-score
rule" caption was also cut.

The cause was confirmed in source: `CreateButton` derived both
`LayoutElement.minWidth` and `preferredWidth` from fixed design constants
(Store 62, Previous 88, Recall/Delete 70, Return to Point Buy 210,
Up/Down 64, caption column 108) without consulting the caption's actual text
metrics. Button labels use `TextOverflowModes.Ellipsis`, so any caption wider
than its constant was clipped, and the fixed constants predate the native
theme's font, style, character and word spacing, which
`NativeBookTheme.CopyText` applies after construction without any width
recalculation.

## The repair

- Every textual owned button — including Close and Roll Stats — now reserves
  `max(design minimum, measured caption + horizontal insets + safety)` and the
  corresponding height, applied through its existing `LayoutElement` so
  parent layouts cannot compress it below the caption's needs. Measurement
  uses the installed TextMesh Pro `preferredWidth`/`preferredHeight`, which IL
  inspection of the installed assembly confirmed is unconstrained (large
  margins) and computed from the live font, style, character and word
  spacing — never from truncated rendered text.
- Captions are re-measured when native theme styling is applied or fallback
  is restored (including delayed recovery) via additional `ButtonText`/`Body`
  capability bindings, and dynamic disclosure captions (Roll Options,
  History, Saved) re-fit only when their text changes. There is no per-frame
  measurement and no control recreation.
- Selector captions such as "Low-score rule" reserve their full styled width
  instead of the fixed 108-unit column; the selected rule and `<`/`>`
  navigation keep their existing space.
- Each assignment Up/Down pair shares one unified fitted width so the six
  rows stay aligned. In Compact layouts, where the single point-actions row
  cannot hold Roll, Reroll and the fitted Return caption, the Return control
  reflows onto a spare row; Wide keeps the original single row.
- The native button artwork, typography, hover/pressed/disabled states,
  click routing and audio are unchanged. The 0.1.8 input-initialization order
  and bounded construction retry are preserved; the caption fit guards the
  label font before reading any TMP property.

## Qualification and limitations

- 384/384 C# behavior cases (nine new caption-fit policy cases, including
  wide row capacity with the scrollbar allowance and the Compact reflow
  bounds) and 30/30 Python oracle cases passed; 13/13 release-gate cases and
  repository source validation passed.
- Existing Kingmaker contracts, 11 respec groups and 30/30 native UI IL
  checks passed, including five new checks: the installed TMP preferred-width
  computation path, font-guard-before-measure ordering, LayoutElement width
  reservation, ButtonText capability re-fit wiring, Up/Down unification and
  the Compact reflow hook. The checks fail against the released 0.1.8 DLL as
  a negative control.
- The lab-only runtime probe now reports each owned button's caption text,
  unconstrained preferred width, rendered width and reserved width, plus the
  selector caption, so a guarded live session can prove the fit with the real
  native font. If no guarded interactive session is available, rendered
  caption verification is recorded NOT RUN with the concrete blocker.
- The owner's in-play test on another computer after publication is the
  acceptance lane for the rendered result. Mercenary/respec live fixtures,
  audio and additional resolutions remain separately NOT RUN.
