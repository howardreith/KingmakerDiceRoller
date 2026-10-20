# Project state

## 0.2.0 stable release preparation

On 2026-10-19 the owner requested merging the native stats UI mission branch
into the GitHub default branch and publishing a stable `0.2.0` release. The
0.1.17 testing alpha is the behavioral basis; the release preparation changes
version metadata, release documentation and source validation only. The
latest alpha's page-return badge fix and its reproducible smoke scenario
remain included. The development agent has not completed in-game visual,
save/reload or compatibility qualification, so runtime qualification remains
**No**. The owner specifically authorized a stable release; the guarded
publisher's pre-runtime path preserves that distinction. It must run full
qualification on the final default-branch commit.

## Qualification truth

- Implemented: **Yes** — native stats UI work and 0.2.0 version metadata.
- Source-qualified: **Yes** — the 0.2.0 preparation run passed release-gate
  tests 20/20, repository validation 18 groups, the Python oracle 30/30 and
  deterministic C# behavior cases 416/416.
- Contract-qualified: **Yes** — installed Kingmaker 2.1.7b read-only
  contracts, respec checks 11/11 and native UI IL/contracts 30/30 passed.
- Build-qualified: **Yes** — 0.2.0 Release build succeeded with zero warnings
  and zero errors; DLL SHA-256
  `cd3b60233f348bf634e76bdea44031952700054ab0f82766ebda51342823b3a5`.
- Package-qualified: **Yes** — strict validation passed for the six-entry
  preparation ZIP `artifacts/packages/KingmakerDiceRoller-0.2.0.zip` (SHA-256
  `43f99fabcdd31192ecd5deb46533c2004fa64b59a432c4b094b85290f6dadaad`).
  The guarded publisher must regenerate and validate the final ZIP from the
  exact merge commit; this preparation ZIP is not the release asset.
- Installed: **No** — the development agent has not installed 0.2.0 in game.
- Focused runtime test: **NOT RUN** — live visual, input and save/reload checks.
- Runtime-qualified: **No**.
- Compatibility-qualified: **No**.
- Release-authorized: **Yes** — the owner requested merge to default and a
  stable 0.2.0 GitHub release on 2026-10-19.
- Owner-authorized pre-runtime release: **Yes** — the owner authorized stable
  v0.2.0 despite the unverified runtime checks; no runtime PASS is recorded.
- Testing-prerelease-authorized: **No** — the requested release is stable.
- Publicly released: **No** — pending merge, default-branch qualification and
  publication.

## 0.1.17 Value badge color alpha

