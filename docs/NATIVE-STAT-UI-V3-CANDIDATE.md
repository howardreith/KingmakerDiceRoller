# 0.1.14 native-stat UI presentation candidate

## Artifact and provenance

- Branch: `codex/native-stat-ui-v2`; continuation of [draft PR #7](https://github.com/howardreith/KingmakerDiceRoller/pull/7).
- Clean implementation commit: `252ff2f435476bdc74d051cf09358250dd3e204c`.
- Installable UMM ZIP: `artifacts/candidates/252ff2f435476bdc74d051cf09358250dd3e204c/KingmakerDiceRoller-0.1.14.zip`.
- ZIP SHA-256: `95603893a14a0b6538977070e893603ab1328f556c7893fe70d5229fbfca6d1a`.
- DLL SHA-256: `73d5600ec3c4e7df3a2cc954d321775a09518ea33755307a71de411407d6e684`.
- Target: Pathfinder: Kingmaker 2.1.7b, .NET Framework 4.7.2, UMM/Harmony12.
- Publication: [v0.1.14 testing alpha](https://github.com/howardreith/KingmakerDiceRoller/releases/tag/v0.1.14), with the identical qualified ZIP. The annotated tag points to documentation-only release commit `a45e5336add15dfc45aea249ee2b6d7c5e78c142`. The prior v0.1.13 alpha is unchanged.

## Qualification

| Check | Result | Evidence |
| --- | --- | --- |
| Clean candidate workflow | PASS | `powershell.exe -File scripts/Qualify.ps1 -Build -Package -Candidate` on the implementation commit. |
| Source/release gate | PASS | 20/20 release-gate cases; repository/source validation. |
| Deterministic C# behavior runner | PASS | 413/413, including five tooltip frame-placement cases. |
| Installed Kingmaker/respec contracts | PASS | Exact 2.1.7b reflection identities and 11 respec groups. |
| Native UI IL/contracts | PASS | 30/30. |
| Release build and UMM package | PASS | Zero warnings/errors; strict package validation and ZIP/DLL hashes above. |
| Actual game visuals, input, audio, and teardown | NOT RUN | Computer-use helper failed during initialization with `helper_unknown_error` before window discovery, including the reset/retry. No after capture was obtained. |
| Live compatibility | NOT RUN | No game run. |

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
The candidate was not installed into the game on this computer, so installed
package identity, resolution, canvas scale, creation context, and race for an
after run are **NOT RUN**. Use the cases in `docs/SMOKE-TEST.md` for the six
remaining live checks on this exact ZIP.
