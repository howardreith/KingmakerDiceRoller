# Native-theme activation repair handoff

## Baseline and scope

Repair branch `codex/native-theme-activation-repair` starts at current accepted
main `9b69751983a6bc80d1e73999d3fbba3e3e7f8927`, retaining all reskin and
character-creation fixes. `77d16ef1e1bbfefd4d0696c9288e1f5966e83312` is an
ancestor. The annotated `v0.1.6` tag peels to
`7662a4239512edd9be7a8d3c3ca511a1d9ce763a`; downloaded release and installed
pre-repair DLL both hash to
`e2803d3f70ac168d52de27a075b36e6562c7209e709d02c62485f7ded0b13092`.
No downgrade/reset occurred. Version strings alone were not used as identity.
The historical release is official/latest by owner instruction, despite the
original testing-prerelease description. Its ZIP hash is
`6e81dde5773b1d23706f57ed96f1f738a28a9cabc955295d5625a21a8911d1f1`.

The new repair is version `0.1.7`, for branch push and review PR only. Merge,
tag and public release are outside this mission. The owner explicitly
authorized this machine's Kingmaker/Steam installation for disposable runtime
testing. No valued campaign saves may be loaded or modified.

## Root cause and repair

The action label is a single direct child named `Next/Complete text`. Old
`Require` passed it to `Transform.Find`, interpreting the slash as hierarchy
syntax. Serialized direct-child identities in both installed PC scenes confirm
that mismatch. The exception escaped `NativeBookTheme.Resolve`, became a null
theme in `NativeUiPresentation.ResolveTheme`, and disabled all paper, button,
font, input, scrollbar and decorative styling. `EnsureAttached` then returned
early for the same allocator; donors becoming available could not recover it.

`NativeUiDonorLookup` now distinguishes bounded paths and literal direct names,
including inactive nodes, exact ordinal comparisons, component/child ambiguity
and bounded owner/candidate diagnostics. `NativeThemeResolver` is shared
production code: nine strict independent capabilities preserve unrelated
valid resources. `NativeBookTheme` supplies the actual Unity adapter and exact
sprite/font/material checks. No native callbacks, objects or shared assets are
cloned or modified. No new dependency or extracted asset is distributed.

`NativeThemeRecovery` allows initial resolution plus two retries at the existing
allocator FillData postfix. Verified native IL links this boundary to Skills
phase refresh through SetupUI. Repeated own synchronization does not reset its
budget. `NativeRollPanelHost` applies styling to the same controls, preserving
input drafts and listeners. Cached donors are invalidated when destroyed or
moved outside their owner. Real teardown/replacement clears bindings and the
budget. Layout, commands, skill refresh and forward guards retain their behavior.
Detailed donor maintenance rules: [native style reference](docs/NATIVE-UI-STYLE.md).

## Qualification record

Source repair implemented: **YES**. Native themed path verified: **NOT RUN**.
Rendered appearance and interactions inspected: **NOT RUN**.

The repair suite has 370 C# cases, 30 Python cases, 13 release-gate cases,
18 source groups, existing native contracts, 11 respec contract groups and
22 native UI IL checks. A complete dirty-source build passed without warnings
or errors; final clean-candidate qualification and exact hashes are pending.

Deterministic tests execute the shipped lookup, resolver, capabilities,
recovery and binding logic through an opaque node adapter. They do not host
Unity or prove `NativeBookTheme.Resolve` works in-game. Additional production
workflow/coordinator tests retain rolls, RNG consumption, assignments, saved
state, incomplete input, exact command counts and both INT/skill-guard cases.
The lab-only runtime probe exercises the actual Unity entry point and adapter;
see [runtime procedure](docs/NATIVE-THEME-RUNTIME.md).

All machine-local logs, screenshots and binaries remain ignored under
`artifacts/theme-activation/`; no game assets/logs/saves enter Git.
- `baseline-identity.json`: remote/tag/release and installed artifact identity.
- `native-child-identity.json`: serialized literal-child/component evidence.
- `native-skills-phase-il.txt`, `native-phase-update-il.txt`: retry lifecycle.
- `repair-build.txt`: preliminary complete repair build.
- `domain-session-repair.txt`: workflow/session regression coverage.
- Final clean candidate, runtime evidence, restoration and review PR: pending.

Mercenary/respec, finalization/save/reload, controller and compatibility lanes
remain **NOT RUN** until exercised on authorized disposable fixtures. Hearing
or recording ordinary clicks is a separate audio lane; IL alone cannot pass it.
