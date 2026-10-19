# 0.1.13 revised native-stat UI candidate

## Artifact and provenance

- Branch: `codex/native-stat-ui-v2`.
- Clean implementation commit: `b99cbfc5eb07070c916d82e06d4a7dd72b4acc34`.
- Installable UMM ZIP: `artifacts/candidates/b99cbfc5eb07070c916d82e06d4a7dd72b4acc34/KingmakerDiceRoller-0.1.13.zip`.
- ZIP SHA-256: `819744d22fdcf1dfffca92619547b7c5c17db91390cfb9ffcb22c6b52c53e2be`.
- DLL SHA-256: `47d4c894b3050c5d567ed35ed03e88317de8c72d5e62ffb376734b005492a169`.
- Target: Pathfinder: Kingmaker 2.1.7b, .NET Framework 4.7.2, UMM/Harmony12.
- Publication: **No GitHub or official release** for 0.1.13. The published
  0.1.12 alpha is an earlier build and is not evidence for this revision.

## Implemented behavior

Ordinary Point Buy adds one Roll for Stats button. Roll-ready is a view-only
state with concealed native buy/refund controls, an empty Value badge, an
editable formula, and no generated array. The exact ability allocator badge
shows the applied six-base-score total after Roll, Load, or history recall;
native Points returns after Point Buy restoration. The inline workspace has
one right-hand downward adjacent-swap column, a six-choice preset parchment,
three primary actions, direct Previous/Next, and a separate Return button.
Load contains saved arrays only. Low-score policy and minimum are available
in UMM Roll settings, with a note on the workspace when active.

History navigation inspects a neighboring record before applying it and
activates the cursor only after verified application. Endpoint navigation does
not wrap; a successful saved recall appends one entry. History selection no
longer overwrites the departed entry when equal arrays have different source
permutations. Formula preflight rejects provably impossible constants without
using RNG, while the raw draft remains independent of the persisted valid
configuration.

## Evidence

| Check | Result | Evidence |
| --- | --- | --- |
| Repository source and release-gate validation | PASS | `Qualify.ps1 -Build -Package -Candidate`; release gate 20/20. |
| Deterministic C# behavior runner | PASS | 408/408, including new formula draft/preflight, equal-array history, saved recall, and UMM policy-setting cases. |
| Installed Kingmaker reflection contracts | PASS | Read-only 2.1.7b verifier, including exact ability badge TMP fields. |
| Respec and native UI IL contracts | PASS | 11 respec groups and 30 native UI checks. |
| Release build | PASS | Zero warnings and errors; DLL SHA-256 above. |
| Deterministic six-file UMM package | PASS | Strict package validation; ZIP SHA-256 above. |
| Actual in-game layout, focus, audio, clicks, and lifecycle | NOT RUN | Computer-use bridge exited before window inventory on two attempts; no game screen was observed. |
| Live compatibility sweep | NOT RUN | Requires actual game access. |

The two Kingmaker screenshots in the v2 mission archive are owner-provided
**before** evidence. The proposed-layout and preset-menu SVG/PNG files are
design schematics. Neither is an actual 0.1.13 after capture. The installed
scene/assembly inspection proves bindings and donor availability, not rendered
placement or interaction.

## Live acceptance still needed

Follow `docs/SMOKE-TEST.md` under “0.1.13 revised native-stat candidate.”
Capture ordinary Point Buy, Roll-ready, applied Roll, the expanded six-row
preset menu, and an invalid formula at 1920×1200 and a smaller 16:9 layout.
Verify badge legibility, the arrow's visible direction and centering in every
state, keyboard Enter/Escape, popup reachability, one-click/one-audio behavior,
racial selectors, Back/Next clearance, and same-owner rebuild/owner-change
cleanup. Repeat main-character, mercenary, and qualified editable-respec
paths, including exact Point Buy restoration and derived skill allowances.
