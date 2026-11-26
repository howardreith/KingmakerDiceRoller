<!-- agent-review:v1 enrollment=kdr-umm0311-codex-umm-0311-baseline-20261123-20261123161619-b437 key=r03-dispositions-5c0ac9007a part=1/1 sha256=5c0ac9007afefd7beaa5bb3fa612cfe6f8b62859a6942c0471e5819d53f111dc -->
### Claude Opus — finding dispositions — round 3

Candidate after corrections: `d07bcfb1cade7ec6d4180a7de3180e5190b3e8e6`

- **R1-F1** — `fixed` — commits: `9911d5d`, `039f323816`, `b1a0e2c`, `d07bcfb1ca`
  docs/evidence/umm-0311/loader-baseline.json (85/85 on a temporary clone of 039f323): compiler-resolves-release-only-managed-directory (a Release-only KingmakerManagedDir in GamePath.props; the build record shows Csc resolving the copied directory), package-reuse-after-resolved-managed-reference-change-rejected (one byte changed in that resolved file), package-with-identical-managed-references-elsewhere-accepted. build-provenance.json lists compiler_references.
- **R3-F1** — `fixed` — commits: `9911d5d`, `039f323816`, `b1a0e2c`, `d07bcfb1ca`
  docs/evidence/umm-0311/loader-baseline.json (85/85 on a temporary clone of 039f323): candidate-refuses-earlier-commit-receipt-with-identical-dll, candidate-refuses-when-receipt-refreshed-during-packaging, package-receipt-commit-array-rejected, package-receipt-dirty-string-rejected, candidate-accepted-after-build-at-current-commit (genuine control).
- **R3-F2** — `fixed` — commits: `9911d5d`, `039f323816`, `b1a0e2c`, `d07bcfb1ca`
  docs/evidence/umm-0311/loader-baseline.json (85/85 on a temporary clone of 039f323): validator-report-pairs-zip-digest-with-its-own-dll (two independently identified archives; the caller's file replaced by a disposable instrumented validator copy after the snapshot; the report pairs the snapshot's ZIP digest with its DLL).
- **R1-F4** — `rebutted` — commits: `d07bcfb1ca`
  Owner decision (2026-11-24, in the developer conversation): the owner has never opened or built this mod in an IDE and does not want IDE evidence unless essential; no IDE is installed on the build host. docs/UMM-COMPATIBILITY.md (d07bcfb) states that IDE builds are not a supported or verified route and records the missing IDE trace as an open, owner-accepted gap. The supported routes (Build-Local and command-line MSBuild) are covered by incremental-build-after-{manifest,pin}-only-change-rejected and incremental-build-up-to-date-accepted; DisableFastUpToDateCheck and the loader compile inputs remain in LoaderReferences.targets.
- **R3-F3** — `fixed` — commits: `9911d5d`, `039f323816`, `b1a0e2c`, `d07bcfb1ca`
  docs/evidence/umm-0311/loader-baseline.json (85/85 on a temporary clone of 039f323): candidate-package-records-frozen-after-build-output-replaced (replacement done by an instrumented disposable copy of Package.ps1, excluded in the clone), ordinary-package-preserves-build-output-and-receipt. scripts/Package.ps1 contains no environment hook.
- **R3-F4** — `fixed` — commits: `9911d5d`, `039f323816`, `b1a0e2c`, `d07bcfb1ca`
  docs/evidence/umm-0311/loader-baseline.json (85/85 on a temporary clone of 039f323): package-level0-deflate-empty-directory-accepted (genuine six-file package plus a level-0 DEFLATE 'KingmakerDiceRoller/licenses/' record), package-data-bearing-directory-record-rejected, package-own-directory-record-with-data-rejected.
- **R3-F5** — `fixed` — commits: `9911d5d`, `039f323816`, `b1a0e2c`, `d07bcfb1ca`
  b1a0e2c removes build-local.txt, loader-baseline.txt, package.txt, ide-inventory.txt, newest-stable-selection.txt and audit-loader-api.sh.txt; the remaining files are structured JSON/Markdown whose local prefixes are replaced (INDEX.md). Compiler inputs: build-record.json; artifact identities: build-provenance.json, package-manifest.json, package-validation.json; boundary results: loader-baseline.json.
- **R3-F6** — `fixed` — commits: `9911d5d`, `039f323816`, `b1a0e2c`, `d07bcfb1ca`
  docs/evidence/umm-0311/loader-profile-inventory.json: owner-install 0Harmony-1.2.dll recorded with assembly 0Harmony-1.2 1.2.0.1 and full SHA-256; its referenced_by list is empty (no UnityModManager, 0Harmony12 or candidate reference to it) and it only references 0Harmony 2.0.1.0, so it is outside the runtime stack.
- **R3-F7** — `fixed` — commits: `9911d5d`, `039f323816`, `b1a0e2c`, `d07bcfb1ca`
  docs/UMM-COMPATIBILITY.md and PROJECT-STATE.md current sections state 85/85 boundary checks, DLL 634f2277 and package b8cb456b, matching loader-baseline.json (source_commit 039f323, 85/85), build-provenance.json and package-manifest.json (commit 039f323); INDEX.md names the candidate commit; historical release sections unchanged.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/agent_review.py verify --enrollment kdr-umm0311-codex-umm-0311-baseline-20261123-20261123161619-b437 --gate domain | yes | pass | 1.0s | Controller receipt on d07bcfb. |
| scripts/Test-LoaderBaseline.ps1 (temporary clone of 039f323) | no | pass | ~4 min | 85/85; docs/evidence/umm-0311/loader-baseline.json (85/85 on a temporary clone of 039f323). |
| scripts/Build-Local.ps1 and Package.ps1 on clean 039f323 | no | pass | ~15s | build-provenance.json, build-record.json, package-manifest.json, package-validation.json. |
| LoaderApiAudit | no | pass | ~1s | api-audit.json; DLL 634f2277 unchanged since the audited build, so the audit applies. |
| IDE build trace | no | not_run | 0 | No IDE on the host; owner states the mod is never built in an IDE (see R1-F4). |
| Native in-game smoke on UMM 0.31.1 / 0.32.4 / 0.33.0 / 0.33.4 | no | not_run | 0 | No disposable-game harness; live installation must not be modified. |

_Model-generated developer response (Claude opus / high), posted by the agent-review controller through the owner's GitHub account; not a human review._
