# Native book UI reskin

**Official/latest release at the owner's direction.** Live visual, audio,
runtime and compatibility acceptance checks remain **NOT RUN**. Promotion
preserves the existing ZIP, checksum asset and version tag. The README inside
the unchanged ZIP retains its original testing-prerelease wording; this release
page and the current repository record the official/latest status.

Dice Roller now presents its existing rolled-ability workflow on a smaller
textured paper page within Kingmaker's character-creation book.

- Native inset paper with irregular edges, restrained depth and ornament.
- Gray framed buttons with native normal, hover, pressed and disabled sprites;
  role-specific heading, body, selector and button typography.
- One ordinary native ButtonClick route per accepted activation, using owned
  controls without copied gameplay or navigation callbacks.
- No player-facing “Array applied” message or permanently empty success strip.
  Actionable errors remain visible in measured, scrollable content.
- Readable wrapping selectors, compact Saved actions, a fixed reachable Close
  control, and the existing responsive placement and conditional scrolling.

This is a presentation update. Roll rules, assignments, History/Saved commands,
point-buy restoration, session ownership, close-time skill-counter synchronization
and Next/forward-navigation guards remain unchanged. Cosmetic resource or audio
failures preserve the working roll session through bounded fallback.

## Qualification

- 347/347 deterministic C# tests; 30/30 Python oracle tests.
- 13/13 release-gate tests and 18 source-validation groups.
- Existing Kingmaker contracts, 11 respec contract groups and 16/16 native UI
  IL/component-wiring checks passed.
- Release build: zero warnings and zero errors; package contains the six
  allowlisted files and no extracted game artwork, fonts, audio or assemblies.
- Live interactions, native visual/press-motion acceptance, audio/volume/mute,
  in-game workflow/finalization/save regressions and compatibility matrix:
  **NOT RUN**. Publication does not establish these acceptance results.

Targets Pathfinder: Kingmaker 2.1.7b with the existing Unity Mod Manager and
Harmony12 conventions. No other mod dependency or new supported context is added.
Native resources were inspected in the PC main-menu and in-game scene metadata;
actual fresh-launch rendering, UI scaling, input methods and donor integrity
still require the documented live smoke checks. No game installation or valued-save
modification was performed as part of preparing this release.
