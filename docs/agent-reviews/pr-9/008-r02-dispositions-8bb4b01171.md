<!-- agent-review:v1 enrollment=kdr-umm0311-codex-umm-0311-baseline-20261123-20261123161619-b437 key=r02-dispositions-8bb4b01171 part=1/1 sha256=8bb4b01171c4e2e71e3955ad30f0044b72b3ea8cc454d73e9aa90a66f59bd50a -->
### Claude Opus — finding dispositions — round 2

Candidate after corrections: `444d58ba12a351281a2d0e4e8d42decffbbd927f`

- **R1-F1** — `fixed` — commits: `d8010ee`, `fca7cdef06`, `444d58ba12`
  docs/evidence/umm-0311/loader-baseline.json and loader-baseline.txt (75/75 on a temporary clone of fca7cde): package-reuse-after-build-wrapper-change-rejected, package-with-identical-managed-references-elsewhere-accepted, package-reuse-after-resolved-managed-reference-change-rejected (one byte of UnityEngine.UI.dll in a copied Managed dir), rebuild-after-source-change-accepted + package-after-matching-rebuild-accepted, package-names-producing-build-across-commits (producing_commit = build commit, packaging_commit = later docs-only commit, same DLL).
- **R1-F2** — `fixed` — commits: `d8010ee`, `fca7cdef06`, `444d58ba12`
  docs/evidence/umm-0311/loader-baseline.json and loader-baseline.txt (75/75 on a temporary clone of fca7cde): candidate-package-records-frozen-after-build-output-replaced (build DLL replaced by the v0.2.0 DLL after staging; manifest, loose DLL, receipt copy and ZIP still describe 634f2277...), plus archived-dll-vs-first-receipt-rejected.
- **R2-F1** — `fixed` — commits: `d8010ee`, `fca7cdef06`, `444d58ba12`
  docs/evidence/umm-0311/loader-baseline.json and loader-baseline.txt (75/75 on a temporary clone of fca7cde): install-whatif-valid-package-previews-target (real Install.ps1 -WhatIf, disposable GamePath/installation; 'What if: ... on target <temp>/.../disposable game/Mods/KingmakerDiceRoller' and 'WhatIf completed'), install-whatif-invalid-package-rejected, install-whatif-leaves-mods-and-temp-unchanged.
- **R2-F2** — `fixed` — commits: `d8010ee`, `fca7cdef06`, `444d58ba12`
  docs/evidence/umm-0311/loader-baseline.json and loader-baseline.txt (75/75 on a temporary clone of fca7cde): package-receipt-{dll-digest-array,dll-digest-boolean,inputs-digest-array,configuration-array,loader-version-array}-rejected and package-genuine-receipt-accepted (ordinary Package.ps1 route); receipt-of-first-build-for-changed-dll-rejected; validator-explicit-empty-expected-digest-rejected (supplied-but-invalid digest distinguished from omitted).
- **R2-F3** — `fixed` — commits: `d8010ee`, `fca7cdef06`, `444d58ba12`
  docs/evidence/umm-0311/loader-baseline.json and loader-baseline.txt (75/75 on a temporary clone of fca7cde): package-data-bearing-directory-record-rejected ('KingmakerDiceRoller/extra/' with an inert marker) and package-own-directory-record-with-data-rejected, with package-candidate-accepted for the genuine six-file ZIP.
- **R1-F4** — `rebutted` — commits: `d8010ee`, `fca7cdef06`, `444d58ba12`
  docs/evidence/umm-0311/ide-inventory.txt: the build host has only Visual Studio Build Tools 2022 17.14 (vswhere) and no devenv, so an IDE log cannot be produced here. The supported routes on this host are Build-Local.ps1 and command-line MSBuild; the suite's incremental-build-* cases show the guard runs on ordinary incremental /t:Build after manifest-only and pin-only changes. docs/UMM-COMPATIBILITY.md no longer claims IDE behaviour was observed; it states the settings (DisableFastUpToDateCheck, CustomAdditionalCompileInputs/UpToDateCheckInput) and that IDE behaviour was not observed.
- **R1-F9** — `fixed` — commits: `d8010ee`, `fca7cdef06`, `444d58ba12`
  Committed in the candidate: docs/evidence/umm-0311/build-local.txt (contract results, compiler reference paths), build-provenance.json (pin hashes, receipt), package-validation.json and package-manifest.json (archived DLL SHA-256/MVID/AssemblyRefs, ZIP digest), loader-baseline.txt/.json (per-case validator results incl. package-legacy-0.33.0-dll-with-lowered-manifest-rejected and same-host-validation-* in both orders); INDEX.md hashes.
- **R1-F10** — `fixed` — commits: `d8010ee`, `fca7cdef06`, `444d58ba12`
  Committed: docs/evidence/umm-0311/api-audit.json (frozen DLL 634f2277, full component versions and SHA-256 per profile, 0 unresolved for 0.31.1, 0.32.4, owner 0.33.0, 0.33.4), loader-profiles.md (provenance, owner measurement 2026-11-23), newest-stable-selection.txt (NuGet index/registration, GitHub 3d14bf0d Version 0.33.4), and the exact auditor source (loader-api-audit-Program.cs.txt, -csproj.txt, audit-loader-api.sh.txt).

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/agent_review.py verify --enrollment kdr-umm0311-codex-umm-0311-baseline-20261123-20261123161619-b437 --gate domain | yes | pass | ~2s | Controller receipt on 444d58b: 416/416 domain tests. |
| scripts/Build-Local.ps1 on clean fca7cde | no | pass | ~10s | docs/evidence/umm-0311/build-local.txt + build-provenance.json (commit fca7cde, dirty false, Release, inputs digest recorded). |
| scripts/Package.ps1 on clean fca7cde | no | pass | ~3s | docs/evidence/umm-0311/package.txt, package-manifest.json (producing_commit = packaging_commit = fca7cde), package-validation.json (DLL hash, MVID, AssemblyRefs). |
| scripts/Test-LoaderBaseline.ps1 (temporary clone of fca7cde) | no | pass | ~4 min | 75/75; docs/evidence/umm-0311/loader-baseline.txt and .json. Install.ps1 only run with -WhatIf against a disposable configuration. No IDE installed; incremental MSBuild exercised. |
| LoaderApiAudit (source in docs/evidence/umm-0311/loader-api-audit-*.txt) | no | pass | ~1s | docs/evidence/umm-0311/api-audit.json; static only. |
| Native in-game smoke on UMM 0.31.1 / 0.32.4 / 0.33.0 / 0.33.4 | no | not_run | 0 | No disposable-game harness; live installation must not be modified. |

_Model-generated developer response (Claude opus / high), posted by the agent-review controller through the owner's GitHub account; not a human review._
