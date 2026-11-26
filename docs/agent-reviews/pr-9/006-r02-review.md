<!-- agent-review:v1 enrollment=kdr-umm0311-codex-umm-0311-baseline-20261123-20261123161619-b437 key=r02-review part=1/1 sha256=2ea102bb0428aa33deb07c57ffd50ea28c944c78ce0eedd7a7fde593a83fda1b -->
### Codex GPT-6.2 Astra — review — round 2 — candidate `b3018c1b97`

**Decision:** `changes_required` — The controller confirms 416/416 domain tests. Ordinary packaging can still misidentify the producing build or archived DLL, malformed receipts can disable required checks, and the documented installer dry run fails. The production build, package regressions, IDE check and four-loader audit also lack reviewable execution records.

## Continuation for Claude

Bind package provenance to the producing receipt and effective compiler inputs. Carry the validated archive identity through the manifest and candidate copies. Require correctly typed receipt fields, check every ZIP entry, and make private validation work under Install.ps1 -WhatIf while keeping live writes behind ShouldProcess. Preserve the clean-candidate guard and the isolated legacy fixture.

At the corrected commit, provide scoped machine-generated build, package, IDE and API-audit records through a permitted route that reaches the reviewer snapshot. Check the delivered evidence inventory before claiming qualification. Printing results into the mixed foreground session yielded only a withholding notice here. Keep the work within Dice Roller tooling and documentation. Native and combined-stack checks may retain their documented NOT RUN status under the existing fixture constraints.

#### Findings

**R1-F1** · P1 · defect · blocks readiness — Ordinary packages can still name a commit that did not produce the DLL
- Location: `scripts/Common.ps1:147`; acceptance: mission §5, mission §6
- Trigger: Reuse an existing DLL and receipt after changing the build invocation, changing a selected managed reference, or advancing to another commit with identical fingerprinted files.
- Expected: Material compiler-input changes invalidate reuse. When inputs are identical, package provenance preserves the producing commit and dirty state alongside any packaging checkout information.
- Actual: The fingerprint omits Build-Local.ps1, which supplies the production compiler invocation, and the effective game references selected through GamePath.props. Package.ps1 still writes the current checkout's commit and dirty state into the ordinary package manifest. For example, 4deddcf and b3018c1 have identical fingerprinted files. A receipt from the former can produce a manifest naming only the latter.
- Why it blocks / does not: A material compiler-input change can qualify an old DLL, and even valid reuse loses the identity of its producing build.
- Requested correction: Include the effective production build inputs in the reuse check, including material wrapper options and selected managed reference identities. Record the producing receipt's provenance in ordinary packages and retain the clean-commit guard for candidate packages.
- Requested evidence: In a temporary repository, show refusal after a material wrapper or resolved-reference change, acceptance after a matching rebuild, and accurate producing-build metadata when identical inputs are reused across commits.

**R1-F2** · P1 · defect · blocks readiness — Package metadata rereads mutable build output after archive validation
- Location: `scripts/Package.ps1:58`; acceptance: mission §5, mission §6
- Trigger: A build replaces the output DLL or receipt after packaging stages its DLL and before it finishes writing candidate metadata.
- Expected: The archive, manifest, exported candidate DLL and producing receipt agree on the same bytes through completion.
- Actual: The new validator compares the archived DLL with the initial receipt. Package.ps1 then hashes the live build DLL again at line 58 and copies the live DLL and receipt at lines 66–67. A rebuild completing after staging can leave archive A validated while the manifest or loose candidate DLL describes build B.
- Why it blocks / does not: An accepted candidate directory can contain conflicting identities for the payload that was validated.
- Requested correction: Generate the manifest and candidate copies from the same frozen archive, staged DLL and verified receipt. Check their final agreement before reporting success.
- Requested evidence: Use temporary paths to change the build output after staging. Show that the accepted archive, manifest, loose DLL and receipt still match, or that packaging rejects the mixed result.

