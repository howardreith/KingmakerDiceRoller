# Native book UI reskin state

Mission branch: `codex/native-book-ui-reskin`.
Actual clean baseline: `504b5ae53600be99a01f437b35ed57cdff6c3535`
(`main` and locally recorded `origin/main`). No reset or unknown work replaced.

Completed: full mission and required project/integrity records read; all twelve
owner screenshots inspected; exact PC main-menu/in-game donor paths, paper
texture/borders, native button state sprites, typography and audio call route
inspected. First slice implemented: opener, paper/shadow, heading and Close.
It compiles against installed Unity/Kingmaker without warnings. Full reskin,
new regression cases, documentation and candidate packaging follow.

Baseline offline: 336/336 C#; 30/30 Python; repository/release gates and native
contracts pass; 11 respec checks pass; Release zero warnings/errors. Baseline
DLL matches released 0.1.5 hash. Evidence:
`artifacts/native-ui-reskin/baseline-build.txt`.
First slice: `artifacts/native-ui-reskin/slice-build.txt`.
Donor reference: `docs/NATIVE-UI-STYLE.md`.

Live interactions, visuals, audio, workflow regressions, compatibility and
final-character/save tests: **NOT RUN**. No authorized guarded runtime fixture,
launch, desktop control or test installation is present. Next safe action:
apply the compiled source-qualified slice to all controls and finish offline
qualification. Fresh-launch rendering, held/canceled presses, focus, UI
volume, actual scaling, donor integrity and lifecycle remain live questions.

No push, merge, PR, tag, publication, active install, other-mod edit or save
modification is authorized or performed. Historical release permission is not
authority for this mission.
