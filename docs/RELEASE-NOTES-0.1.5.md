# Kingmaker Dice Roller 0.1.5

This release completes the skills-page integration after the rolled-stat
build-integrity repair. It fixes two defects reported together:

- **The red skill-points badge stayed at its old number** after a changed
  rolled assignment — even after closing the roll drawer — until some later
  skill interaction repainted it.
- **The creator allowed advancing out of Skills with a newly invalid skill
  allocation** (unspent or overspent budget after the Intelligence changed),
  which then produced an empty feats/traits page.

Exact Kingmaker 2.1.7b IL inspection established why: the badge is repainted
only by `CharBSkillsAllocator.FillLevelUpData` through the dirty Skills phase,
the phase-unlock cache that gates every forward transition is recomputed only
by `DefinePhases` inside `SetupUI`, and the feature-selection sections are
rebuilt by `DefineAvailibleData`. A rolled-assignment change ran none of those,
so both the display and the navigation decision were stale.

The repair:

- After every relevant change — Roll, Reroll, reassignment, History, Recall,
  Return to Point Buy, verified preview replacement, bounded restage, failed-
  command rollback, and drawer close — the mod replays the exact native
  skill-click refresh sequence (`DefineAvailibleData` + mark the Skills phase
  dirty + `SetupUI`). The badge, skill rows, phase completion, unlock chain,
  and feature sections then agree with the live model. It never simulates
  clicks, allocates points, rerolls, or clears selections, and repeated
  open/close without changes is a no-op.
- A veto-only guard now sits on `CharacterBuildController.SetPhase`, the single
  funnel for Next, keyboard/gamepad submit, and later-phase jumps. While a
  Dice Roller session owns the build, forward movement beyond Skills is
  blocked whenever the live native predicate
  `LevelUpState.IsSkillPointsComplete()` is false — including overspending
  after a lower-Intelligence array — showing the native attention marks and an
  actionable reason. Back and same-phase navigation always stay available,
  legal correction immediately restores progression, and the guard can never
  turn a native rejection into permission or affect ordinary level-ups.

336/336 C# service cases (8 new skills-navigation regressions) and 30/30
Python cases pass. Exact Kingmaker 2.1.7b contracts — including the new
skills-page refresh/transition members — were verified against the installed
assembly. Release compilation and package validation pass.

**Official release.** After the 0.1.4 testing prerelease was played and the
remaining skills-badge defects were reported and fixed, Howie instructed:
"Please commit, merge, push, and do a release. And make it an actual release,
not a test pre-release like the last one. We can make it 0.1.5 it's fine. No
one else is using this mod yet." `v0.1.5` is therefore published as the
official latest release at the owner's direction. The agent-executed
interactive/save/provider matrix remains separately recorded as NOT RUN
(`docs/SMOKE-TEST.md`, sections K and L); `v0.1.4` remains available as the
prior testing prerelease.