The owner reported that the red Value badge's numeral turns gray after
rolling, navigating to an earlier character-build page, then returning to
the ability page. Installed Kingmaker 2.1.7b IL shows
`CharBAbilityScoresAllocator.FillData()` calls `SetupVisualMarks()`, which
sets the native Points numeral to `m_TextDisableColor` when the allocator is
complete. The mod copied that live numeral color into its owned badge each
time it reattached. The owned numeral and caption now take the allocator's
exact `m_TextAvailibalePointsColor` and `m_LabelAvailibalePointsColor`
instead, leaving the native color transitions untouched. The owner explicitly
requested a new GitHub alpha after candidate qualification. A testing
prerelease is authorized; stable or runtime-qualified publication is not.
The published v0.1.16 alpha is unchanged. See
`docs/VALUE-BADGE-COLOR-0.1.17-CANDIDATE.md` for qualification and the
remaining live check.
Clean implementation commit `2f480e0f3c55986756d761e5d793aeb04582e771`
passed `Qualify.ps1 -Build -Package -Candidate`: release gate 20/20,
deterministic C# runner 416/416, installed Kingmaker contracts, 11 respec
checks, 30 native UI IL/contracts, zero-warning Release build, and strict
package validation. The installable ZIP is
`artifacts/candidates/2f480e0f3c55986756d761e5d793aeb04582e771/KingmakerDiceRoller-0.1.17.zip`
(SHA-256 `1ea0856d5440e20390ca734093c051990ef54dbf006616d64c851a87df84f213`).
The Windows computer-use process exited before window discovery, so the
rendered page-return badge remains **NOT RUN**.
The owner's earlier instruction says the full release test suite need not be
rerun for alpha publication.
The annotated `v0.1.17` tag points to release documentation commit
`f1493281ad19422fd7fc948a6bf5a95426d9e5c2` on the mission branch.
The [GitHub testing prerelease](https://github.com/howardreith/KingmakerDiceRoller/releases/tag/v0.1.17)
is published with the exact qualified ZIP and `SHA256SUMS.txt`; GitHub reports
ZIP SHA-256 `1ea0856d5440e20390ca734093c051990ef54dbf006616d64c851a87df84f213`.
A fresh Release rebuild reproduced the qualified DLL byte for byte and strict
ZIP validation passed immediately before publication. The full suite was not
rerun for publication. v0.1.11 remains latest stable.

## 0.1.16 Value badge correction candidate

The owner corrected the intended meaning of the native Value badge: in Roll
Mode it should display the point-buy equivalent of the six base scores, not
their arithmetic sum. The snapshot already calculates both; the owned badge
now formats `PointBuyEquivalent` while Details retains both numbers. The
Point Buy badge remains fully native. Version `0.1.16` was qualified as a
review candidate because v0.1.15 was already published. The owner then
explicitly requested another GitHub alpha. Publishing `v0.1.16` as a testing
prerelease is authorized; stable or runtime-qualified publication is not.
The published v0.1.15 alpha remains unchanged. See
`docs/VALUE-BADGE-0.1.16-CANDIDATE.md` for qualification and the remaining
live visual check.
Clean implementation commit `8e9d60d00a232e327c4207317ce4cdee8b7ee49a`
passed `Qualify.ps1 -Build -Package -Candidate`: release gate 20/20,
deterministic C# runner 416/416, installed Kingmaker contracts, 11 respec
checks, 30 native UI IL/contracts, zero-warning Release build, and strict
package validation. The installable ZIP is
`artifacts/candidates/8e9d60d00a232e327c4207317ce4cdee8b7ee49a/KingmakerDiceRoller-0.1.16.zip`
(SHA-256 `174e21f2c2ef0f65f0dbe66a8619dc3873f37daf49b504adce94e0ec57eb03ca`).
The Windows computer-use process exited before window discovery, so the
rendered badge remains **NOT RUN**.
The owner's earlier instruction says the full release test suite need not be
rerun for alpha publication.
The annotated `v0.1.16` tag points to release documentation commit
`98b394cf33bf44b71ff7c8af7a6e8f496ee26f52` on the mission branch.
The [GitHub testing prerelease](https://github.com/howardreith/KingmakerDiceRoller/releases/tag/v0.1.16)
is published with the exact qualified ZIP and `SHA256SUMS.txt`; GitHub reports
ZIP SHA-256 `174e21f2c2ef0f65f0dbe66a8619dc3873f37daf49b504adce94e0ec57eb03ca`.
A fresh Release rebuild reproduced the qualified DLL byte for byte and strict
ZIP validation passed immediately before publication. The full suite was not
rerun for publication. v0.1.11 remains latest stable.

## 0.1.15 native-stat UI v4 review candidate

Candidate version `0.1.15` continues draft PR #7 on
`codex/native-stat-ui-v2`. The v4 mission targets intermittent Reroll
failure, the Value numeral, owned notice placement, complete racial-selector
clearance, formula tooltip triggers, and secondary-control tooltip removal.
The implementation captures native skill and spell allowance state before a
candidate, verifies it after rollback, admits commands before RNG use, and
quarantines incomplete recovery. Native exceptions now retain their inner
stage and stack in diagnostics. The original intermittent in-game failure
cause is unproven because no live run or log is available.

The owner requested v4 fixes and a reviewable candidate, then explicitly
requested another alpha release. Publishing `v0.1.15` as a testing prerelease
is authorized; stable or runtime-qualified publication is not. The published
0.1.14 testing alpha remains unchanged.
Clean implementation commit `b3fa9d07042c9946af71eb8aeaa562ff94905944`
passed `Qualify.ps1 -Build -Package -Candidate`: release gate 20/20,
deterministic runner 415/415, installed respec contracts 11 checks, native
UI IL/contracts 30/30, and a zero-warning Release build. The installable ZIP
is `artifacts/candidates/b3fa9d07042c9946af71eb8aeaa562ff94905944/KingmakerDiceRoller-0.1.15.zip`
(SHA-256 `e1c06ad23f290d735285227a487483dcf8a0aa5d33be738c46f48b039c63eac6`).
See `docs/NATIVE-STAT-UI-V4-CANDIDATE.md` for the six-item acceptance record.
The Windows computer-use helper failed before window discovery again;
actual-game visual, input, and compatibility checks remain **NOT RUN**.
The owner said the full release test suite need not be rerun for this alpha.
The annotated `v0.1.15` tag points to release documentation commit
`73d18da6bf36fa4c785c7376f0e310a8643afd0c` on the mission branch.
The [GitHub testing prerelease](https://github.com/howardreith/KingmakerDiceRoller/releases/tag/v0.1.15)
is published with the validated ZIP and `SHA256SUMS.txt`; GitHub reports ZIP
SHA-256 `e1c06ad23f290d735285227a487483dcf8a0aa5d33be738c46f48b039c63eac6`.
A fresh zero-warning Release rebuild reproduced the qualified DLL byte for
byte, and strict ZIP validation passed immediately before publication. The
full suite was not rerun for publication. v0.1.11 remains latest stable.

## 0.1.14 native-stat UI presentation alpha

Candidate version `0.1.14` continues draft PR #7 on
`codex/native-stat-ui-v2`. It responds to the owner's v3 mission: centered
formula selector and visible native chevron; full native ability badge;
downward right-column icons; one measured, nonblocking native-style tooltip
overlay; exact score-row cost presentation concealment; and a single compact
Previous/Next/Return row with final-content measurement for Human.

The 0.1.13 testing prerelease remains published and unchanged. Clean
implementation commit `252ff2f435476bdc74d051cf09358250dd3e204c`
passed `Qualify.ps1 -Build -Package -Candidate`: release gate 20/20,
deterministic runner 413/413, installed respec contracts 11 groups, native UI
IL/contracts 30/30, and a zero-warning Release build. The installable ZIP is
`artifacts/candidates/252ff2f435476bdc74d051cf09358250dd3e204c/KingmakerDiceRoller-0.1.14.zip`
(SHA-256 `95603893a14a0b6538977070e893603ab1328f556c7893fe70d5229fbfca6d1a`).
The exact artifact and six-item acceptance record are in
`docs/NATIVE-STAT-UI-V3-CANDIDATE.md`. The Windows computer-use helper failed
during initialization, before window discovery; actual-game visual and input
checks remain **NOT RUN**. On 2026-10-19 the owner requested another alpha
release of this version. The earlier instruction to build and publish an alpha
without rerunning the full release suite remains in effect. Testing-alpha
publication is **authorized**; stable or runtime-qualified publication is not.
The annotated `v0.1.14` tag points to
`a45e5336add15dfc45aea249ee2b6d7c5e78c142` on the mission branch. The
[GitHub testing prerelease](https://github.com/howardreith/KingmakerDiceRoller/releases/tag/v0.1.14)
is published with the validated ZIP and `SHA256SUMS.txt`; GitHub reports ZIP
SHA-256 `95603893a14a0b6538977070e893603ab1328f556c7893fe70d5229fbfca6d1a`.
A fresh zero-warning Release rebuild and strict package validation reproduced
the qualified candidate ZIP and DLL byte for byte. The full suite was not
rerun for publication, per the owner's alpha instruction. v0.1.11 remains
latest stable. Actual-game visuals, input, and compatibility remain **NOT RUN**.

## 0.1.13 revised native-stat UI candidate

Candidate version `0.1.13` on branch `codex/native-stat-ui-v2` starts from the
published 0.1.12 alpha record.
This revision makes ordinary Point Buy the single-button default, adds a
separate Roll-ready view, binds the ability allocator's exact Value badge,
uses one right-hand downward swap column, and moves low-score controls to UMM
settings. Recent-roll navigation directly applies bounded history entries;
saved recalls append one entry. The raw formula draft is validated without
random consumption before generation. The published 0.1.12 alpha remains
unchanged.

- Final implementation commit: `46a1f2c5fdf236ef48dc4e5ffb4a78316d6bcf05`.
- Source-qualified: **Yes** — source oracle, repository validation, and
  deterministic runner passed on the implementation commit.
- Contract-qualified: **Yes** — installed 2.1.7b contract resolver, 11 respec
  groups, and 30 native UI IL/contracts passed.
- Build-qualified: **Yes** — Release build had zero warnings/errors.
- Package-qualified: **Yes** — `Qualify.ps1 -Build -Package -Candidate` passed.
  The ignored ZIP is
  `artifacts/candidates/46a1f2c5fdf236ef48dc4e5ffb4a78316d6bcf05/KingmakerDiceRoller-0.1.13.zip`
  (SHA-256 `e22827efea6450a089da8e4582a5fb7b82de3909c4d5b884e6850c9729067667`).
- Actual game visuals and input: **NOT RUN** for 0.1.13. The computer-use
  helper exited before it could list windows on two attempts. No after
  screenshots or in-game click/input observations were obtained.
- Compatibility sweep: **NOT RUN** for 0.1.13.
- Stable/official release: **No**; this is a testing prerelease.
- Testing-alpha publication authorized: **Yes** — on 2026-10-19 the owner
  requested another alpha release of the new version. The earlier instruction
  to build and publish an alpha without rerunning the full release suite
  remains in effect. This does not authorize a stable or runtime-qualified
  release.
- Alpha-published: **Yes** — the annotated `v0.1.13` tag points to
  `528b827e198769e4818d65f637a1ff1a4d4b0b9a` on the v2 feature branch.
  The [GitHub prerelease](https://github.com/howardreith/KingmakerDiceRoller/releases/tag/v0.1.13)
  contains the validated ZIP and `SHA256SUMS.txt`; GitHub reports the ZIP's
  SHA-256 as `e22827efea6450a089da8e4582a5fb7b82de3909c4d5b884e6850c9729067667`.
  A fresh Release build and package validation passed immediately before
  publication, and the ZIP/DLL matched the fully qualified implementation
  candidate byte-for-byte. The full suite was not rerun for publication, per
  the owner's alpha instruction. v0.1.11 remains latest stable. No merge,
  stable release, or Nexus upload occurred. Runtime/visual and compatibility
  qualification remain **No**.
See `docs/NATIVE-STAT-UI-V2-CANDIDATE.md` for the acceptance evidence and
remaining live checks. [Draft PR #7](https://github.com/howardreith/KingmakerDiceRoller/pull/7)
reviews this revision against the existing 0.1.12 alpha branch.

## 0.1.12 inline native-stat UI alpha

Mission branch `codex/inline-native-stat-ui` carries version `0.1.12` and starts from published v0.1.11
record `ee49f10`. The unchanged UMM ID, assembly, saved-array schema, and
starting-score ownership policy remain authoritative. The candidate uses the
native six score rows as the primary score display, adds separate owned
vertical swap arrows, and inserts a two-row Method/Roll/Save/Load/Point Buy/
Options/Details strip after the native stat container. Preset selection is
direct; Load exposes session History and persistent Saved arrays from Point
Buy. The earlier drawer remains only when exact row/layout attachment fails.

Installed 2.1.7b IL verifies that `CharBAbilityScoresAllocator.Init()` assigns
the inherited `CharSScoreEntry.Type` from `m_Types`; the host maps six unique
keys against the existing verified ability keys. The PC main-menu and in-game
scene metadata show 600-by-50 score rows, 34-by-34 native arrow buttons, and
the stat container followed by the racial selector. The new view checks live
rect bounds before accepting inline attachment. It does not infer row identity
from order, text, or score values.

- Implemented: **Yes, as a review candidate** — source, UI, tests, and docs.
- Source-qualified: **Yes** — the project-owned deterministic and source gates
  pass (see `docs/NATIVE-STAT-UI-CANDIDATE.md` for the exact final candidate).
- Contract-qualified: **Yes** — installed Kingmaker and native UI contract
  checks pass; this does not prove a rendered screen.
- Build-qualified: **Yes** — Release build has zero warnings/errors.
- Package-qualified: **Yes** — `Qualify.ps1 -Build -Package -Candidate`
  passed on clean implementation commit `8b8ce349e029e6c68bf52a860a37dd75e5294f6c`.
  The ignored candidate ZIP is `artifacts/candidates/8b8ce349e029e6c68bf52a860a37dd75e5294f6c/KingmakerDiceRoller-0.1.12.zip`
  (SHA-256 `53e1344d0bd1d93b52f7fadf66a20c64993363462e432d7c74302518940080c0`;
  DLL SHA-256 `d17d9f866dbb0e3b7134ee3cbef6697064f13627b401a8b6c51b36054ce2a53a`).
- Installed: **No** — the user's installed mod was not changed.
- Runtime/visual-qualified: **No** — actual-game frames and interactions are
  NOT RUN. The computer-use helper exited unexpectedly twice before window
  inventory; no game UI was observed or driven.
- Compatibility-qualified: **No** — no new live compatibility sweep.
- Alpha-published: **Yes** — on 2026-10-19 the owner requested an alpha on
  GitHub for installation on another computer, with a fresh build and without
  rerunning the full release suite. The annotated `v0.1.12` tag points to
  `fff39292b8d8f86988a96d1ac139d1eb51213b24` on the mission branch.
  [GitHub release](https://github.com/howardreith/KingmakerDiceRoller/releases/tag/v0.1.12)
  is a published prerelease; v0.1.11 remains latest stable. The uploaded ZIP
  digest matches the validated candidate SHA-256 above. A fresh Release build
  had zero warnings/errors and `Package.ps1` validated the identical ZIP;
  the full suite was not rerun for this publication. No merge, stable release,
  or Nexus upload occurred. Runtime/visual and compatibility qualification
  remain **No**.


## Current 0.1.11 settings-panel cleanup release

Version `0.1.11`; branch `z/umm-panel-simplification`, based on `main` at
`8c57c1e` (post-v0.1.10 publication record). v0.1.10 is published and
distributed, so this presentation change takes the next unused patch version.
The owner asked for a public-release-appropriate Unity Mod Manager panel without
changing rolling, the native UI or safety behavior. After reviewing the
candidate, on 2026-10-15 the owner instructed: "Go ahead and release the new
version and increment the version number please." That instruction
authorizes merging, pushing, tagging and publishing 0.1.11, which is the
increment over the published 0.1.10, as a stable GitHub release without an
additional in-game test. Bumping to 0.1.12 would skip an unpublished number.
It does not cover a Nexus upload or a permanent installation.

- Default panel: one sentence on where roll controls appear (new main
  character, mercenary recruitment, respec of all six starting scores), one
  status line, concise notices only for current problems, **Return to Point
  Buy** only while the active session is in a non-Point Buy mode, and a
  collapsed, non-persisted **Show troubleshooting** section. The duplicate
  name/version heading is removed.
- Status is derived from structured state, not status text.
  `RuntimeDiagnostics` records activation (Enabled, EnableFailed, Disabled),
  a refused disable and the last finalization verdict. The coordinator
  records the latest Return to Point Buy outcome (Failed, derived values not
  refreshed, page not refreshed). That outcome applies only while the same
  session keeps the same assignment revision. Readiness additionally requires
  resolved contracts. Routine rejected contexts never become notices.
- Return to Point Buy keeps the unchanged `CanRestorePointBuy` eligibility and
  `TryRestorePointBuy` checks. A session stuck in `RestoringPointBuy` or
  `EnteringRollMode` shows the disabled action with its reason instead of
  hiding it.
- `CompatibilityDetector` keeps its detection and detail/log text and now also
  returns the mod name. The panel shows "Compatibility with ... is unverified.
  See Troubleshooting."; the full text, detected mods and flags move into
  Troubleshooting.
- `SettingsView` renders a Unity-free `SettingsPanel` model and rebuilds it on
  IMGUI Layout events. The old view drew extra rows in the same pass that
  flipped the verbose toggle.
- Unchanged: `Settings.xml` fields and defaults (verbose logging still defaults
  on and keeps saved values), saved-array schema 2, all patches, roll logic,
  context policy, ownership checks, native UI, branding and identifiers. The
  Nexus copy now describes 0.1.11.

## Qualification truth (0.1.11 historical)

- Implemented: **Yes** — UMM panel presentation, structured panel state and
  documentation.
- Source-qualified: **Yes** — 397/397 C# cases (383 existing plus 14 panel
  cases driving the real coordinator, policy, session and restore paths),
  30/30 Python cases, 20/20 release-gate cases and 18/18 repository
  validation groups. Five deliberate presenter/coordinator mutations were each
  caught by the intended panel test: recovery gated on Roll Mode only, a
  dropped application-failure notice, raw status text in the default view, an
  unscoped Return outcome, and rejected contexts promoted to a warning.
- Contract-qualified: **Yes** — existing Kingmaker contracts, 11 respec groups
  and 30/30 native UI IL checks against the installed 2.1.7b assemblies
  (read-only).
- Build-qualified: **Yes** — Release build, zero warnings/errors.
- Package-qualified: **Yes** — `Qualify.ps1 -Build -Package -Candidate` on the
  clean implementation commit `b669400ea16affddd6bcde1421a5572841e4f178`
  re-ran every check above and produced
  `artifacts/candidates/b669400ea16affddd6bcde1421a5572841e4f178/KingmakerDiceRoller-0.1.11.zip`
  (SHA-256 `7d5213cc5226174801bfc97aed406d73b50da9b9fa125b6757f55627dc48e96f`,
  DLL SHA-256
  `6d39e0b69b1f915f27a2b1aa64c3a8070cd4d71038127550112e52f603fcd808`). It
  passed strict validation: six allowlisted files, branded name/author,
  version 0.1.11, and the unchanged ID, assembly name and entry method. The
  release-preparation commit changes only documentation, including the
  packaged README release status. The guarded publisher re-runs build and
  package qualification on the merge commit and records the released ZIP's
  SHA-256 in its checksum asset.
- Installed: **No** — no game installation, launch or saved-game access. This
  machine still has 0.1.6 installed, untouched.
- Focused runtime test: **NOT RUN** — rendered UMM panel inspection is
  blocked. `CopyFromScreen` fails with "The handle is invalid" from the agent
  session even though the RDP session is listed as Active, and no
  desktop-control tool is available. Blind input and guard bypass are not
  permitted. A startup-only launch cannot reach the changed IMGUI draw path,
  so none was attempted
  (`artifacts/umm-panel/runtime-b669400/desktop-blocker.json`, ignored).
  `docs/SMOKE-TEST.md` section K lists the manual panel check.
- Runtime-qualified: **No**.
- Compatibility-qualified: **No**.
- Release-authorized: **Yes** — the owner's 2026-10-15 instruction quoted
  above.
- Owner-authorized pre-runtime release: **Yes** — the owner authorized stable
  v0.1.11 without an in-game test on 2026-10-15; no runtime PASS is recorded.
- Testing-prerelease-authorized: **No** — the owner requested a release.
- Publicly released: **Yes** — published 2026-10-16T01:00:37Z (2026-10-15
  local) by the guarded publisher (`-ConfirmOwnerAuthorizedPreRuntimeRelease`)
  from a clean release clone. It is stable and latest:
  https://github.com/howardreith/KingmakerDiceRoller/releases/tag/v0.1.11,
  built from PR #4 merge `69b78b64350e1d3914bb70d89f67454c1c961751`, which the
  owner merged. Annotated tag `v0.1.11` (`d89d5f3`) peels to that commit, whose
  tree is identical to the qualified head `dbcc2a2`. On the merge commit the
  publisher's `Qualify.ps1 -Build -Package` ran the release gate, source
  validation, the Python oracle, C# 397/397, read-only Kingmaker contracts,
  respec 11/11 and native UI IL 30/30 checks with 0 warnings/errors and strict
  package validation. Asset `KingmakerDiceRoller-0.1.11.zip` SHA-256
  `805c2be8d4f18504a4c368dc911177784d2ef86e07b3e705ea2d1d4fdacf2d40`; DLL
  SHA-256 `6d39e0b69b1f915f27a2b1aa64c3a8070cd4d71038127550112e52f603fcd808`.
  The downloaded asset matched byte-for-byte the publisher artifact,
  `SHA256SUMS.txt`, GitHub's asset digest and the pre-merge `dbcc2a2`
  candidate ZIP. It has the six allowlisted entries, the branded 0.1.11
  manifest and the final README. Runtime-qualified remains **No**; Nexus Mods
  upload was not performed.

## Published 0.1.10 Mr. Bard branding release

Version `0.1.10`; branch `codex/mr-bard-branding`, based on `main` at
`f185603` (post-v0.1.9 publication record). The owner moved the public
branding to the modding identity **Mr. Bard** before Nexus Mods publication,
without changing technical identity or behavior, and on 2026-10-15 authorized
merging, pushing, tagging and publishing 0.1.10 as a stable GitHub release
without an additional in-game test.

- UMM `DisplayName` **Mr. Bard's Kingmaker Roll For Stats UI** and `Author`
  **Mr. Bard**; `ProductMetadata.DisplayName` drives the UMM settings-panel
  title; assembly title/product carry the public name and the assembly company
  is **Mr. Bard**. Two UMM-panel status/warning strings no longer call the mod
  "Dice Roller"; log lines keep the technical name.
- Unchanged: UMM ID `KingmakerDiceRoller`, `KingmakerDiceRoller.dll`,
  `KingmakerDiceRoller.Main.Load`, namespaces/classes, Harmony ID
  `howardreith.kingmakerdiceroller`, ZIP folder and six-file allowlist,
  `Settings.xml` fields and saved-array schema 2, all patches and roll logic.
  LICENSE, THIRD-PARTY-NOTICES and the upstream MIT text are unchanged; the
  assembly copyright keeps its holder and year, with only the mis-encoded
  copyright sign repaired.
- The version advances to `0.1.10` so changed bytes never reuse `0.1.9`.
  Persistence carries no product version (only saved-array schema 2), so the
  bump has no settings consequence.
- The guarded publisher gained `-ConfirmOwnerAuthorizedPreRuntimeRelease`: an
  explicit stable-release disposition for owner approval before runtime
  testing, separate from `-ConfirmRuntimeQualified`. It never records runtime
  evidence, and its generated notes say the release is not runtime-qualified.

## 0.1.10 qualification truth

- Implemented: **Yes** — branding metadata, settings-panel title, validation,
  publisher disposition and documentation only.
- Source-qualified: **Yes** — 383/383 C# cases, 30/30 Python cases, 20/20
  release-gate cases and 18/18 repository validation groups on the final
  branch candidate, including display-name/author, above-0.1.9 UMM ordering
  and owner-disposition checks. The unchanged `f185603` baseline also runs
  383/383 C# cases; the 384 recorded for 0.1.9 below is not reproduced by that
  baseline and no test was removed.
- Contract-qualified: **Yes** — existing Kingmaker contracts, 11 respec
  groups and 30/30 native UI IL checks against the installed 2.1.7b
  assemblies (read-only).
- Build-qualified: **Yes** — Release build, zero warnings/errors.
- Package-qualified: **Yes** — the clean branch candidate passed
  `Qualify.ps1 -Build -Package -Candidate` and strict package validation
  (six-file allowlist, branded name/author, unchanged ID, assembly name and
  entry method). The guarded publisher re-runs build and package
  qualification on the merge commit and records the released ZIP's SHA-256
  in its checksum asset and generated notes.
- Published-package comparison: the earlier branding candidate was compared
  with the downloaded v0.1.9 asset
  (`9942c63d106a2b729ddda5d0f454c1d9623c4b6ae04231d74758e79d565d0e38`): same
  six entries under one `KingmakerDiceRoller` folder, LICENSE,
  THIRD-PARTY-NOTICES and the upstream license byte-identical, and a dnlib IL
  comparison differing only in assembly metadata, the added
  `ProductMetadata.DisplayName` constant, version text in two log lines and
  three UMM-panel strings.
- Installed: **No** — no game installation, launch or saved-game access.
- Focused runtime test: **NOT RUN**.
- Runtime-qualified: **No**.
- Compatibility-qualified: **No**.
- Release-authorized: **Yes** — explicit owner instruction of 2026-10-15 to
  merge, push, tag and publish the three Mr. Bard releases.
- Owner-authorized pre-runtime release: **Yes** — the owner authorized stable
  v0.1.10 without an additional in-game test on 2026-10-15; an unperformed
  optional playtest is not a release blocker, and no runtime PASS is recorded.
- Testing-prerelease-authorized: **No** — the owner requested a stable release.
- Publicly released: **Yes** — published 2026-10-15T22:17:33Z by the guarded
  publisher (`-ConfirmOwnerAuthorizedPreRuntimeRelease`) as stable, latest
  https://github.com/howardreith/KingmakerDiceRoller/releases/tag/v0.1.10 from
  PR #3 merge `9c3f1cee0ecd2a50fa545ab79fefa077acc83e62`; annotated tag
  `v0.1.10` peels to that commit. On the merge commit the publisher's
  `Qualify.ps1 -Build -Package` ran the release gate, source validation, the
  Python oracle, C# 383/383, read-only Kingmaker contracts, respec 11/11 and
  native UI IL 30/30 checks with 0 warnings/errors and strict package
  validation. Asset `KingmakerDiceRoller-0.1.10.zip` SHA-256
  `668dc8082921b3bf4ffbcd32a8e8a5504313d2612ea9136dde9549cefc0e0bb0`; DLL
  SHA-256 `6bf2ba1792ebcdc454eaa973ba223c3edeb80d23c059a3bfbed5be225df2b8df`.
  The downloaded asset matched the local artifact and `SHA256SUMS.txt`
  byte-for-byte, with the six allowlisted entries and the branded manifest.
  Runtime-qualified remains **No**; Nexus Mods upload was not performed.

## Published 0.1.9 button caption fit release

Version `0.1.9`; implementation branch `z/fix-button-caption-sizing`, based
on `main` at `01e73df` (post-v0.1.8 publication record). The owner tested
v0.1.8 in play and supplied screenshots: the drawer appears and the
native-themed aesthetic is much improved, but multiple button captions
truncate with ellipses ("STO…", "PREVI…", "REC…", "DEL…", "DO…",
"RETURN TO POINT B…") and the "Low-score rule" caption is cut.

Source inspection confirmed the cause: `CreateButton` derived
`LayoutElement.minWidth`/`preferredWidth` from fixed design constants
(Store 62, Previous 88, Recall/Delete 70, Return 210, Up/Down 64, caption
column 108) without consulting caption text metrics; labels use
`TextOverflowModes.Ellipsis`; and `NativeBookTheme.CopyText` applies the
native font/style/spacings after construction without recalculating width.

Installed-assembly IL inspection verified the measurement basis:
`TMP_Text.preferredWidth` computes unconstrained (large-margin) width through
`CalculatePreferredValues`, which reads the live `m_characterSpacing`,
`m_wordSpacing`, `m_fontStyle` and `m_fontSize`; with autosizing enabled it
uses `fontSizeMax`, which the buttons pin to `BodyFontSize` (18). Style and
text setters dirty the preferred cache through
`TextMeshProUGUI.SetLayoutDirty`, so post-styling re-reads are accurate.

The repair adds a shared caption-fit path: every textual button (including
Close and Roll Stats) reserves
`max(design minimum, measured caption + insets + safety)` in width and height
through its existing `LayoutElement`, re-measured when `ButtonText`/`Body`
capability styling is applied or restored (including delayed recovery) and
when a disclosure caption's text actually changes — never per frame and never
by recreating controls. Selector captions fit their full styled width;
assignment Up/Down pairs share one unified fitted width; and in Compact
layouts the Return-to-Point-Buy control reflows onto a spare row because the
single point-actions row cannot hold the fitted captions (documented by a
deterministic capacity test). The pure arithmetic lives in
`ButtonCaptionFit`; Unity measurement stays in the panel host. The fit guards
`label.font` before reading any TMP property, preserving the 0.1.8
input-binding and bounded-construction behavior.

On 2026-09-17 the owner instructed: "When you have made the fix, please go
ahead and merge to the default branch and cut a new release. I will test it
myself on another computer after the release is pushed. Thank you Z." This
authorizes merging to `main`, pushing, tagging and publishing an official
(not prerelease) `0.1.9`, with the rendered-verification status below
disclosed honestly and the owner's cross-machine test as the acceptance
lane. It does not convert NOT RUN lanes into passed lanes; publication uses
the owner's instruction rather than asserting `-ConfirmRuntimeQualified`, and
the general qualification gate and its tests remain unchanged.

## 0.1.9 qualification truth

- Implemented: **Yes** — measured caption-fit sizing with theme re-measure,
  selector caption fit, Up/Down unification, Compact reflow.
- Source-qualified: **Yes** — 384/384 C# cases (nine new caption-fit cases),
  30/30 Python cases, 13/13 release-gate cases, repository validation.
- Contract-qualified: **Yes** — existing Kingmaker contracts, 11 respec
  groups and 30/30 native UI IL checks, including five new caption-fit
  checks negative-control verified against the released v0.1.8 DLL.
- Build-qualified: **Yes** — Release build, zero warnings/errors.
- Package-qualified: **Yes** — clean committed tree
  `d4a34164a2836619bd39fd364bb832f4f59f8aeb`; candidate ZIP
  `artifacts/candidates/d4a34164a2836619bd39fd364bb832f4f59f8aeb/KingmakerDiceRoller-0.1.9.zip`
  (SHA-256 `9942c63d106a2b729ddda5d0f454c1d9623c4b6ae04231d74758e79d565d0e38`,
  DLL SHA-256
  `185f2b0e9d1e5de2dfe120bfebebeeec94df0b41b4b71107f5075c768a11954f`) passed
  the deterministic package validation.
- Installed: **No** — no game installation or launch was performed for this
  candidate; the interactive desktop remains disconnected (session `Disc`),
  which blocks rendered caption verification without blind input
  (`artifacts/button-fit/runtime-d4a3416/desktop-blocker.json`). A headless
  startup would only re-exercise unchanged initialization and cannot reach
  the Skills page where the sizing code runs.
- Rendered caption verification (agent-executed): **NOT RUN** — the concrete
  blocker above. The probe's live CAPTION reporting is ready for any future
  guarded interactive session.
- Runtime-qualified: **No** — not claimed by publication.
- Compatibility-qualified: **No** — mercenary/respec fixtures unavailable.
- Human visual acceptance: **Pending owner cross-machine test**.
- Audio-qualified: **NOT RUN**.
- Release-authorized: **Yes** — the owner's 2026-09-17 instruction quoted
  above; official full release.
- Testing-prerelease-authorized: **No** — the owner requested a full release.
- Publicly released: **Yes** — [v0.1.9 official/latest](https://github.com/howardreith/KingmakerDiceRoller/releases/tag/v0.1.9),
  published 2026-09-17 21:55:33 UTC; draft=false, prerelease=false, latest
  verified. No new runtime evidence is implied by publication.

## Verified v0.1.9 publication

The mission branch `z/fix-button-caption-sizing` was fast-forward merged into
`main` at `cd7052c5b24f9ca73a28f6e6e1305be57459640b` and pushed, together
with the implementation branch. The existing publisher reran the complete
offline qualification from that clean, fully pushed main (384/384 C# cases,
30/30 Python cases, 13/13 release-gate cases, repository validation, 11
respec contract groups, 30/30 native UI IL checks, zero Release
warnings/errors) and created the draft release in one pass. After downloaded
asset verification, the owner-authorized publication made v0.1.9
official/latest.

Annotated tag object `2939ce5a8f07928f76de93e84acc0310e4e33503` peels to the
exact clean release commit `cd7052c` (= `origin/main` at publication). Release
ID `391084409`. ZIP asset 121,940 bytes; checksum asset 97 bytes.

- ZIP SHA-256: `9942c63d106a2b729ddda5d0f454c1d9623c4b6ae04231d74758e79d565d0e38`.
- DLL SHA-256: `185f2b0e9d1e5de2dfe120bfebebeeec94df0b41b4b71107f5075c768a11954f`
  — byte-identical to the qualified candidate from clean commit `d4a3416`.
- `SHA256SUMS.txt` SHA-256: `13812313d03a96eea19d632a99af3c94c89a8d61a86054cd3aa0d0d9a331a551`
  and carries the same ZIP digest.

Downloaded draft and published bytes, all six package entries, GitHub's asset
digests and the latest-release endpoint all match. The prior v0.1.8/v0.1.7
releases and tags were not modified. Post-publication documentation may
advance main without changing the tagged build or assets.

Publication evidence in the default checkout, outside Git:
`artifacts/release-0.1.9/draft-preparation.txt`, `download-draft/` and
`download-published/`. No game installation, launch or saved-game access
occurred at any point during this release; the rendered-verification blocker
above remains the honest runtime record for this version, and the owner's
cross-machine test is the acceptance lane.

## Historical 0.1.8 roll-panel input initialization repair

Version `0.1.8`; implementation branch `z/fix-roll-panel-input-initialization`,
based on `main` at `54d0814` (post-v0.1.7 documentation). The released v0.1.7
made the entire Roll Stats surface disappear from character creation.
`CreateInput` captured `TMP_InputField.caretColor` fallback styling
immediately after `AddComponent<TMP_InputField>()`, before `textComponent`
was assigned.

Source inspection versus the installed runtime confirmed the cause: the
installed `Assembly-CSharp-firstpass.dll` (`TMPro.TMP_InputField`, MVID
`57f03756-55de-42f5-8bb3-e983306082b2`) implements `get_caretColor` as
`customCaretColor ? m_CaretColor : textComponent.color`, and its constructor
never stores `m_CustomCaretColor`, so a fresh component evaluates the
`textComponent.color` branch against null and throws
`NullReferenceException`. The failure escaped `CreatePanelContent` before
`CreateAccessTab` could run, and the attachment/lifecycle catches detached the
partially constructed view every frame — exactly the two deduplicated warnings
in the reported log. The v0.1.6 code only ever wrote caret properties from a
resolved donor, which is why the regression appeared with the 0.1.7
fallback-capture work (`c06ef75`).

The repair binds `textViewport`, `textComponent` and `placeholder` before any
caret/selection fallback capture and before registering the input theme
binding, preserving caret color, custom caret flag, selection color, blink
rate, placeholder styling, custom-expression editing and theme recovery.
Attachment/lifecycle failure reports now distinguish `(construction)` from
`(rendering)` phases and include the complete exception with stack trace and
inner exceptions while retaining the sixteen-message bounded deduplication.
A deterministic owned-view construction failure is bounded to three attempts
per allocator/controller identity (`NativePanelConstructionBudget`); any
allocator or session-controller identity change reopens construction, and
readiness transients that fail before view construction are not counted.
Theme recovery failures also report the complete exception.

On 2026-09-17, after the candidate handoff, the owner instructed: "Could you
go ahead and make this a real release? Go ahead and merge to master and do a
new full release increasing the number by 1. I will test it when I can,
probably tonight. Thank you." This explicitly authorizes merging to the
default branch and publishing an official full release at `0.1.8`, with the
unrun interactive checks disclosed and the owner performing in-play
acceptance. It does not convert the NOT RUN lanes below into passed lanes;
official publication again uses the owner's instruction rather than asserting
`-ConfirmRuntimeQualified`, and the general qualification gate and its tests
remain unchanged.

## 0.1.8 qualification truth

- Implemented: **Yes** — input dependency binding order, phased complete
  exception reporting, bounded per-identity construction retry.
- Source-qualified: **Yes** — 375/375 C# cases (including five new
  construction-budget cases), 30/30 Python cases, 13/13 release-gate cases and
  the repository source validation passed.
- Contract-qualified: **Yes** — existing Kingmaker contracts, 11 respec groups
  and 25/25 native UI IL checks, including three new checks: the installed TMP
  `caretColor` getter semantics, the candidate `CreateInput` binding order
  (negative-control verified to fail against the official v0.1.7 DLL), and
  construction-budget wiring.
- Build-qualified: **Yes** — Release build, zero warnings/errors.
- Package-qualified: **Yes** — clean committed tree
  `6729f96168d90e0a07a9c29572634ae7c0e13396`; candidate ZIP
  `artifacts/candidates/6729f96168d90e0a07a9c29572634ae7c0e13396/KingmakerDiceRoller-0.1.8.zip`
  (SHA-256 `729fc6dbb64975c2dba0e43a838e6ecfd9b7abdd150406d33a42738af4185b2e`,
  DLL SHA-256
  `43ee3d93a239c6b503ad446c744ca92eaae977fc3522f0199311396ef9129267`) passed
  the deterministic package validation.
- Installed: **No** currently — the exact candidate was temporarily installed
  through the guarded installer (after a `-WhatIf` dry run) and loaded in owned
  PID 9796; the original mod/settings were then restored and hash-verified.
- Temporary installation/startup: **PASS, limited** — UMM log records 0.1.8
  loading, enabling and `Active` with the lab probe, with no Dice Roller
  startup exceptions.
- Focused runtime test: **NOT RUN** — the Windows session was disconnected
  (`Disc`; console `Conn`), screen capture failed and Unity exposes no
  accessibility elements; the mission forbids blind input, so no genuine
  Skills entry, drawer opening, custom-expression roll, reopen/navigation or
  adjacent roll/assign/Point Buy/INT check occurred. Evidence:
  `artifacts/input-initialization/runtime-6729f96/` (ignored by Git).
- Runtime-qualified: **No**.
- Compatibility-qualified: **No** — mercenary/respec fixtures unavailable.
- Human visual acceptance: **Pending owner in-play test** — the owner stated
  they will test this release, probably tonight; no separate record exists yet.
- Audio-qualified: **NOT RUN**.
- Release-authorized: **Yes** — the owner's 2026-09-17 instruction quoted
  above; official full release at the next version, published with the unrun
  interactive checks disclosed.
- Testing-prerelease-authorized: **No** — the owner requested a real release.
- Publicly released: **Yes** — [v0.1.8 official/latest](https://github.com/howardreith/KingmakerDiceRoller/releases/tag/v0.1.8),
  published 2026-09-17 21:06:08 UTC; draft=false, prerelease=false, latest
  verified. No new runtime evidence is implied by publication.

## Verified v0.1.8 publication

The mission branch `z/fix-roll-panel-input-initialization` was fast-forward
merged into `main` at `f63a95eee3298d96fd35d20f2c45203dfe09eb45` and pushed,
together with the implementation branch. The existing publisher reran the
complete offline qualification from that clean, fully pushed main (375/375 C#
cases, 30/30 Python cases, 13/13 release-gate cases, repository validation,
11 respec contract groups, 25/25 native UI IL checks, zero Release
warnings/errors) and prepared a draft release. A transient GitHub HTTP 500
during the first ZIP upload was resolved by the publisher's idempotent
draft-upload path; only one draft was ever created. After downloaded asset
verification, the owner-authorized publication made v0.1.8 official/latest.

Annotated tag object `abc73d37558a741c50ea8b85c0fab145ada7800d` peels to the
exact clean release commit `f63a95e` (= `origin/main`). Release ID `391059843`.
ZIP asset `571135224` (`KingmakerDiceRoller-0.1.8.zip`, 120,701 bytes); checksum
asset `571135185` (`SHA256SUMS.txt`, 97 bytes).

- ZIP SHA-256: `ab837f979144c79153bd4e04cbd005990e2c34e628a5f78fa4579e8277f1858f`.
- DLL SHA-256: `43ee3d93a239c6b503ad446c744ca92eaae977fc3522f0199311396ef9129267`
  — byte-identical to the review candidate that the guarded owned game process
  loaded during the runtime attempt.
- `SHA256SUMS.txt` SHA-256: `51bc7e903b7720d787022186af392ef8761d69503fc282f596f56083f61bae11`
  and carries the same ZIP digest.

Downloaded draft and published bytes, all six package entries, GitHub's asset
digests and the latest-release endpoint all match. The ZIP differs from the
review candidate ZIP only in its packaged README, which now describes the
official publication and discloses the runtime blocker; the DLL is unchanged.
The prior v0.1.7 assets and tag were not modified. Post-publication
documentation may advance main without changing the tagged build or assets.

Publication evidence in the default checkout, outside Git:
`artifacts/release-0.1.8/draft-preparation.txt`,
`draft-preparation-retry.txt`, `download-draft/` and `download-published/`.
No game installation, launch or saved-game access occurred during
publication; the guarded runtime attempt evidence above remains the only
runtime record for this version.

## Historical 0.1.7 native-theme activation release

Version `0.1.7`; branch `codex/native-theme-activation-repair`, based on
`9b69751983a6bc80d1e73999d3fbba3e3e7f8927`. Historical `77d16ef` is an ancestor;
`v0.1.6` peels to `7662a4239512edd9be7a8d3c3ca511a1d9ce763a`. Both the release
ZIP's DLL and the installed pre-repair DLL match
`e2803d3f70ac168d52de27a075b36e6562c7209e709d02c62485f7ded0b13092`.
No reset or downgrade was performed. The owner reported the literal-name donor
failure and initially authorized a repair branch and review PR. On 2026-09-13,
after the candidate handoff, the owner instructed: "Could you please merge,
push, and release this as a new release? Make it an actual release, not a test.
Thank you." This explicitly authorizes merging PR #2, pushing main, tagging
v0.1.7 and publishing an official/latest release with the unrun checks disclosed.
It supersedes the earlier repair-only publication restriction, without claiming
runtime qualification. The existing draft-build/package workflow prepared
verified assets; official publication used the owner's instruction rather
than asserting `-ConfirmRuntimeQualified`. The general qualification gate and
its tests remain unchanged.

The owner additionally authorized this machine's Kingmaker/Steam installation
for disposable runtime verification. Preserve and restore the existing Dice
Roller installation, own the test process, and use new-character creation without
loading or changing valued campaign saves. Mercenary/respec fixtures have not
been supplied. Prior release permissions below are historical.

## 0.1.7 qualification truth (historical)

- Implemented: **Yes** — literal-name lookup, independent styling capabilities,
  and in-place recovery bounded to initial resolution plus two FillData retries.
- Source-qualified: **Yes** — 370 C# cases, 30 Python cases, 13 release-gate
  cases and 18 source groups passed.
- Contract-qualified: **Yes** — existing contracts, 11 respec groups and
  22 native UI IL checks passed.
- Build-qualified: **Yes** — Release build, zero warnings/errors.
- Package-qualified: **Yes** — clean merged release commit `db36b3a`, six-file
  allowlist and downloaded public ZIP/checksum parity. DLL equals candidate
  `c06ef75`; ZIP differs only by the packaged official-release README.
  Exact hashes/evidence: `CODEX-NATIVE-THEME-ACTIVATION-STATE.md`.
- Installed: **No** currently — exact candidate was temporarily installed and
  loaded in owned PID 11832, then original mod/settings were hash-verified restored.
- Focused runtime test: **NOT RUN** — startup succeeded, but Windows session
  was disconnected with no foreground window; guarded input/capture refused.
- Native themed path verified: **NOT RUN** — no genuine Skills entry or live Resolve.
- Rendered appearance/interactions inspected: **NOT RUN**.
- Runtime-qualified: **No**.
- Compatibility-qualified: **No** — mercenary/respec fixtures unavailable.
- Human visual acceptance: **NOT RUN**.
- Audio-qualified: **NOT RUN**.
- Release-authorized: **Yes** — explicit owner instruction above; official/latest.
- Testing-prerelease-authorized: **No** — the owner requested an actual release.
- Publicly released: **Yes** — [v0.1.7 official/latest](https://github.com/howardreith/KingmakerDiceRoller/releases/tag/v0.1.7),
  published 2026-09-13 15:07:07 UTC; draft=false, prerelease=false, latest verified.
  No new runtime evidence is implied by publication.

## Verified v0.1.7 publication

[PR #2](https://github.com/howardreith/KingmakerDiceRoller/pull/2) merged to
`main` at `db36b3a55da66751478fa5847fb3cf92ee3eb7f0`. Annotated `v0.1.7` tag
object `3f9dac9d3e76e83ba9280227f03e6d964f4e8aec` peels to that exact clean,
fully pushed build commit. The release was prepared through the existing
publisher, downloaded/verified as a draft, then published official/latest under
the explicit owner instruction. The tag and assets were not retargeted/replaced.
Post-publication documentation may advance main without changing release bytes.

Release ID `387935603`; ZIP asset `561406947` (120289 bytes); checksum asset
`561406946` (97 bytes). DLL SHA-256:
`9bd269d9f54f937323ef48285781e4ad67896c06117a4449f8b099fc04ad852d`.
ZIP SHA-256:
`1b3a5ec3be5085359f87c8da5732f8148573b0792072ba567fc995d45ed07cbc`.
Downloaded draft and published bytes, all six package inputs, GitHub asset
digests and checksum contents match. Native themed-path, rendered, audio,
runtime and compatibility statuses above remain unchanged.

Default-checkout evidence is under ignored `artifacts/release-0.1.7/`:
`draft-preparation.txt`, `draft-verification.json`, `published-verification.json`,
GitHub metadata and both download snapshots. The full build again passed all
370 C#/30 Python/13 gate cases, 18 source groups, existing native contracts,
11 respec groups and 22 native UI checks, with zero warnings/errors. No game
installation, launch or saved-game access was performed during publication.

## Historical 0.1.6 native book UI release

Version `0.1.6`; implementation branch `codex/native-book-ui-reskin`.
Baseline `504b5ae53600be99a01f437b35ed57cdff6c3535`. Presentation-only
paper/button/type/message reskin; mechanic and context services unchanged.
Exact donors, offline evidence and remaining gates: `docs/NATIVE-UI-STYLE.md`
and `CODEX-NATIVE-UI-RESKIN-STATE.md`.

On 2026-09-13, after the local candidate handoff, the owner instructed:
"Could you please commit, merge to the default branch, push to remote, and
cut a release? Thank you." This authorizes publication of 0.1.6 and supersedes
the original mission's local-only publication restriction. The owner then
selected "Official/latest, with the unrun checks disclosed" for v0.1.6. The
existing release was promoted accordingly; this explicit publication decision
does not establish runtime qualification. Live acceptance remains NOT RUN.
Installation, desktop/game testing and save modification remain unauthorized.

## Historical 0.1.6 qualification record

- Implemented: **Yes** — full native style with bounded usable fallback.
- Source-qualified: **Yes** — 18 source groups, 13/13 release-gate tests,
  347/347 C# cases, 30/30 Python cases.
- Contract-qualified: **Yes** — existing Kingmaker contracts, 11 respec groups,
  16 installed/candidate UI IL checks; exact donors inspected in both PC scenes.
- Build-qualified: **Yes** — Release, zero warnings/errors.
- Package-qualified: **Yes** — release rebuilt from clean, pushed main and
  validated against the six-file allowlist. Downloaded GitHub ZIP/checksum match
  local bytes and GitHub asset digests; all entries match source/build inputs.
  Released DLL matches the original candidate exactly. Historical candidate
  provenance remains under ignored `artifacts/candidates/<clean-commit>/`.
- Installed: **No** — no active/test game installation by this mission.
- Focused runtime test: **NOT RUN** — guarded disposable workflow not authorized.
- Runtime-qualified: **No** — interactions, workflow/finalization/save regressions NOT RUN.
- Compatibility-qualified: **No** — mercenary/respec/provider/input-method live matrix NOT RUN.
- Human visual acceptance: **NOT RUN** — all twelve references and actual native
  resources inspected; no real candidate captures or motion evidence.
- Audio-qualified: **NOT RUN** — ordinary route source-qualified only.
- Release-authorized: **Yes** — owner's 2026-09-13 release instruction and
  subsequent explicit official/latest selection quoted above.
- Testing-prerelease-authorized: **No** — superseded by the owner's selection
  of official/latest; the initial prerelease publication is historical below.
- Publicly released: **Yes** — `v0.1.6` official/latest (not a prerelease);
  exact artifact hashes and verified promotion below.


## Verified 0.1.6 publication

[v0.1.6](https://github.com/howardreith/KingmakerDiceRoller/releases/tag/v0.1.6) was initially published on
2026-09-13 at 12:56:46 UTC as a testing prerelease and subsequently promoted to
**official/latest** as recorded below. Release ID `387900346`. Implementation and release
preparation were fast-forward merged from `codex/native-book-ui-reskin` into
`main`, then both branches were pushed. The guarded publisher reran the complete
offline qualification from clean, fully pushed main at release commit
`7662a4239512edd9be7a8d3c3ca511a1d9ce763a`. Annotated tag `v0.1.6` is
`4016542b39111fada3e777962d55d40e4092f3d0` and peels to that commit.

All 347 C# cases, 30 Python cases, 13 release-gate tests, 18 source-validation
groups, existing native contracts, 11 respec groups and 16 UI IL checks passed.
Release build: zero warnings/errors. Visual, audio, interactive workflow,
finalization/save and compatibility acceptance remain **NOT RUN**.

- ZIP: `KingmakerDiceRoller-0.1.6.zip`, 113,947 bytes,
  GitHub asset `561218651`.
- ZIP SHA-256: `6e81dde5773b1d23706f57ed96f1f738a28a9cabc955295d5625a21a8911d1f1`.
- DLL SHA-256: `e2803d3f70ac168d52de27a075b36e6562c7209e709d02c62485f7ded0b13092`.
- Checksum asset: `SHA256SUMS.txt`, 97 bytes, GitHub asset `561218652`;
  SHA-256 `bd83cfa1b9888ddf312d8ba757ea7ab8032246940cd86d092e53907092fa761f`.

The published ZIP and checksum were downloaded and compared byte-for-byte with
the locally qualified files and GitHub SHA-256 digests. Every one of the six ZIP
entries matched its source/build input; Info version is 0.1.6 and the DLL is
identical to the original local candidate. The ZIP differs because its README
now describes the release. Historical candidate files and the v0.1.5 ZIP remain
unchanged. The initial publication audit identified v0.1.5 as latest; the
subsequent promotion audit below now identifies v0.1.6.

Evidence remains under ignored `artifacts/release-0.1.6/`: `publication.txt`,
`github-release.json`, `github-latest.json`, `independent-publication-audit.json`,
copied build/package/contract reports and the downloaded assets. This publication
record is a later documentation commit; the release tag and asset bytes remain
fixed at the release commit above. No PR, installation, game/desktop session,
other-mod change or save modification was performed.


## Verified 0.1.6 official/latest promotion

The owner selected "Official/latest, with the unrun checks disclosed" on
2026-09-13. The existing release was promoted using `gh release edit`, retaining
the original build, ZIP, checksum asset and annotated version tag. This explicit
owner decision supersedes the normal requirement to finish live qualification
before official publication for this release; it does not mark those lanes as
passed or change the automated new-release gate.

At 13:19:54 UTC on 2026-09-13, GitHub's latest-release response and the v0.1.6
release response both identified release `387900346`, with `prerelease=false`
and `draft=false`. The title is `Kingmaker Dice Roller v0.1.6`. Both asset IDs,
names, sizes, SHA-256 digests and upload timestamps matched the records above;
the annotated tag object and peeled release commit also matched exactly.

Release notes prominently disclose that visual, audio, runtime and compatibility
acceptance remain **NOT RUN**, and preserve the original release checksum and
commit. The unchanged ZIP's README still says testing prerelease; the release
page and current repository documentation record the promotion. Runtime-qualified
remains **No**. Promotion involved no rebuild, repackaging, installation, game
session or save modification. Repository/source validation and all 13 release-gate
tests pass for the documentation update; the original artifact's complete offline
qualification remains the evidence for its unchanged DLL/package bytes.

Before/after GitHub responses, tag refs, exact posted notes and
`promotion-audit.json` are preserved under ignored
`artifacts/release-0.1.6/promotion/`. Promotion documentation is newer than the
immutable tagged build; no published asset or tag was replaced.


## Historical 0.1.5 candidate — skills-counter and forward-navigation integrity

Current candidate version: `0.1.5` on branch `z/skills-counter-next-guard`
(baseline: `main` at `f9f7cf31`). IL inspection of the installed assembly
confirmed the two follow-up defects: the native red skill-points badge repaints
only through the dirty Skills phase, and the phase-unlock cache gating every
forward route is recomputed only inside `SetupUI` — so a changed rolled
assignment left a stale badge and allowed leaving Skills with an invalid skill
allocation, yielding an empty feats/traits page. The repair adds a bounded,
idempotent native skills-page refresh after every relevant change (including
drawer close) and a veto-only prefix on `CharacterBuildController.SetPhase`
(the single forward funnel) that blocks forward movement beyond Skills while
the live `LevelUpState.IsSkillPointsComplete()` is false. Evidence and the
open live lanes: `Z-SKILLS-COUNTER-STATE.md` and
`docs/CHARACTER-BUILD-INTEGRITY.md`.

After the 0.1.5 candidate was delivered, Howie instructed: "Please commit,
merge, push, and do a release. And make it an actual release, not a test
pre-release like the last one. We can make it 0.1.5 it's fine. No one else is
using this mod yet." That was owner release authorization for an
official (latest, non-prerelease) v0.1.5.

## 0.1.5 qualification record (historical)

- Implemented: **Yes** for new-main, mercenary, and the inspected native/Eddic
  respec path, including the skills-page synchronization and forward guard.
- Source-qualified: **Yes** — repository validation 13/13, 336/336 C# cases,
  30/30 Python cases.
- Contract-qualified: **Yes** — exact native contracts plus the skills-page
  refresh/transition members; eleven respec contract groups.
- Build-qualified: **Yes** — Release, zero warnings/errors, installed
  UMM/Harmony references.
- Package-qualified: **Yes** — deterministic 0.1.5 archive passed the
  six-file allowlist and hash gate.
- Installed: **Not performed by the agent**; owner-side installation only.
- Focused runtime test: **Owner-confirmed for the 0.1.4 rolled-stat workflow**
  (the owner played it and reported only the skills-badge/Next defects); the
  0.1.5 skills-page changes were accepted by the owner for direct official
  release without a testing-prerelease stage.
- Runtime-qualified: **Yes** — owner-directed official release per the
  instruction quoted above; the agent-executed interactive/save/provider
  matrix remains unexecuted and is recorded as NOT RUN below.
- Compatibility-qualified: **No** for the provider/configuration matrix.
- Human visual acceptance: **Owner release instruction** (quoted above); a
  separate in-play confirmation of the 0.1.5 skills-page behavior is still
  welcome and will be recorded when supplied.
- Release-authorized: **Yes** — the owner's instruction quoted above.
- Testing-prerelease-authorized: **Yes** historically; superseded by the
  explicit request for a direct official release.
- Publicly released: **Yes** — `v0.1.5` official (latest).

## Verified 0.1.5 publication

`v0.1.5` was published on 2026-09-11 at 20:33:03 UTC as the **official latest
release** by rerunning the full qualification from clean, fully pushed `main`
release commit `3117dac9606964442b0263ab8b86f72043897dc8` (13/13 repository
gates, 30/30 Python cases, 336/336 C# cases, exact native contracts including
the skills-page members, eleven respec contract groups, zero Release
warnings/errors). GitHub reports `isDraft=false`, `isPrerelease=false`, title
`Kingmaker Dice Roller v0.1.5`, and the latest-release endpoint returns
`v0.1.5`. The annotated tag resolves to the release commit (= `origin/main`).
Asset digests match the qualified local bytes:

- ZIP SHA-256: `b0b33305274785d089fb0f148a1b9e3e2812967070b8d3a00fce4c2f6877741e`
  (`KingmakerDiceRoller-0.1.5.zip`; the packaged README carries the finalized
  release text, so the archive differs from the earlier local-only candidate
  while the DLL is unchanged).
- DLL SHA-256: `591ef6eeaa1d8074ab32f644657f0b6246120b55d637e51b1d26b5353cf042e7`.
- `SHA256SUMS.txt` carries the same ZIP digest.

`v0.1.4` remains available as the prior testing prerelease. The
agent-executed interactive/save/provider matrix remains NOT RUN; the release
standing rests on the owner's explicit instruction recorded above.

## 0.1.4 testing prerelease — rolled-stat build integrity

Current candidate version: `0.1.4`. Branch `z/rolled-stat-build-integrity`
(baseline: released 0.1.3 `main` at `f4eba6d`) repaired the reported
rolled-stat build-integrity defects. Exact
2.1.7b IL inspection confirmed three divergences: stale native derived
allowances after staging (skill points/spell slots cached from the pre-roll
Intelligence), the mercenary authoritative Commit replaying actions against
pre-roll source scores before a late six-value correction (dropping
selections), and unverified new-main completion. The repair adds a shared
`DerivedStateRefreshService` transaction for every supported creation kind and
replaces the mercenary late correction with a one-use pre-replay commit ticket
plus a new-main final-recipient audit. Details and evidence are in
`Z-ROLLED-STAT-INTEGRITY-STATE.md` and `docs/CHARACTER-BUILD-INTEGRITY.md`.

On 2026-09-11, after reviewing the delivered mission, Howie instructed:
"Please finalize, merge, push to remote, and cut a new release." That is the
current owner release authorization. Because the interactive/save/provider
lanes were never executed by the agent, the publication follows the
established 0.1.3 pattern: `v0.1.4` is published as a **testing prerelease**
(not latest) and promotion to official is a metadata-only change awaiting the
owner's in-play confirmation.

## 0.1.4 qualification record (historical)

- Implemented: **Yes** for new-main, mercenary, and the inspected native/Eddic
  respec path.
- Source-qualified: **Yes** — repository validation 13/13, 328/328 C# cases,
  30/30 Python cases.
- Contract-qualified: **Yes** — exact native contracts plus the new
  derived-allowance members; eleven respec contract groups.
- Build-qualified: **Yes** — Release, zero warnings/errors, installed
  UMM/Harmony references.
- Package-qualified: **Yes** — deterministic 0.1.4 archive passed the
  six-file allowlist and hash gate.
- Installed: **Not performed by the agent**; owner-side installation only.
- Focused runtime test: **NOT RUN** — no game launch, save operation, or
  desktop control was authorized during development.
- Runtime-qualified: **No** — interactive, final-character, and persistence
  evidence is absent by design until the guarded section K procedure runs.
- Compatibility-qualified: **No** for the provider/configuration matrix.
- Human visual acceptance: **Pending** — owner confirmation of this prerelease
  in play.
- Release-authorized: **Yes** — the owner's 2026-09-11 instruction quoted
  above.
- Testing-prerelease-authorized: **Yes** — same instruction; prerelease chosen
  because runtime lanes are unexecuted.
- Publicly released: **Yes** — `v0.1.4` testing prerelease (not latest);
  official promotion pending owner in-play confirmation.

## Verified 0.1.4 publication

`v0.1.4` was published on 2026-09-11 at 19:16:31 UTC by rerunning the full
qualification from clean, fully pushed `main` (13/13 repository gates, 30/30
Python cases, 328/328 C# cases, exact native contracts including the new
derived-allowance members, eleven respec contract groups, zero Release
warnings/errors). GitHub reports `isDraft=false`, `isPrerelease=true`, title
`Kingmaker Dice Roller v0.1.4 (testing prerelease)`, and the latest-release
endpoint still returns `v0.1.3`. The annotated tag resolves to release commit
`748350bf9317ef8cb96803a7dbaf33e08fc6166f` (= `origin/main`). Asset digests
match the qualified local bytes:

- ZIP SHA-256: `19dc3b383ff16bffe8f022049032a5dbabc05756b92309255fde7d340ed9cf76`
  (`KingmakerDiceRoller-0.1.4.zip`).
- DLL SHA-256: `68467b41eea8aa0c959457de4de71d52ea053dd7a5558a15069e28e160172e13`.
- `SHA256SUMS.txt` carries the same ZIP digest.

Official promotion is a metadata-only change that reuses these exact package
bytes and awaits the owner's in-play confirmation; until then `v0.1.3` remains
the latest official release and the runtime/compatibility lanes below stay
NOT RUN.

## Current 0.1.3 official release

Version `0.1.3` was initially published as a testing prerelease on 2026-09-07
at 02:13:13 UTC. Howie subsequently confirmed that it works and authorized its
official release. Promotion preserves the same published package and tag.
Implementation is commit `4629adf3202f6d04d547f3a7ec7c805b89a9f221` on
`codex/respec-starting-scores`; publication uses the clean, pushed `main` branch.
The clean DATA checkout began at `bbf145014df0d1b889f811a655d9d183ccc1f087`, the
reviewed historical `0.1.2` main. No newer local source was replaced. The installed
0.1.2 DLL was inspected, not overwritten; its SHA-256 is
`962d5968d5021db2868d39104b4cbfc3911fe1de00ebde85974a1a2b1b977acd`.
The unchanged source baseline built to different bytes
(`1b1c8f12335636aa4e83da06971f2ffc2ec6fbb885bb6608b6e53bdc970754d5`);
same-version metadata was not treated as byte parity.

Howie first authorized publication for testing, then reported:
"Confirmed this work. Please make this a real official release."
This is current owner acceptance of the released 0.1.3 workflow and explicit
authority to promote it to official/latest. It does not enumerate individual
provider/character cases or save/reload results; those lanes are not inferred
from the confirmation. No agent-operated game launch, installation, or save
operation is part of this promotion.

## Confirmed local causes and repair

The installed/source policy explicitly rejected Respec. The separate immutable
Mercenary completion guard also rejected it, and a main-character respec clone
could fail the different-main identity check. The repair preserves recruitment's
finalization guard and adds a distinct scoped Respec lifecycle.

Exact installed Eddic 1.0 and Kingmaker 2.1.7b inspection established the selected
original -> native rebuild source -> preview -> native serialization into the
original entity path. Eddic retains Respec mode and rebuilds story companions
from zero. Native recruitment-level story retraining does not reopen starting
allocation. The native HandleLevelUpStart postfix supplies a fully bound owner
after constructor-time state is incomplete.

The initial assignment now reaches native action checks through the fresh state
inside the exact first-level Commit invocation. Replay is verified without late
rewrites. The exact native copy callback must complete before final recipient
verification; the original entity's current descriptor is reread. The session
closes before catch-up levels and subsequent native ability increases.

A new restoration regression exposed an additional confirmed service defect:
a pre-Roll allocation identical to the rolled array with a full budget was
rejected as a hybrid. It now passes only with independently captured origin
provenance and exact live values/budget/availability/ownership. The prior invalid
hybrid regression remains protected.

The development session did not run an interactive reproduction on DATA. Local
source and installed binary contracts confirmed the admission defect. Howie later
confirmed the released workflow works. That human acceptance is recorded separately
from service tests and does not supply unspecified per-provider or persistence results.

## Baseline and implementation evidence

The unchanged baseline passed the repository gate (8 cases), Python oracle
(30 cases), deterministic C# runner (283 cases), exact native contracts, and
Release compilation with zero warnings/errors. The initial build attempt failed
because PATH resolved Python to the Windows Store alias. Selecting DATA's existing
Python 3.12 directory in the process PATH resolved it; no prerequisite or global
configuration was changed.

The candidate adds 33 respec service cases (316 total), preserving the dice domain
and existing runner. Installed-assembly checks cover eleven contract groups,
including four actual-Harmony scope cases on owned test methods. They use DATA's
UMM 0.33.0, Harmony12 1.2.0.1, and HarmonyLib 2.3.6.0 without replacing libraries.
The scope tests run on desktop .NET, not Unity/Mono. Relevant source files and
replay details are documented in `docs/ARCHITECTURE.md` and
`docs/INTEGRATION-SEAMS.md`.

Native/Eddic selector ownership, negative locked/progression cases, preview
replacement without RNG, modified-budget and previously rolled origins,
pre-action authoritative staging, copy/overwrite detection, duplicate/reentered
commits, cancellation, exceptions, source rollback, disable, and owner loss have
service coverage. Existing layout/control regressions cover the reused panel;
the owner supplied overall workflow acceptance without a per-control visual record.

## 0.1.3 qualification record (historical)

- Implemented: **Yes** for the native PC selector and inspected Eddic 1.0 path.
- Source-qualified: **Yes** — repository validation, 316/316 C# cases, 30/30 Python cases.
- Contract-qualified: **Yes** — exact native contracts plus eleven respec contract groups; four Harmony scope cases.
- Build-qualified: **Yes** — Release, zero warnings/errors, installed UMM/Harmony references.
- Package-qualified: **Yes** - deterministic 0.1.3 archive passed the six-file allowlist and hash gate.
- Installed: **Not performed by the agent**; owner-reported use has no installed-byte parity record.
- Focused runtime test: **Owner-confirmed** for released 0.1.3 usage; detailed scenario results were not supplied.
- Runtime-qualified: **Yes** for the owner-confirmed workflow and release sign-off; the full matrix remains unverified.
- Compatibility-qualified: **No** for the exhaustive provider/character/configuration matrix.
- Human visual acceptance: **Owner sign-off**; no separate per-control screenshot record.
- Release-authorized: **Yes** - current owner instruction explicitly requests official release.
- Testing-prerelease-authorized: **Yes** - historical authorization for the initial 0.1.3 publication.
- Publicly released: **Yes** - `v0.1.3` is official, non-prerelease, and GitHub Latest.

The release package is `artifacts/packages/KingmakerDiceRoller-0.1.3.zip`.
Exact commit, dirty state, DLL/package hashes, contract reports and inventory are
recorded in ignored artifacts by the final qualification commands. No installed
parity or save/reload result is claimed. The installed profile, Steam state,
Cloud, and saves remain untouched.

## Verified publication

Official promotion was verified through GitHub: `isDraft=false`,
`isPrerelease=false`, title `Kingmaker Dice Roller v0.1.3`, and the latest-release
endpoint returns `v0.1.3`. Both asset IDs, SHA-256 digests, sizes, download URLs,
and the peeled annotated tag commit match the pre-promotion record. No package
was rebuilt or uploaded. Repository validation and all 13 publication-gate cases
pass; the stable publication gate accepts the scoped owner runtime sign-off.


[GitHub release v0.1.3](https://github.com/howardreith/KingmakerDiceRoller/releases/tag/v0.1.3)
was initially published as a prerelease. Its annotated tag resolves to release commit
`f9772f06b6d86338a0b22af94b86cbba1cc6d317`. Both implementation and release
finalization were fast-forwarded into `main` and pushed without rewriting history.

The publisher reran qualification from clean, fully pushed `main`: 316/316 C#
cases, 30/30 Python cases, 13/13 publication-gate cases, exact native contracts,
eleven respec contract groups, and zero Release warnings/errors. The public ZIP
and `SHA256SUMS.txt` were downloaded and compared with the qualified local bytes.

- ZIP SHA-256: `a298fa8d21879b9b45de08c8c6e321bd83056959b562801680ca00a24dcc7513`.
- DLL SHA-256: `7f1265aedab4f74c210f4181e8f0458d06710e791eebbcf1b7b2546a523e85d2`.
- DLL bytes match the tested implementation candidate; the ZIP changed because
  its packaged README now contains finalized release/install information.
- The original publication retained `v0.1.2` as latest stable. Official promotion
  changes release metadata only; no release assets or tags are replaced.
- The published ZIP keeps the original testing README. Current repository docs
  and release notes record the owner's subsequent acceptance and official status.

## Remaining provider and runtime lanes

See `docs/COMPATIBILITY.md` for the actual provider/configuration inventory and
separate service/contract/live matrix. newman55 Respecialization was absent from
the installed/test-available locations checked. Its exact launch modes,
Original-score/legacy settings, replacement callback, and final mercenary
recipient remain unresolved; no adapter was invented. Installed newman55 Cheat
Menu is a different mod. No additional installed full-respec engine was found.

Detailed development-session live lanes remain **NOT RUN**: creation/recruitment
regressions, native main and mercenary respec, native locked-base story negative control, Eddic main/mercenary/
story/remote and NPC entry, Bag of Tricks/CotW, full profile, later progression,
and save/reload including without Dice Roller. They require the specific guarded
procedure in `docs/SMOKE-TEST.md` and an owner-nominated disposable fixture. The
owner's release acceptance is complete; the unspecified detailed compatibility
and persistence lanes remain open.