**R2-F1** · P1 · defect · blocks readiness — Installer dry run skips the private ZIP it needs to validate
- Location: `scripts/Common.ps1:241`; acceptance: mission §5, mission §6
- Trigger: Follow the preflight command documented in docs/BUILD-AND-RELEASE.md with a valid baseline ZIP.
- Expected: Install.ps1 -WhatIf validates and cleans up a private copy, reports the intended mod directory and leaves the installation untouched.
- Actual: Expand-ValidatedPackage creates the private directory and copies the ZIP with New-Item and Copy-Item. Both honor the inherited WhatIf preference. The validator consequently receives a missing private ZIP before Install.ps1 reaches its target preview. The suite calls the helper without WhatIf and checks the installer by searching its source.
- Why it blocks / does not: The documented preflight fails for a valid package, removing the supported way to inspect the install target before proceeding.
- Requested correction: Make private copy, validation-report and cleanup operations execute during preflight. Keep every live installation mutation behind the existing ShouldProcess decision.
- Requested evidence: Invoke the actual Install.ps1 -WhatIf entry point against a disposable configuration. Show a valid ZIP reaching the target preview, an invalid ZIP being rejected, temporary cleanup and an unchanged Mods directory.

**R2-F2** · P1 · defect · blocks readiness — Malformed receipt fields can disable DLL and build-input checks
- Location: `scripts/Common.ps1:167`; acceptance: mission §5, mission §6
- Trigger: Read a malformed JSON build receipt through the ordinary packaging route.
- Expected: Required receipt hashes, configuration and loader identity fields have valid scalar types and values before comparison. A supplied archive digest is always enforced.
- Actual: Assert-PinnedBuildReceipt checks property presence and then uses PowerShell comparisons on untyped JSON values. In Windows PowerShell 5.1, an empty-array DLL digest did not trigger the mismatch predicate and bound to a zero-length ExpectedDllSha256, disabling the archive comparison. Boolean fingerprint and observed-version values also passed their mismatch predicates.
- Why it blocks / does not: A structurally invalid receipt can qualify different candidate bytes or inputs despite the new receipt checks.
- Requested correction: Validate receipt field types and digest formats before comparing them. Distinguish an omitted standalone digest from an explicitly supplied invalid one.
- Requested evidence: Exercise the ordinary package route with array and Boolean receipt fields, including a different baseline DLL paired with a malformed digest. Show rejection alongside a genuine receipt that passes.

**R2-F3** · P2 · defect · blocks readiness — The standalone validator skips data-bearing ZIP directory entries
- Location: `scripts/Validate-Package.ps1:38`; acceptance: mission §5, mission §6
- Trigger: Validate a supplied archive containing the six allowed files and a directory record with a nonzero uncompressed length.
- Expected: Every ZIP entry is accounted for. Any accepted directory marker is safe and empty.
- Actual: Validate-Package.ps1 filters out every entry whose name ends in a slash before inspecting its length, name or content. A data-bearing directory record is omitted from both the six-file count and the new binary-content checks.
- Why it blocks / does not: Standalone validation can approve a ZIP containing payload bytes outside the six validated files.
- Requested correction: Validate directory records before excluding them from the file count. Reject records with data or unsafe paths.
- Requested evidence: Show standalone rejection of an otherwise valid ZIP with a nonempty directory record containing an inert marker, together with acceptance of the genuine six-file ZIP.

**R1-F4** · P2 · evidence_gap · blocks readiness — The IDE up-to-date decision still lacks execution evidence
- Location: `scripts/Test-LoaderBaseline.ps1:216`; acceptance: mission §5
- Trigger: Assess the claimed IDE coverage after only a manifest or pin changes.
- Expected: An ordinary supported IDE build reaches the reference guard after a pin-only or manifest-only change to an otherwise up-to-date project.
- Actual: The project now sets DisableFastUpToDateCheck and lists the loader-contract inputs. The proposed test invokes MSBuild /t:Build directly. The report states that Visual Studio itself was never driven, and the review inputs contain no IDE trace.
- Why it blocks / does not: The supported IDE can make its up-to-date decision before invoking MSBuild. That decision remains unobserved.
- Requested correction: Capture the supported IDE build path with the new settings. Adjust the integration if the IDE skips the guard.
- Requested evidence: Provide an IDE build log or trace covering an up-to-date control, rejection after a loader-contract-only change and acceptance after restoration.

