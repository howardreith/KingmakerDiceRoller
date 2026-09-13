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

The repair is version `0.1.7`. The initial branch/review-only publication scope
was superseded on 2026-09-13 by the owner's explicit request: "Could you please
merge, push, and release this as a new release? Make it an actual release, not
a test. Thank you." Official/latest publication is now authorized, with the
unrun checks disclosed and without claiming runtime qualification. The owner also
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

## Exact review candidate

Branch: `codex/native-theme-activation-repair`.
Clean implementation/candidate commit:
`c06ef756cf69d25c052da9ab3802215815a9e512`.
Subsequent handoff-only commits do not change candidate bytes.
Package, relative to the repair checkout:
`artifacts/candidates/c06ef756cf69d25c052da9ab3802215815a9e512/KingmakerDiceRoller-0.1.7.zip`.
DLL SHA-256:
`9bd269d9f54f937323ef48285781e4ad67896c06117a4449f8b099fc04ad852d`.
Package SHA-256:
`a4e309e7bafcb61c44d5fea01f4c6728fa8063130f49f92617b563c7d9397061`.

`Qualify.ps1 -Build -Package -Candidate` passed from that exact clean commit.
The six-entry package contains only the DLL, Info.json, README, LICENSE,
third-party notices and upstream license. It excludes the probe, extracted
assets, game libraries, settings, logs and saves. Framework .NET 4.7.2,
C# 7.3, Harmony12 and UMM dependencies are unchanged.

## Verified official 0.1.7 publication

