# 0.1.14 native-stat UI presentation candidate

## Artifact and provenance

- Branch: `codex/native-stat-ui-v2`; continuation of [draft PR #7](https://github.com/howardreith/KingmakerDiceRoller/pull/7).
- Clean implementation commit: pending qualification.
- Installable UMM ZIP and SHA-256: pending clean-commit candidate packaging.
- Target: Pathfinder: Kingmaker 2.1.7b, .NET Framework 4.7.2, UMM/Harmony12.
- Publication: not requested for this revision. The prior v0.1.13 testing alpha is unchanged.

## V3 acceptance evidence

| Item | Status | Evidence and remaining live check |
| --- | --- | --- |
| V3-01 formula affordance | NOT RUN in game | Inspect centered native image icon, all six presets, long and invalid drafts at both resolutions. |
| V3-02 complete Value badge | NOT RUN in game | Inspect caption and dash/6/77/120/720 inside the original frame, then Point Buy caption/remaining value. |
| V3-03 downward swap icons | NOT RUN in game | Inspect six rows after refresh; click each enabled adjacent swap once and confirm the disabled endpoint. |
| V3-04 mod tooltips | NOT RUN in game | Inspect short and long help near page edges, pointer/focus and teardown behavior. |
| V3-05 native hover cleanup | NOT RUN in game | Inspect a partially spent Point Buy origin through Roll-ready, hover, FillData, applied Roll, Return and legal purchase/refund. |
| V3-06 Human fit | NOT RUN in game | Inspect full Human page with chooser and bottom navigation, conditional note and error at 1920×1200 and a smaller 16:9 scale. |

The project-owned deterministic runner exercises the tooltip frame placement
at the top, side edges, and a constrained viewport. The installed Kingmaker
contract verifier confirms `CharBScoresEntry.m_CostContainerAnimator`,
`m_CostLabel`, `m_UpCost`, and `m_DownCost`, plus the animator field reads in
`OnPointerEnter` and `OnPointerExit`. The inspected IL shows the row's pointer
handler invokes `FadeAnimator.AppearAnimation`/`DisappearAnimation` and raises
the ordinary ability highlighter. The guard is appended to those six exact
native rows, conceals only their cost container and text, and is removed on
teardown. Point-buy math and the row's ability description/highlighter remain
native.

The same installed scene capture locates the ability caption at
`PointsContainer/FieldName`, the numeral at `PointsContainer/Points/Value`,
and the original 64×64 circle at `PointsContainer/Points`. The badge copies
those exact text rects and TMP properties as owned siblings. The native
score-icon sprite is shared by `ButtonPlaceNext/Button` and
`ButtonPlaceBack/Button`; their captured root rotations are zero and the
next icon is 180 degrees. The earlier additional 180-degree turn produced the
owner's left arrow; the new icon-only turn targets downward screen direction,
pending the in-game visual check.
The tooltip uses the validated `dialogue_backsheet` paper,
`blockscroll_bottom` ornament, and native Body font capability when present.

Owner screenshots in the mission package are before observations, and design
schematics are not candidate after captures. Source/IL qualification proves
the scoped field and method identities, not rendered placement or input.