**R1-F9** · P1 · evidence_gap · blocks readiness — Production build and package qualification results are absent from the review packet
- Location: `docs/UMM-COMPATIBILITY.md:108`; acceptance: mission §5, mission §6, mission §8
- Trigger: Verify the candidate's build-qualified, package-qualified and 58/58 claims.
- Expected: Reviewer-readable execution records bind the source commit, resolved compiler inputs, build receipt, DLL, ZIP and observed regression outcomes.
- Actual: The transcript contains a withholding notice. The evidence directory contains only the domain receipt and its log. The production build log, build receipt, package validation and boundary-suite output cited by the developer were not delivered.
- Why it blocks / does not: The required Windows production build, archived references and genuine legacy rejection cannot be independently confirmed from a domain test run.
- Requested correction: Supply scoped machine-generated outputs through the permitted review evidence route. Confirm they are present in the next review packet.
- Requested evidence: Include the corrected commit's production build and contract results, compiler reference paths and hashes, build receipt, archived DLL SHA-256, MVID and AssemblyRefs, ZIP digest, and per-case validator results. Include the genuine v0.2.0 manifest-only regression and both validation orders in one Windows PowerShell host.

**R1-F10** · P1 · evidence_gap · blocks readiness — The four-loader static audit still lacks reviewable measured results
- Location: `docs/UMM-COMPATIBILITY.md:94`; acceptance: mission §7, mission §8
- Trigger: Verify or repeat the newer-loader assessment for this candidate DLL.
- Expected: Each static result identifies the frozen DLL, full loader and Harmony identities, reproducible auditor invocation and measured resolution outcome.
- Actual: The runbook adds a method and profile-construction outline. It names an external LoaderApiAudit tool without a revision or retrieval identifier. The table provides abbreviated newer-component hashes and authored outcomes. The packet contains no machine-generated audit output, owner profile measurement or newest-stable selection record.
- Why it blocks / does not: The four claimed static passes cannot be checked against the measured assemblies or repeated with an identified auditor.
- Requested correction: Identify the exact shared auditor and how to obtain or select it. Supply its Dice-only machine output and profile provenance through the review evidence route.
- Requested evidence: Provide the frozen mod digest, full component versions and SHA-256 values, dated owner measurement, official newest-stable selection evidence and per-profile results for 0.31.1, 0.32.4, owner 0.33.0 and 0.33.4.

#### Prior finding assessments

- **R1-F1** — `fix_rejected` — The fingerprint and configuration check cover the tested source, project, pin and verifier edits. Material build-invocation and managed-reference inputs remain outside the check, and ordinary package provenance still substitutes the packaging commit for the producing commit.
- **R1-F2** — `fix_rejected` — The validator now compares an archived DLL with a genuine receipt, and installation validates a private copy. The manifest and loose candidate files still reread mutable build outputs after that comparison.
- **R1-F3** — `fix_verified` — Both pin readers require exactly the canonical UMM and Harmony12 name-to-file mappings. The resolved-input guard requires one match per assembly and compares each full path with the verified filename. This closes the reported warm-cache omission and same-stem path defects in source.
- **R1-F4** — `fix_rejected` — The project adds the requested up-to-date settings. The test and report cover command-line MSBuild only, leaving the IDE observation outstanding.
- **R1-F5** — `fix_verified` — The suite copies and hashes the legacy input before packaging and directs its builds and package outputs into a temporary clone. The package writer no longer targets the caller's conventional release path. Execution evidence for the repeated-run check falls under R1-F9.
- **R1-F6** — `fix_verified` — Negative controls now use a separately hash-pinned 0.32.4 fixture. The installed loader supplies measurement data only. A read-only check of the revised expression retained the complete quoted reference path containing spaces.
- **R1-F7** — `fix_verified` — The manifest checks require a string before comparing its value. In Windows PowerShell 5.1 the revised predicate accepted the genuine string and rejected array, Boolean and numeric values.
- **R1-F8** — `fix_verified` — All four documentation and license payloads now receive PE-header, NUL-byte and strict UTF-8 checks. This closes the reported renamed-assembly case. R2-F3 concerns separately filtered directory records.
- **R1-F9** — `fix_rejected` — The exact-candidate domain receipt is genuine. The build and package outputs described as printed in the transcript are absent from the transcript and evidence directory supplied for review.
- **R1-F10** — `fix_rejected` — The documentation better explains component assembly and profile selection. The auditor revision, measured profiles and machine-generated result for the frozen DLL remain unavailable in the review packet.
- **R1-F11** — `fix_verified` — The temporary-probe runbook now agrees with the compiler arguments and generated manifest. It identifies installed Kingmaker and Unity references, the pinned UMM 0.31.1.0 reference and ManagerVersion 0.31.1.