[PR #2](https://github.com/howardreith/KingmakerDiceRoller/pull/2) merged to main
at `db36b3a55da66751478fa5847fb3cf92ee3eb7f0`. The existing publisher rebuilt
and fully qualified that clean, fully pushed source, then prepared a draft.
After downloaded asset verification, the owner-authorized publication made
[v0.1.7 official/latest](https://github.com/howardreith/KingmakerDiceRoller/releases/tag/v0.1.7)
at `2026-09-13T15:07:07Z`. GitHub reports draft=false, prerelease=false and
latest release ID `387935603`. No runtime-confirmation assertion was made and
the general qualification gate remains unchanged.

Annotated tag object: `3f9dac9d3e76e83ba9280227f03e6d964f4e8aec`.
Peeled release source: `db36b3a55da66751478fa5847fb3cf92ee3eb7f0`.
Official package in the default-branch checkout:
`artifacts/packages/KingmakerDiceRoller-0.1.7.zip`.
Release DLL SHA-256:
`9bd269d9f54f937323ef48285781e4ad67896c06117a4449f8b099fc04ad852d`.
Release ZIP SHA-256:
`1b3a5ec3be5085359f87c8da5732f8148573b0792072ba567fc995d45ed07cbc`.
ZIP asset ID `561406947`, 120289 bytes; `SHA256SUMS.txt` asset ID `561406946`,
97 bytes, SHA-256
`4556a2486558a2ac428f7b3ba7e90e5aced87dafb63d0fb9b0180a1873fcb0b3`.

The DLL exactly matches the review candidate. The ZIP differs only in its
packaged README, which now describes official publication and discloses the
runtime blocker. Both the original review candidate and prior v0.1.6 assets
remain unchanged. All six published ZIP entries match their merged source/build
inputs; downloaded draft/public assets, checksum contents and GitHub digests
match. Publication retained the draft asset IDs/bytes and annotated tag.
Post-publication documentation may advance main without changing the tagged
build or assets. No additional game installation, launch or save access occurred.

Complete release verification in the default checkout, outside Git:
`artifacts/release-0.1.7/draft-preparation.txt`, `draft-verification.json`,
`published-verification.json`, `draft-metadata.json`, `published-metadata.json`,
`latest-metadata.json`, `download-draft/` and `download-published/`. Repair
candidate/runtime evidence below remains in the isolated repair checkout.

## Separate qualification statuses

| Status | Result and evidence |
| --- | --- |
| Source repair implemented | **YES** — production resolver/capabilities/recovery and regression coverage committed |
| Source qualified | **PASS** — 370 C# cases, 30 Python cases, 13 release-gate cases, 18 source groups |
| Native contracts qualified | **PASS** — existing native contracts, 11 respec groups, 22 native UI IL checks |
| Build qualified | **PASS** — clean Release build, zero warnings/errors |
| Package qualified | **PASS** — exact clean commit, deterministic package and six-file allowlist |
| Temporary installation/startup | **PASS, limited** — pinned DLL installed, fresh owned game and probe loaded, then original installation restored |
| Native themed path verified | **NOT RUN** — genuine new-character Skills and production runtime Resolve were not reached |
| Rendered appearance and interactions inspected | **NOT RUN** — disconnected desktop exposed no foreground window; input/capture guard refused all operations |
| Audio qualified | **NOT RUN** — no audible/captured click test; ordinary route is source/IL checked only |
| Runtime qualified | **NO** — character-creation interaction/skill/navigation/finalization lanes unrun |
| Compatibility qualified | **NO** — no mercenary/respec disposable campaign fixture or focused provider matrix |
| Human visual acceptance | **NOT RUN** |
| Release authorized / published | **YES / YES** — v0.1.7 official/latest; runtime statuses remain unchanged |

Deterministic tests execute the shipped lookup, resolver, capabilities,
recovery and binding logic through an opaque node adapter. They do not host
Unity or prove `NativeBookTheme.Resolve` works in-game. Additional production
workflow/coordinator tests retain rolls, RNG consumption, assignments, saved
state, incomplete input, exact command counts and both INT/skill-guard cases.
The lab-only probe compiles against the installed engine and is ready to
exercise actual Unity objects, but its runtime fixture requests were **NOT RUN**.
See [runtime procedure](docs/NATIVE-THEME-RUNTIME.md).

## Acceptance matrix

| Required lane | Automated/contract evidence | Actual engine / visual lane |
| --- | --- | --- |
| Literal slash child and genuine path remain distinct | PASS: production resolver fixtures; both installed serialized PC hierarchies inspected | NOT RUN |
| Missing owner/child/component, ambiguity, inactive nodes | PASS: bounded exact production lookup fixtures | NOT RUN |
| Missing input/scroll/decor retains valid paper/buttons/text | PASS: independent capability fixtures; strict Unity validators compile | NOT RUN |
| Delayed readiness, capped failures and reentrancy | PASS: initial + two retries; actual native FillData boundary verified in IL | NOT RUN |
| Destroyed/moved/replaced donor/allocator and teardown | PASS: cache invalidation and recovery reset/binding cleanup fixtures | NOT RUN |
| Session, RNG, assignments, saved selection and unfinished draft | PASS: actual workflow/coordinator tests through theme transitions | NOT RUN |
| Single Roll/Close/Store/Previous/Next/Recall/Delete dispatch | PASS: shared command/binding tests and compiled single-listener route | NOT RUN |
| Paper, typography, button states, access tab, input/scroll clipping | PASS: exact contracts and unchanged responsive geometry tests | NOT RUN at normal or additional resolution |
| Ordinary click sound and native isolation | PASS: one ordinary UI sound call and fresh owned Unity button | NOT RUN audible/input behavior |
| INT increase/decrease, immediate close badge and invalid-forward veto | PASS: integration regressions and preserved correctness contracts | NOT RUN; populated traits/feats not observed |
| New-main roll modes, reopen, saved actions, custom input, Point Buy | PASS: existing workflow regression suite | NOT RUN |
| Mercenary / native and provider respec / controller / finalization-save-reload | Existing bounded contract and deterministic coverage retained | NOT RUN; no disposable campaign fixture supplied |

## Owned runtime attempt and restoration

The owner authorized the installed Kingmaker/Steam development machine. A fresh
exclusive ownership record pinned PID **11832**, start
`2026-09-13T14:14:06.3460249Z`, candidate commit/hash and evidence directory.
No pre-existing Kingmaker process or foreign installed candidate was taken over.
The entire original Dice Roller directory (including settings/cache), UMM
configuration and display preferences were backed up and verified. Steam/Cloud
settings and other mod enablement were not changed.

The candidate and temporary probe loaded successfully in Kingmaker. Windows
reported session 1 **Disc** and `GetForegroundWindow() == 0`. Both ordinary
activation and attached-input-thread activation failed to expose the owned
window. The foreground guard refused capture and input. No new character,
roll, save, native resolver request or screenshot was produced. A reconnection
request was sent to the owner; lack of a response was not treated as permission
to bypass the disconnected desktop. Startup context-rejection diagnostics were
kept separate from theme activation; no allocator-budget or unrelated-mod
behavior was changed to address them.

The exact owned process exited gracefully. Restoration completed at
`2026-09-13T14:20:29.1035684Z`: all eight original mod files matched their
pre-test hashes, original DLL hash returned to the recorded `e2803d3f...`, UMM
Params.xml and display preferences matched, the probe was removed, and all
**85** inventoried campaign-save files retained identical paths/lengths/hashes.
The exclusive ownership record was released. No game remains owned by this
mission. The archived candidate/probe/backup are local evidence only.

## Evidence paths and remaining work

All paths below are relative to the repair checkout and ignored by Git.
- `artifacts/theme-activation/baseline-identity.json`: verified historical and installed identities.
- `artifacts/theme-activation/native-child-identity.json`: direct child and TMP identities in both PC scenes.
- `artifacts/theme-activation/native-skills-phase-il.txt` and `native-phase-update-il.txt`: actual retry lifecycle.
- `artifacts/theme-activation/candidate-qualification.txt`: complete final clean build/test/package transcript.
- `artifacts/candidates/c06ef756cf69d25c052da9ab3802215815a9e512/`: DLL, ZIP, manifests, source/build/native qualification records.
- `artifacts/theme-activation/runtime-c06ef75/ownership.json`: final candidate/process identity and restored state.
- `artifacts/theme-activation/runtime-c06ef75/startup-dice-roller.txt`: candidate and probe loading, scoped startup messages.
- `artifacts/theme-activation/runtime-c06ef75/desktop-blocker.json`: disconnected session and failed foreground guard.
- `artifacts/theme-activation/runtime-c06ef75/UMM-runtime-log.txt` and `output-runtime-log.txt`: archived actual game logs, not general modpack investigation.
- `artifacts/theme-activation/runtime-c06ef75/restoration.json`: file/settings/preferences/save integrity checks.
- `artifacts/theme-activation/runtime-c06ef75/saves-before.json` and `saves-after.json`: local read-only save identity inventory.

No native/visual screenshots exist for this attempt. With an interactive desktop,
reinstall the exact candidate under a fresh guarded ownership/backup record and
complete `docs/NATIVE-THEME-RUNTIME.md`, especially real production resolution,
controlled Unity fixtures, both resolution captures, button/audio interaction,
and both INT/skill-navigation regressions. Do not equate startup or disappearing
warnings with a functioning native reskin. Mercenary/respec and persistence
remain separate fixture-dependent lanes.
