# 0.1.15 native-stat UI v4 review candidate

## Artifact and provenance

- Branch: `codex/native-stat-ui-v2`; continuation of [draft PR #7](https://github.com/howardreith/KingmakerDiceRoller/pull/7).
- Clean implementation commit: recorded after candidate qualification.
- Installable UMM ZIP and SHA-256: recorded after candidate qualification.
- Target: Pathfinder: Kingmaker 2.1.7b, .NET Framework 4.7.2, UMM/Harmony12.
- Publication: review candidate only; no 0.1.15 release requested.

## What changed

The roll transaction now admits one command before dice generation. Each
candidate captures the current owner, generation, six scores, allocator,
history, and native skill/spell allowances. A failure restores only that exact
live binding, replays native derived bookkeeping, and verifies the restored
skill-point total, remaining points, spell slots, choices, prior assignment,
and history. A failed verification quarantines the candidate and leaves only
Return to Point Buy as the recovery action. Detailed diagnostics retain the
native exception stage, inner exception, and stack. The intermittent failure
in the owner's screenshot has no accompanying live log, so its original
throwing instruction is not established.

The owned Value numeral is removed from the native layout group and centered
inside the original circle. Notices no longer add height to the three-row
toolbar; the host measures a free band above native scores. The racial fit
check measures the complete visible selector against the book and global
navigation. Formula help is limited to pointer hover or intentional keyboard
focus and is suppressed while the preset popup opens. The tooltip has no
scroll-end ornament. Previous, Next, and Return have no owned tooltip.

## Qualification and v4 acceptance

| Item | Status | Evidence / remaining live check |
| --- | --- | --- |
| V4-01 Reroll recovery | Source test PASS; live NOT RUN | Partial native side effects restore prior skill/spell state and history; reentrant Reroll consumes no extra RNG; incomplete recovery blocks further commands. The original intermittent in-game failure cause needs a live log. |
| V4-02 Value circle | NOT RUN in game | Inspect totals 6, 50, 120, 720 at 1920×1200 and a second scale. Source geometry cannot prove rendered placement. |
| V4-03 notices/racial fit | NOT RUN in game | Inspect a visible error/note and complete Human lower frame against the book edge and navigation at both scales. |
| V4-04 formula tooltip | NOT RUN in game | Check hover, keyboard focus, popup open/selection, pointer exit, and teardown. |
| V4-05 tooltip ornament | Source inspection PASS; live NOT RUN | `NativeScrollEnd` is no longer created; inspect the parchment frame in game. |
| V4-06 secondary tooltips | Source inspection PASS; live NOT RUN | Previous, Next, and Return no longer attach the shared tooltip; inspect hover/focus in game. |

The live procedure is in `docs/SMOKE-TEST.md`. Owner screenshots in the
mission archive are before observations, not after captures. No game visuals,
input, audio, or compatibility qualification is claimed by this candidate.