#### Evidence assessment

- **Inspected:** AGENTS.md, the complete mission and feature diff, the correction commits, the developer report, all eleven prior findings, the supplied transcript notice and the controller evidence inventory.; The production project, pin definitions and verifier, MSBuild targets, build receipt, packaging and qualification consumers, standalone validator, installer, boundary suite and affected runbooks.
- **Executed verification:** Verified the controller's domain receipt against its log SHA-256. The receipt names the exact candidate, records exit code 0, and the log ends with 416/416 passed.; Confirmed HEAD and the mission SHA-256. git diff --check passed for the supplied merge base through the candidate.; Independently hashed the 114 files selected by the candidate's fingerprint rules and obtained 263c4e9e6d6aa77c2cf80cb8e96e387624a3415a1d6cd98fd441b525606ec0d7. Git showed no change to those files between 4deddcf and b3018c1.; Read-only Windows PowerShell 5.1 checks confirmed the malformed-receipt comparison results, empty-string parameter binding, inherited WhatIf preference, scalar ManagerVersion rejection cases and quoted reference-path parsing.; Applied the validator's entry-selection predicate to in-memory records. It omitted a directory record with a positive length.
- **Source inspection only:** The project selects the canonical pinned UMM and Harmony12 paths. The boundary suite contains temporary-clone, wrong-loader, quoted-path, same-host validation and private-extraction cases.; The reported 58/58 boundary results, production DLL and ZIP identities, contract passes and four static API passes appear in developer-authored material. The supplied transcript contains no command output for those runs.
- **Required gaps:** Correct the remaining provenance, receipt typing, archive-entry and installer preflight defects.; Supply candidate-bound Windows production build and package results, including the genuine legacy manifest-only regression and the corrected boundary cases.; Supply an observation of the supported IDE up-to-date path and reproducible machine-generated results for all four static loader profiles.
- **Limitations:** Reviewer execution was limited to read-only inspection and in-memory checks. The supplied snapshot contains the domain gate receipt and log. The production build, suite and audit files cited at external paths were outside the fixed evidence set.; Native creation, persistence, mercenary, respec and combined-stack behavior are unqualified for this candidate. The documentation records native checks as NOT RUN on all four loaders.

#### Summary

- **Delivered behavior:** The manifest declares UMM 0.31.1. The production project selects pinned UnityModManager 0.31.1.0 and Harmony12 1.2.0.1, the validator inspects archived AssemblyRefs, and the regression suite isolates its legacy fixture in a temporary clone. Six prior findings are closed.
- **Scope reviewed:** The complete Dice Roller portion of the mission at the fixed final candidate, including all eleven prior findings and the directly affected build, package, installer and evidence paths.
- **Pending later-phase work:** none
- **Unresolved questions:** none
- **Remaining owner review:** After technical closure, the owner retains the final decision on draft PR #9 and any future authorization for disposable-game runtime qualification.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: none.

Codex CLI `0.170.0`, thread `01a20a77-545c-7931-a0d5-1f0b86afbe55`, duration 689.1s, response sha256 `a48b7346d21c43fe581955c48f9a197991be0917f2e936d32af0843cf47c05cd`, request `kdr-umm0311-codex-umm-0311-baseline-20261123-20261123161619-b437-r02-73010c`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "b3018c1b97aa69a7a545ecdf7579cee5d1e011ac",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The controller confirms 416/416 domain tests. Ordinary packaging can still misidentify the producing build or archived DLL, malformed receipts can disable required checks, and the documented installer dry run fails. The production build, package regressions, IDE check and four-loader audit also lack reviewable execution records.\n\n## Continuation for Claude\n\nBind package provenance to the producing receipt and effective compiler inputs. Carry the validated archive identity through the manifest and candidate copies. Require correctly typed receipt fields, check every ZIP entry, and make private validation work under Install.ps1 -WhatIf while keeping live writes behind ShouldProcess. Preserve the clean-candidate guard and the isolated legacy fixture.\n\nAt the corrected commit, provide scoped machine-generated build, package, IDE and API-audit records through a permitted route that reaches the reviewer snapshot. Check the delivered evidence inventory before claiming qualification. Printing results into the mixed foreground session yielded only a withholding notice here. Keep the work within Dice Roller tooling and documentation. Native and combined-stack checks may retain their documented NOT RUN status under the existing fixture constraints.",
  "evidence_assessment": {
    "executed_verification": [
      "Verified the controller's domain receipt against its log SHA-256. The receipt names the exact candidate, records exit code 0, and the log ends with 416/416 passed.",
      "Confirmed HEAD and the mission SHA-256. git diff --check passed for the supplied merge base through the candidate.",
      "Independently hashed the 114 files selected by the candidate's fingerprint rules and obtained 263c4e9e6d6aa77c2cf80cb8e96e387624a3415a1d6cd98fd441b525606ec0d7. Git showed no change to those files between 4deddcf and b3018c1.",
      "Read-only Windows PowerShell 5.1 checks confirmed the malformed-receipt comparison results, empty-string parameter binding, inherited WhatIf preference, scalar ManagerVersion rejection cases and quoted reference-path parsing.",
      "Applied the validator's entry-selection predicate to in-memory records. It omitted a directory record with a positive length."
    ],
    "inspected": [
      "AGENTS.md, the complete mission and feature diff, the correction commits, the developer report, all eleven prior findings, the supplied transcript notice and the controller evidence inventory.",
      "The production project, pin definitions and verifier, MSBuild targets, build receipt, packaging and qualification consumers, standalone validator, installer, boundary suite and affected runbooks."
    ],
    "limitations": [
      "Reviewer execution was limited to read-only inspection and in-memory checks. The supplied snapshot contains the domain gate receipt and log. The production build, suite and audit files cited at external paths were outside the fixed evidence set.",
      "Native creation, persistence, mercenary, respec and combined-stack behavior are unqualified for this candidate. The documentation records native checks as NOT RUN on all four loaders."
    ],
    "required_gaps": [
      "Correct the remaining provenance, receipt typing, archive-entry and installer preflight defects.",
      "Supply candidate-bound Windows production build and package results, including the genuine legacy manifest-only regression and the corrected boundary cases.",
      "Supply an observation of the supported IDE up-to-date path and reproducible machine-generated results for all four static loader profiles."
    ],
    "source_inspection_only": [
      "The project selects the canonical pinned UMM and Harmony12 paths. The boundary suite contains temporary-clone, wrong-loader, quoted-path, same-host validation and private-extraction cases.",
      "The reported 58/58 boundary results, production DLL and ZIP identities, contract passes and four static API passes appear in developer-authored material. The supplied transcript contains no command output for those runs."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76"
      ],
      "actual": "The fingerprint omits Build-Local.ps1, which supplies the production compiler invocation, and the effective game references selected through GamePath.props. Package.ps1 still writes the current checkout's commit and dirty state into the ordinary package manifest. For example, 4deddcf and b3018c1 have identical fingerprinted files. A receipt from the former can produce a manifest naming only the latter.",
      "blocking_rationale": "A material compiler-input change can qualify an old DLL, and even valid reuse loses the identity of its producing build.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Material compiler-input changes invalidate reuse. When inputs are identical, package provenance preserves the producing commit and dirty state alongside any packaging checkout information.",
      "id": "R1-F1",
      "location": {
        "file": "scripts/Common.ps1",
        "line": 147
      },
      "priority": "P1",
      "requested_correction": "Include the effective production build inputs in the reuse check, including material wrapper options and selected managed reference identities. Record the producing receipt's provenance in ordinary packages and retain the clean-commit guard for candidate packages.",
      "requested_evidence": "In a temporary repository, show refusal after a material wrapper or resolved-reference change, acceptance after a matching rebuild, and accurate producing-build metadata when identical inputs are reused across commits.",
      "title": "Ordinary packages can still name a commit that did not produce the DLL",
      "trigger": "Reuse an existing DLL and receipt after changing the build invocation, changing a selected managed reference, or advancing to another commit with identical fingerprinted files."
    },
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76"
      ],
      "actual": "The new validator compares the archived DLL with the initial receipt. Package.ps1 then hashes the live build DLL again at line 58 and copies the live DLL and receipt at lines 66\u201367. A rebuild completing after staging can leave archive A validated while the manifest or loose candidate DLL describes build B.",
      "blocking_rationale": "An accepted candidate directory can contain conflicting identities for the payload that was validated.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The archive, manifest, exported candidate DLL and producing receipt agree on the same bytes through completion.",
      "id": "R1-F2",
      "location": {
        "file": "scripts/Package.ps1",
        "line": 58
      },
      "priority": "P1",
      "requested_correction": "Generate the manifest and candidate copies from the same frozen archive, staged DLL and verified receipt. Check their final agreement before reporting success.",
      "requested_evidence": "Use temporary paths to change the build output after staging. Show that the accepted archive, manifest, loose DLL and receipt still match, or that packaging rejects the mixed result.",
      "title": "Package metadata rereads mutable build output after archive validation",
      "trigger": "A build replaces the output DLL or receipt after packaging stages its DLL and before it finishes writing candidate metadata."
    },
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76"
      ],
      "actual": "Expand-ValidatedPackage creates the private directory and copies the ZIP with New-Item and Copy-Item. Both honor the inherited WhatIf preference. The validator consequently receives a missing private ZIP before Install.ps1 reaches its target preview. The suite calls the helper without WhatIf and checks the installer by searching its source.",
      "blocking_rationale": "The documented preflight fails for a valid package, removing the supported way to inspect the install target before proceeding.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Install.ps1 -WhatIf validates and cleans up a private copy, reports the intended mod directory and leaves the installation untouched.",
      "id": "R2-F1",
      "location": {
        "file": "scripts/Common.ps1",
        "line": 241
      },
      "priority": "P1",
      "requested_correction": "Make private copy, validation-report and cleanup operations execute during preflight. Keep every live installation mutation behind the existing ShouldProcess decision.",
      "requested_evidence": "Invoke the actual Install.ps1 -WhatIf entry point against a disposable configuration. Show a valid ZIP reaching the target preview, an invalid ZIP being rejected, temporary cleanup and an unchanged Mods directory.",
      "title": "Installer dry run skips the private ZIP it needs to validate",
      "trigger": "Follow the preflight command documented in docs/BUILD-AND-RELEASE.md with a valid baseline ZIP."
    },
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76"
      ],
      "actual": "Assert-PinnedBuildReceipt checks property presence and then uses PowerShell comparisons on untyped JSON values. In Windows PowerShell 5.1, an empty-array DLL digest did not trigger the mismatch predicate and bound to a zero-length ExpectedDllSha256, disabling the archive comparison. Boolean fingerprint and observed-version values also passed their mismatch predicates.",
      "blocking_rationale": "A structurally invalid receipt can qualify different candidate bytes or inputs despite the new receipt checks.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Required receipt hashes, configuration and loader identity fields have valid scalar types and values before comparison. A supplied archive digest is always enforced.",
      "id": "R2-F2",
      "location": {
        "file": "scripts/Common.ps1",
        "line": 167
      },
      "priority": "P1",
      "requested_correction": "Validate receipt field types and digest formats before comparing them. Distinguish an omitted standalone digest from an explicitly supplied invalid one.",
      "requested_evidence": "Exercise the ordinary package route with array and Boolean receipt fields, including a different baseline DLL paired with a malformed digest. Show rejection alongside a genuine receipt that passes.",
      "title": "Malformed receipt fields can disable DLL and build-input checks",
      "trigger": "Read a malformed JSON build receipt through the ordinary packaging route."
    },
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76"
      ],
      "actual": "Validate-Package.ps1 filters out every entry whose name ends in a slash before inspecting its length, name or content. A data-bearing directory record is omitted from both the six-file count and the new binary-content checks.",
      "blocking_rationale": "Standalone validation can approve a ZIP containing payload bytes outside the six validated files.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Every ZIP entry is accounted for. Any accepted directory marker is safe and empty.",
      "id": "R2-F3",
      "location": {
        "file": "scripts/Validate-Package.ps1",
        "line": 38
      },
      "priority": "P2",
      "requested_correction": "Validate directory records before excluding them from the file count. Reject records with data or unsafe paths.",
      "requested_evidence": "Show standalone rejection of an otherwise valid ZIP with a nonempty directory record containing an inert marker, together with acceptance of the genuine six-file ZIP.",
      "title": "The standalone validator skips data-bearing ZIP directory entries",
      "trigger": "Validate a supplied archive containing the six allowed files and a directory record with a nonzero uncompressed length."
    },
    {
      "acceptance_ids": [
        "mission \u00a75"
      ],
      "actual": "The project now sets DisableFastUpToDateCheck and lists the loader-contract inputs. The proposed test invokes MSBuild /t:Build directly. The report states that Visual Studio itself was never driven, and the review inputs contain no IDE trace.",
      "blocking_rationale": "The supported IDE can make its up-to-date decision before invoking MSBuild. That decision remains unobserved.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "An ordinary supported IDE build reaches the reference guard after a pin-only or manifest-only change to an otherwise up-to-date project.",
      "id": "R1-F4",
      "location": {
        "file": "scripts/Test-LoaderBaseline.ps1",
        "line": 216
      },
      "priority": "P2",
      "requested_correction": "Capture the supported IDE build path with the new settings. Adjust the integration if the IDE skips the guard.",
      "requested_evidence": "Provide an IDE build log or trace covering an up-to-date control, rejection after a loader-contract-only change and acceptance after restoration.",
      "title": "The IDE up-to-date decision still lacks execution evidence",
      "trigger": "Assess the claimed IDE coverage after only a manifest or pin changes."
    },
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76",
        "mission \u00a78"
      ],
      "actual": "The transcript contains a withholding notice. The evidence directory contains only the domain receipt and its log. The production build log, build receipt, package validation and boundary-suite output cited by the developer were not delivered.",
      "blocking_rationale": "The required Windows production build, archived references and genuine legacy rejection cannot be independently confirmed from a domain test run.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "Reviewer-readable execution records bind the source commit, resolved compiler inputs, build receipt, DLL, ZIP and observed regression outcomes.",
      "id": "R1-F9",
      "location": {
        "file": "docs/UMM-COMPATIBILITY.md",
        "line": 108
      },
      "priority": "P1",
      "requested_correction": "Supply scoped machine-generated outputs through the permitted review evidence route. Confirm they are present in the next review packet.",
      "requested_evidence": "Include the corrected commit's production build and contract results, compiler reference paths and hashes, build receipt, archived DLL SHA-256, MVID and AssemblyRefs, ZIP digest, and per-case validator results. Include the genuine v0.2.0 manifest-only regression and both validation orders in one Windows PowerShell host.",
      "title": "Production build and package qualification results are absent from the review packet",
      "trigger": "Verify the candidate's build-qualified, package-qualified and 58/58 claims."
    },
    {
      "acceptance_ids": [
        "mission \u00a77",
        "mission \u00a78"
      ],
      "actual": "The runbook adds a method and profile-construction outline. It names an external LoaderApiAudit tool without a revision or retrieval identifier. The table provides abbreviated newer-component hashes and authored outcomes. The packet contains no machine-generated audit output, owner profile measurement or newest-stable selection record.",
      "blocking_rationale": "The four claimed static passes cannot be checked against the measured assemblies or repeated with an identified auditor.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "Each static result identifies the frozen DLL, full loader and Harmony identities, reproducible auditor invocation and measured resolution outcome.",
      "id": "R1-F10",
      "location": {
        "file": "docs/UMM-COMPATIBILITY.md",
        "line": 94
      },
      "priority": "P1",
      "requested_correction": "Identify the exact shared auditor and how to obtain or select it. Supply its Dice-only machine output and profile provenance through the review evidence route.",
      "requested_evidence": "Provide the frozen mod digest, full component versions and SHA-256 values, dated owner measurement, official newest-stable selection evidence and per-profile results for 0.31.1, 0.32.4, owner 0.33.0 and 0.33.4.",
      "title": "The four-loader static audit still lacks reviewable measured results",
      "trigger": "Verify or repeat the newer-loader assessment for this candidate DLL."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F1",
      "rationale": "The fingerprint and configuration check cover the tested source, project, pin and verifier edits. Material build-invocation and managed-reference inputs remain outside the check, and ordinary package provenance still substitutes the packaging commit for the producing commit."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F2",
      "rationale": "The validator now compares an archived DLL with a genuine receipt, and installation validates a private copy. The manifest and loose candidate files still reread mutable build outputs after that comparison."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F3",
      "rationale": "Both pin readers require exactly the canonical UMM and Harmony12 name-to-file mappings. The resolved-input guard requires one match per assembly and compares each full path with the verified filename. This closes the reported warm-cache omission and same-stem path defects in source."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F4",
      "rationale": "The project adds the requested up-to-date settings. The test and report cover command-line MSBuild only, leaving the IDE observation outstanding."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F5",
      "rationale": "The suite copies and hashes the legacy input before packaging and directs its builds and package outputs into a temporary clone. The package writer no longer targets the caller's conventional release path. Execution evidence for the repeated-run check falls under R1-F9."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F6",
      "rationale": "Negative controls now use a separately hash-pinned 0.32.4 fixture. The installed loader supplies measurement data only. A read-only check of the revised expression retained the complete quoted reference path containing spaces."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F7",
      "rationale": "The manifest checks require a string before comparing its value. In Windows PowerShell 5.1 the revised predicate accepted the genuine string and rejected array, Boolean and numeric values."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F8",
      "rationale": "All four documentation and license payloads now receive PE-header, NUL-byte and strict UTF-8 checks. This closes the reported renamed-assembly case. R2-F3 concerns separately filtered directory records."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F9",
      "rationale": "The exact-candidate domain receipt is genuine. The build and package outputs described as printed in the transcript are absent from the transcript and evidence directory supplied for review."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F10",
      "rationale": "The documentation better explains component assembly and profile selection. The auditor revision, measured profiles and machine-generated result for the frozen DLL remain unavailable in the review packet."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F11",
      "rationale": "The temporary-probe runbook now agrees with the compiler arguments and generated manifest. It identifies installed Kingmaker and Unity references, the pinned UMM 0.31.1.0 reference and ManagerVersion 0.31.1."
    }
  ],
  "request_id": "kdr-umm0311-codex-umm-0311-baseline-20261123-20261123161619-b437-r02-73010c",
  "round": 2,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The manifest declares UMM 0.31.1. The production project selects pinned UnityModManager 0.31.1.0 and Harmony12 1.2.0.1, the validator inspects archived AssemblyRefs, and the regression suite isolates its legacy fixture in a temporary clone. Six prior findings are closed.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "After technical closure, the owner retains the final decision on draft PR #9 and any future authorization for disposable-game runtime qualification."
    ],
    "scope_reviewed": "The complete Dice Roller portion of the mission at the fixed final candidate, including all eleven prior findings and the directly affected build, package, installer and evidence paths.",
    "unresolved_questions": []
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
