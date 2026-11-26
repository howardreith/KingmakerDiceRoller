<!-- agent-review:v1 enrollment=kdr-umm0311-codex-umm-0311-baseline-20261123-20261123161619-b437 key=r01-review part=1/1 sha256=725232fc9605461b28130425c26feb27391f4d6f23f9a0526d762427359a8c30 -->
### Codex GPT-6.2 Astra — review — round 1 — candidate `65c3580bcb`

**Decision:** `changes_required` — The manifest declares 0.31.1 and the controller confirms 416/416 domain tests. Ordinary packaging can still attribute an old DLL to a new commit, and the regression suite can overwrite its genuine release input. The supplied evidence also does not establish the reported production build, package checks, or four-version API audit.

## Continuation for Claude

Bind reused DLLs to their material build inputs and compare the archived payload with its receipt. Close the pin-definition, IDE, and standalone-validator gaps. Run the regression suite in isolated output directories with deterministic fixtures. Supply reviewer-readable Windows build, package, and API-audit results tied to the corrected candidate, and update the reproduction instructions.

Keep the work within Dice Roller's build, validation, and documentation paths. Preserve the published v0.2.0 artifact, gameplay code, controller configuration, and owner's installation. Native qualification may remain NOT RUN under the documented fixture limitation.

#### Findings

**R1-F1** · P1 · defect · blocks readiness — Ordinary packaging can relabel a stale DLL with the current commit
- Location: `scripts/Package.ps1:13`; acceptance: mission §5, mission §6
- Trigger: Run Package.ps1 or Test-LoaderBaseline.ps1 after a material change with the previous DLL and its matching receipt still present.
- Expected: Material source, project-reference, pin, or helper changes invalidate reuse. Package provenance identifies the build that produced the DLL.
- Actual: Assert-PinnedBuildReceipt checks the existing DLL hash and pin records. Only -Candidate checks the producing commit and configuration. Ordinary Package.ps1 writes the current Git commit into its manifest, and Test-LoaderBaseline.ps1 can return success after merely warning about stale provenance.
- Why it blocks / does not: A package and a passing test report can identify a commit whose material inputs never produced the enclosed DLL.
- Requested correction: Enforce material-input and configuration provenance on every route that reuses build output. Preserve the existing clean-commit candidate guard and make stale inputs fail qualification.
- Requested evidence: In a temporary Git repository, build once, change a production source or reference helper while retaining the old DLL and receipt, and show the ordinary package and qualification paths refuse reuse. Show acceptance after a matching rebuild.

**R1-F2** · P1 · defect · blocks readiness — The archived DLL digest is recorded but never enforced at its consumers
- Location: `scripts/Package.ps1:52`; acceptance: mission §5, mission §6
- Trigger: The staged or supplied archive differs from the receipt-checked build output while its mod DLL still references the pinned loader versions.
- Expected: The DLL hash observed inside the ZIP matches the producing receipt. Installation stages the bytes that passed standalone validation.
- Actual: Package.ps1 validates the source DLL, then writes its manifest hash from that source file. It never compares the validator's archived dll_sha256 with the receipt. The receipt check also ignores its observed assembly_version field. Install.ps1 reopens the ZIP after validation and takes its expected hash from the subsequent extraction.
- Why it blocks / does not: A different baseline-compiled payload can pass the AssemblyRef check while the package manifest identifies other bytes. A replacement at the same ZIP path can also detach installation from the validation result.
- Requested correction: Compare the archive reader's DLL digest with the expected build receipt before accepting a produced package, and validate the recorded observed loader versions. Carry the validated identity through installation or validate a private copy used for extraction.
- Requested evidence: Show rejection of a different baseline-compiled DLL paired with the first build's receipt and of a receipt with a conflicting observed version. Demonstrate the validation-to-staging check using temporary files.

**R1-F3** · P1 · defect · blocks readiness — An incomplete pin definition can leave Harmony12 unchecked
- Location: `scripts/Get-PinnedLoaderReferences.ps1:13`; acceptance: mission §5, mission §6
- Trigger: A pin edit drops or misnames the Harmony12 entry while its canonical cache file still exists.
- Expected: Both required name-to-file mappings are present exactly once, and every accepted loader compiler input is the exact verified file.
- Actual: The pin reader requires a UnityModManager record but permits omission of 0Harmony12. The project still accepts an existing 0Harmony12.dll in the cache. Package validation iterates the shortened pin list. The resolved-input guard compares filename stems and directories rather than the complete paths of the verified files.
- Why it blocks / does not: A warm cache can supply an unverified Harmony compiler input and the resulting DLL can qualify without its Harmony reference being checked.
- Requested correction: Require the canonical UMM and Harmony12 mappings in the pin definition used by build and package validation. Compare the complete resolved compiler paths with those verified by the helper.
- Requested evidence: With both DLLs already cached, show that a missing or misnamed Harmony pin fails. Show that an alternate same-stem file in the verified directory cannot qualify as the compiler input.

**R1-F4** · P2 · evidence_gap · blocks readiness — A forced MSBuild rebuild does not establish IDE up-to-date coverage
- Location: `LoaderReferences.targets:8`; acceptance: mission §5
- Trigger: Invoke an ordinary IDE build after changing only a loader-contract input.
- Expected: A supported IDE build detects a pin or manifest change even when its previous C# output appears up to date.
- Actual: Both guards run inside MSBuild. The project supplies no IDE up-to-date input for Info.json, the pin JSON, or the verifier. The direct-build test uses a fresh intermediate directory and forces Rebuild for its positive case.
- Why it blocks / does not: The claimed IDE coverage leaves an explicitly required reuse path unverified.
- Requested correction: Establish that the supported IDE path invokes the guard after a pin-only change. Wire those inputs into its up-to-date decision or disable the shortcut for this project if needed.
- Requested evidence: Start with an up-to-date production project, change only a pin or manifest to a mismatched value, and record rejection from an ordinary supported IDE build followed by acceptance with the genuine inputs.

**R1-F5** · P1 · defect · blocks readiness — The legacy regression can overwrite the genuine release ZIP
- Location: `scripts/Test-LoaderBaseline.ps1:208`; acceptance: mission §6, mission §8
- Trigger: Pass the genuine release at artifacts/packages/KingmakerDiceRoller-0.2.0.zip through -LegacyReleaseZip.
- Expected: The regression uses isolated temporary files and preserves the supplied genuine release and existing package outputs.
- Actual: The suite verifies the supplied legacy ZIP, then invokes ordinary Package.ps1. The deterministic ZIP writer replaces artifacts/packages/KingmakerDiceRoller-0.2.0.zip. If that was the supplied legacy path, the later manifest-only regression reads the newly built ZIP and the original local release copy is gone.
- Why it blocks / does not: The required regression can destroy its own evidence and overwrite a preserved artifact.
- Requested correction: Use isolated staging and package output for the test and retain a private verified copy of its legacy input throughout the run.
- Requested evidence: In a temporary worktree, supply the genuine release from the conventional package path. Run the suite twice and show that the input's SHA-256 and existing package outputs are preserved.

**R1-F6** · P2 · defect · blocks readiness — The baseline tests assume a newer installation and space-free paths
- Location: `scripts/Test-LoaderBaseline.ps1:104`; acceptance: mission §5, mission §6, mission §8
- Trigger: Run the suite with genuine 0.31.1 installed or from a worktree whose path contains a space.
- Expected: Negative fixtures differ from the baseline by construction, and valid quoted compiler paths are recognized.
- Actual: Two negative controls use the installed UMM digest as an assumed wrong digest. On a genuine baseline installation it equals the pin. The compiler-line regex also excludes spaces inside quoted paths, which I confirmed with a quoted Windows reference path.
- Why it blocks / does not: The documented qualification command reports failures for valid supported environments.
- Requested correction: Use a known distinct fixture for wrong-loader and wrong-receipt checks, keep the owner-install measurement separate, and parse quoted reference paths completely.
- Requested evidence: Exercise the suite with baseline-identical installed-reference fixture bytes and a worktree path containing a space. Preserve the owner's live loader.

**R1-F7** · P2 · defect · blocks readiness — The packaged minimum-version check accepts invalid JSON types
- Location: `scripts/Validate-Package.ps1:85`; acceptance: mission §5, mission §6
- Trigger: Validate a baseline DLL with a packaged ManagerVersion value of the wrong JSON type.
- Expected: Packaged ManagerVersion is a JSON string whose value is exactly 0.31.1.
- Actual: PowerShell's comparison filters array operands and coerces Boolean operands. The exact rejection predicate returned false for [], ["0.31.1"], and true.
- Why it blocks / does not: Standalone validation can accept a package without the required scalar minimum-version declaration.
- Requested correction: Require a scalar string before comparing its value. Apply the same manifest contract wherever pin agreement is checked.
- Requested evidence: Run the standalone validator on an otherwise valid baseline ZIP with array and Boolean ManagerVersion values, alongside a passing scalar-string control.

**R1-F8** · P2 · defect · blocks readiness — The six-file allowlist checks dependency filenames but misses binary content
- Location: `scripts/Validate-Package.ps1:50`; acceptance: mission §5, mission §6
- Trigger: An archive retains a valid mod DLL and all expected names but contains dependency bytes in a documentation entry.
- Expected: Allowed text entries cannot carry UMM, Harmony, game, or Unity binaries.
- Actual: For the four documentation and license payloads, the validator checks only the entry name and positive length. A managed dependency occupying an allowed text entry passes those checks. The current swap tests also change the entry name, so they exercise the filename allowlist.
- Why it blocks / does not: A supplied ZIP can distribute prohibited assembly bytes while retaining the six expected filenames.
- Requested correction: Validate the expected text payloads sufficiently to reject binary assembly content under an allowed text filename.
- Requested evidence: Keep all six entry names intact and show that standalone validation rejects a pinned dependency occupying a documentation entry while accepting the genuine package.

**R1-F9** · P1 · evidence_gap · blocks readiness — The production build and package regression results lack reviewable execution evidence
- Location: `docs/UMM-COMPATIBILITY.md:67`; acceptance: mission §5, mission §6, mission §8
- Trigger: Assess the published build-qualified and package-qualified claims for this commit.
- Expected: Reviewer-readable execution records connect the clean source commit to the compiler inputs, DLL, ZIP, and observed boundary-test outcomes.
- Actual: The supplied transcript contains a withholding notice. Its sole accompanying receipt builds the domain test executable. The production build log and loader-baseline report are external paths, and the snapshot contains neither output.
- Why it blocks / does not: The required Windows production build, compiler input identities, candidate ZIP, and genuine 0.33.0 rejection cannot be independently confirmed.
- Requested correction: Supply scoped machine-generated outputs through the permitted review evidence route for the corrected commit. Include the relevant draft PR readback if it is the canonical source/ZIP record.
- Requested evidence: Provide the Windows production build and contract results, resolved compiler paths and pin hashes, build receipt, archived DLL hash/MVID/AssemblyRefs, ZIP hash, and positive and negative standalone-validator output. Exercise the genuine v0.2.0 manifest-only case. Include both archives in one Windows PowerShell host to cover the invocation style used by Install.ps1.

**R1-F10** · P1 · evidence_gap · blocks readiness — The four-loader API audit has no reviewable output or runnable reproduction instructions
- Location: `docs/UMM-COMPATIBILITY.md:85`; acceptance: mission §7, mission §8
- Trigger: Verify the claimed static results or repeat the newer-loader assessment.
- Expected: Each static result identifies the same candidate DLL, the measured loader and Harmony stack, a reproducible audit invocation, and its observed outcome.
- Actual: The document summarizes 7 types and 25 members across four profiles. Its reproduction commands stop at package validation. The audit implementation or revision, invocation, measured profile records, and result file are absent from the review inputs.
- Why it blocks / does not: The required static compatibility assessment cannot be verified or repeated from the supplied material.
- Requested correction: Document how to obtain or identify the shared audit tool and construct or select each profile. Supply its machine-generated Dice results and profile provenance through the review evidence route.
- Requested evidence: Provide the frozen DLL digest, full component identities and hashes, dated owner-install measurement and newest-stable selection, and per-profile resolution results for 0.31.1, 0.32.4, owner 0.33.0, and 0.33.4. Retain the distinction between component-assembled and owner-install profiles.

**R1-F11** · P2 · required_gap · blocks readiness — The native-theme probe runbook still identifies installed UMM as its compiler reference
- Location: `docs/NATIVE-THEME-RUNTIME.md:23`; acceptance: mission §8
- Trigger: Follow the current documentation for the changed lab probe.
- Expected: The probe instructions distinguish its installed Unity references from its pinned UMM compiler input.
- Actual: The probe runbook says it compiles against installed Unity/UMM contracts. The changed Build-NativeThemeRuntimeProbe.ps1 now takes UnityModManager.dll from the verified baseline cache.
- Why it blocks / does not: A current build instruction contradicts the changed reference source, which the mission expressly requires the documentation to explain.
- Requested correction: Update the present-tense probe build description while preserving its historical runtime observations.
- Requested evidence: Show that the revised instruction agrees with the probe compiler arguments and pin source.

#### Evidence assessment

- **Inspected:** Mission, AGENTS.md, request metadata, developer report, transcript notice, evidence directory, and empty prior-findings ledger.; The full 15-file feature diff and its build, package, installation, qualification, ZIP-writing, and documentation consumers.
- **Executed verification:** Verified that HEAD and the merge base match the controller inputs, that the mission SHA-256 matches, and that git diff --check passes across M..H.; Verified the controller's domain receipt against its log SHA-256. Test-Domain.ps1 ran at 65c3580bcb314c2e1df90ab5d20a6c48d78fb48f, exited 0, and printed RESULT 416/416 passed.; Evaluated the packaged ManagerVersion predicate in isolation. It accepted an empty array, a one-element string array, and JSON true. The compiler-reference regex also returned zero matches for a quoted reference path containing a space.
- **Source inspection only:** Info.json declares 0.31.1. The production project retains .NET Framework 4.7.2 and C# 7.3 and points to the ignored reference cache. The committed UMM and Harmony12 pins match the mission's versions and SHA-256 values.; The acquisition helper checks downloaded bytes and assembly identities. The project imports pre-resolution and post-resolution guards. The standalone validator reads the ZIP's mod DLL and Install.ps1 invokes it before extraction.; The reported production DLL 634f2277…12ee, ZIP b8cb456b…6e81, MVID 7a6d0a75-f788-47a5-83c9-fed6fb14e3ff, 23/23 checks, and four successful static rows appear in developer-authored material. Their generating runs could not be confirmed.
- **Required gaps:** Source-to-DLL and receipt-to-archive binding across supported packaging paths.; Complete pin coverage, IDE up-to-date behavior, and strict supplied-ZIP validation.; Temporary, repeatable regression checks that preserve the genuine release input.; Reviewer-readable production build, package, and static API evidence for the corrected candidate, with reproducible audit instructions.
- **Limitations:** The foreground transcript was withheld. The snapshot contains the domain receipt and log only. The external build log, loader test report, API audit, and profile records named in the developer report were unavailable.; Review was read-only. The PowerShell sandbox also prevented an independent reflection-only metadata probe. Same-process Windows PowerShell validation remains unverified.; The controller identifies draft PR #9. Its live body and the artifact measurements attributed to it were unavailable in the snapshot.; Native roll/assignment, persistence, mercenary/respec, and combined-stack behavior remain unverified on all four loaders. The mission permits the recorded NOT RUN disposition without an approved disposable route.

#### Summary

- **Delivered behavior:** The candidate declares UMM 0.31.1, selects pinned production references, adds build and ZIP checks, and reports native qualification as NOT RUN. The candidate-bound domain gate passed 416/416.
- **Scope reviewed:** Final Dice Roller checkpoint for draft PR #9, from 9046aefc3c1e096712e46b297b2b232ed7441a0f through 65c3580bcb314c2e1df90ab5d20a6c48d78fb48f.
- **Pending later-phase work:** none
- **Unresolved questions:** Whether repeated Windows PowerShell validations of the two same-identity mod DLLs inspect each archive independently.
- **Remaining owner review:** Any future authorization for a disposable native fixture and acceptance of in-game roll/assignment and persistence.; Merge or release disposition after the required corrections and independent review.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: none.

Codex CLI `0.170.0`, thread `01a20a46-f37a-7ef1-9958-1897bea83160`, duration 785.3s, response sha256 `7ef90deed93ee55aaae6f2274a8bfde83816cc7348fa691a607ce2f1402d349c`, request `kdr-umm0311-codex-umm-0311-baseline-20261123-20261123161619-b437-r01-cc943f`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "65c3580bcb314c2e1df90ab5d20a6c48d78fb48f",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The manifest declares 0.31.1 and the controller confirms 416/416 domain tests. Ordinary packaging can still attribute an old DLL to a new commit, and the regression suite can overwrite its genuine release input. The supplied evidence also does not establish the reported production build, package checks, or four-version API audit.\n\n## Continuation for Claude\n\nBind reused DLLs to their material build inputs and compare the archived payload with its receipt. Close the pin-definition, IDE, and standalone-validator gaps. Run the regression suite in isolated output directories with deterministic fixtures. Supply reviewer-readable Windows build, package, and API-audit results tied to the corrected candidate, and update the reproduction instructions.\n\nKeep the work within Dice Roller's build, validation, and documentation paths. Preserve the published v0.2.0 artifact, gameplay code, controller configuration, and owner's installation. Native qualification may remain NOT RUN under the documented fixture limitation.",
  "evidence_assessment": {
    "executed_verification": [
      "Verified that HEAD and the merge base match the controller inputs, that the mission SHA-256 matches, and that git diff --check passes across M..H.",
      "Verified the controller's domain receipt against its log SHA-256. Test-Domain.ps1 ran at 65c3580bcb314c2e1df90ab5d20a6c48d78fb48f, exited 0, and printed RESULT 416/416 passed.",
      "Evaluated the packaged ManagerVersion predicate in isolation. It accepted an empty array, a one-element string array, and JSON true. The compiler-reference regex also returned zero matches for a quoted reference path containing a space."
    ],
    "inspected": [
      "Mission, AGENTS.md, request metadata, developer report, transcript notice, evidence directory, and empty prior-findings ledger.",
      "The full 15-file feature diff and its build, package, installation, qualification, ZIP-writing, and documentation consumers."
    ],
    "limitations": [
      "The foreground transcript was withheld. The snapshot contains the domain receipt and log only. The external build log, loader test report, API audit, and profile records named in the developer report were unavailable.",
      "Review was read-only. The PowerShell sandbox also prevented an independent reflection-only metadata probe. Same-process Windows PowerShell validation remains unverified.",
      "The controller identifies draft PR #9. Its live body and the artifact measurements attributed to it were unavailable in the snapshot.",
      "Native roll/assignment, persistence, mercenary/respec, and combined-stack behavior remain unverified on all four loaders. The mission permits the recorded NOT RUN disposition without an approved disposable route."
    ],
    "required_gaps": [
      "Source-to-DLL and receipt-to-archive binding across supported packaging paths.",
      "Complete pin coverage, IDE up-to-date behavior, and strict supplied-ZIP validation.",
      "Temporary, repeatable regression checks that preserve the genuine release input.",
      "Reviewer-readable production build, package, and static API evidence for the corrected candidate, with reproducible audit instructions."
    ],
    "source_inspection_only": [
      "Info.json declares 0.31.1. The production project retains .NET Framework 4.7.2 and C# 7.3 and points to the ignored reference cache. The committed UMM and Harmony12 pins match the mission's versions and SHA-256 values.",
      "The acquisition helper checks downloaded bytes and assembly identities. The project imports pre-resolution and post-resolution guards. The standalone validator reads the ZIP's mod DLL and Install.ps1 invokes it before extraction.",
      "The reported production DLL 634f2277\u202612ee, ZIP b8cb456b\u20266e81, MVID 7a6d0a75-f788-47a5-83c9-fed6fb14e3ff, 23/23 checks, and four successful static rows appear in developer-authored material. Their generating runs could not be confirmed."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76"
      ],
      "actual": "Assert-PinnedBuildReceipt checks the existing DLL hash and pin records. Only -Candidate checks the producing commit and configuration. Ordinary Package.ps1 writes the current Git commit into its manifest, and Test-LoaderBaseline.ps1 can return success after merely warning about stale provenance.",
      "blocking_rationale": "A package and a passing test report can identify a commit whose material inputs never produced the enclosed DLL.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Material source, project-reference, pin, or helper changes invalidate reuse. Package provenance identifies the build that produced the DLL.",
      "id": "R1-F1",
      "location": {
        "file": "scripts/Package.ps1",
        "line": 13
      },
      "priority": "P1",
      "requested_correction": "Enforce material-input and configuration provenance on every route that reuses build output. Preserve the existing clean-commit candidate guard and make stale inputs fail qualification.",
      "requested_evidence": "In a temporary Git repository, build once, change a production source or reference helper while retaining the old DLL and receipt, and show the ordinary package and qualification paths refuse reuse. Show acceptance after a matching rebuild.",
      "title": "Ordinary packaging can relabel a stale DLL with the current commit",
      "trigger": "Run Package.ps1 or Test-LoaderBaseline.ps1 after a material change with the previous DLL and its matching receipt still present."
    },
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76"
      ],
      "actual": "Package.ps1 validates the source DLL, then writes its manifest hash from that source file. It never compares the validator's archived dll_sha256 with the receipt. The receipt check also ignores its observed assembly_version field. Install.ps1 reopens the ZIP after validation and takes its expected hash from the subsequent extraction.",
      "blocking_rationale": "A different baseline-compiled payload can pass the AssemblyRef check while the package manifest identifies other bytes. A replacement at the same ZIP path can also detach installation from the validation result.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The DLL hash observed inside the ZIP matches the producing receipt. Installation stages the bytes that passed standalone validation.",
      "id": "R1-F2",
      "location": {
        "file": "scripts/Package.ps1",
        "line": 52
      },
      "priority": "P1",
      "requested_correction": "Compare the archive reader's DLL digest with the expected build receipt before accepting a produced package, and validate the recorded observed loader versions. Carry the validated identity through installation or validate a private copy used for extraction.",
      "requested_evidence": "Show rejection of a different baseline-compiled DLL paired with the first build's receipt and of a receipt with a conflicting observed version. Demonstrate the validation-to-staging check using temporary files.",
      "title": "The archived DLL digest is recorded but never enforced at its consumers",
      "trigger": "The staged or supplied archive differs from the receipt-checked build output while its mod DLL still references the pinned loader versions."
    },
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76"
      ],
      "actual": "The pin reader requires a UnityModManager record but permits omission of 0Harmony12. The project still accepts an existing 0Harmony12.dll in the cache. Package validation iterates the shortened pin list. The resolved-input guard compares filename stems and directories rather than the complete paths of the verified files.",
      "blocking_rationale": "A warm cache can supply an unverified Harmony compiler input and the resulting DLL can qualify without its Harmony reference being checked.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Both required name-to-file mappings are present exactly once, and every accepted loader compiler input is the exact verified file.",
      "id": "R1-F3",
      "location": {
        "file": "scripts/Get-PinnedLoaderReferences.ps1",
        "line": 13
      },
      "priority": "P1",
      "requested_correction": "Require the canonical UMM and Harmony12 mappings in the pin definition used by build and package validation. Compare the complete resolved compiler paths with those verified by the helper.",
      "requested_evidence": "With both DLLs already cached, show that a missing or misnamed Harmony pin fails. Show that an alternate same-stem file in the verified directory cannot qualify as the compiler input.",
      "title": "An incomplete pin definition can leave Harmony12 unchecked",
      "trigger": "A pin edit drops or misnames the Harmony12 entry while its canonical cache file still exists."
    },
    {
      "acceptance_ids": [
        "mission \u00a75"
      ],
      "actual": "Both guards run inside MSBuild. The project supplies no IDE up-to-date input for Info.json, the pin JSON, or the verifier. The direct-build test uses a fresh intermediate directory and forces Rebuild for its positive case.",
      "blocking_rationale": "The claimed IDE coverage leaves an explicitly required reuse path unverified.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "A supported IDE build detects a pin or manifest change even when its previous C# output appears up to date.",
      "id": "R1-F4",
      "location": {
        "file": "LoaderReferences.targets",
        "line": 8
      },
      "priority": "P2",
      "requested_correction": "Establish that the supported IDE path invokes the guard after a pin-only change. Wire those inputs into its up-to-date decision or disable the shortcut for this project if needed.",
      "requested_evidence": "Start with an up-to-date production project, change only a pin or manifest to a mismatched value, and record rejection from an ordinary supported IDE build followed by acceptance with the genuine inputs.",
      "title": "A forced MSBuild rebuild does not establish IDE up-to-date coverage",
      "trigger": "Invoke an ordinary IDE build after changing only a loader-contract input."
    },
    {
      "acceptance_ids": [
        "mission \u00a76",
        "mission \u00a78"
      ],
      "actual": "The suite verifies the supplied legacy ZIP, then invokes ordinary Package.ps1. The deterministic ZIP writer replaces artifacts/packages/KingmakerDiceRoller-0.2.0.zip. If that was the supplied legacy path, the later manifest-only regression reads the newly built ZIP and the original local release copy is gone.",
      "blocking_rationale": "The required regression can destroy its own evidence and overwrite a preserved artifact.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The regression uses isolated temporary files and preserves the supplied genuine release and existing package outputs.",
      "id": "R1-F5",
      "location": {
        "file": "scripts/Test-LoaderBaseline.ps1",
        "line": 208
      },
      "priority": "P1",
      "requested_correction": "Use isolated staging and package output for the test and retain a private verified copy of its legacy input throughout the run.",
      "requested_evidence": "In a temporary worktree, supply the genuine release from the conventional package path. Run the suite twice and show that the input's SHA-256 and existing package outputs are preserved.",
      "title": "The legacy regression can overwrite the genuine release ZIP",
      "trigger": "Pass the genuine release at artifacts/packages/KingmakerDiceRoller-0.2.0.zip through -LegacyReleaseZip."
    },
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76",
        "mission \u00a78"
      ],
      "actual": "Two negative controls use the installed UMM digest as an assumed wrong digest. On a genuine baseline installation it equals the pin. The compiler-line regex also excludes spaces inside quoted paths, which I confirmed with a quoted Windows reference path.",
      "blocking_rationale": "The documented qualification command reports failures for valid supported environments.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Negative fixtures differ from the baseline by construction, and valid quoted compiler paths are recognized.",
      "id": "R1-F6",
      "location": {
        "file": "scripts/Test-LoaderBaseline.ps1",
        "line": 104
      },
      "priority": "P2",
      "requested_correction": "Use a known distinct fixture for wrong-loader and wrong-receipt checks, keep the owner-install measurement separate, and parse quoted reference paths completely.",
      "requested_evidence": "Exercise the suite with baseline-identical installed-reference fixture bytes and a worktree path containing a space. Preserve the owner's live loader.",
      "title": "The baseline tests assume a newer installation and space-free paths",
      "trigger": "Run the suite with genuine 0.31.1 installed or from a worktree whose path contains a space."
    },
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76"
      ],
      "actual": "PowerShell's comparison filters array operands and coerces Boolean operands. The exact rejection predicate returned false for [], [\"0.31.1\"], and true.",
      "blocking_rationale": "Standalone validation can accept a package without the required scalar minimum-version declaration.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Packaged ManagerVersion is a JSON string whose value is exactly 0.31.1.",
      "id": "R1-F7",
      "location": {
        "file": "scripts/Validate-Package.ps1",
        "line": 85
      },
      "priority": "P2",
      "requested_correction": "Require a scalar string before comparing its value. Apply the same manifest contract wherever pin agreement is checked.",
      "requested_evidence": "Run the standalone validator on an otherwise valid baseline ZIP with array and Boolean ManagerVersion values, alongside a passing scalar-string control.",
      "title": "The packaged minimum-version check accepts invalid JSON types",
      "trigger": "Validate a baseline DLL with a packaged ManagerVersion value of the wrong JSON type."
    },
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76"
      ],
      "actual": "For the four documentation and license payloads, the validator checks only the entry name and positive length. A managed dependency occupying an allowed text entry passes those checks. The current swap tests also change the entry name, so they exercise the filename allowlist.",
      "blocking_rationale": "A supplied ZIP can distribute prohibited assembly bytes while retaining the six expected filenames.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Allowed text entries cannot carry UMM, Harmony, game, or Unity binaries.",
      "id": "R1-F8",
      "location": {
        "file": "scripts/Validate-Package.ps1",
        "line": 50
      },
      "priority": "P2",
      "requested_correction": "Validate the expected text payloads sufficiently to reject binary assembly content under an allowed text filename.",
      "requested_evidence": "Keep all six entry names intact and show that standalone validation rejects a pinned dependency occupying a documentation entry while accepting the genuine package.",
      "title": "The six-file allowlist checks dependency filenames but misses binary content",
      "trigger": "An archive retains a valid mod DLL and all expected names but contains dependency bytes in a documentation entry."
    },
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76",
        "mission \u00a78"
      ],
      "actual": "The supplied transcript contains a withholding notice. Its sole accompanying receipt builds the domain test executable. The production build log and loader-baseline report are external paths, and the snapshot contains neither output.",
      "blocking_rationale": "The required Windows production build, compiler input identities, candidate ZIP, and genuine 0.33.0 rejection cannot be independently confirmed.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "Reviewer-readable execution records connect the clean source commit to the compiler inputs, DLL, ZIP, and observed boundary-test outcomes.",
      "id": "R1-F9",
      "location": {
        "file": "docs/UMM-COMPATIBILITY.md",
        "line": 67
      },
      "priority": "P1",
      "requested_correction": "Supply scoped machine-generated outputs through the permitted review evidence route for the corrected commit. Include the relevant draft PR readback if it is the canonical source/ZIP record.",
      "requested_evidence": "Provide the Windows production build and contract results, resolved compiler paths and pin hashes, build receipt, archived DLL hash/MVID/AssemblyRefs, ZIP hash, and positive and negative standalone-validator output. Exercise the genuine v0.2.0 manifest-only case. Include both archives in one Windows PowerShell host to cover the invocation style used by Install.ps1.",
      "title": "The production build and package regression results lack reviewable execution evidence",
      "trigger": "Assess the published build-qualified and package-qualified claims for this commit."
    },
    {
      "acceptance_ids": [
        "mission \u00a77",
        "mission \u00a78"
      ],
      "actual": "The document summarizes 7 types and 25 members across four profiles. Its reproduction commands stop at package validation. The audit implementation or revision, invocation, measured profile records, and result file are absent from the review inputs.",
      "blocking_rationale": "The required static compatibility assessment cannot be verified or repeated from the supplied material.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "Each static result identifies the same candidate DLL, the measured loader and Harmony stack, a reproducible audit invocation, and its observed outcome.",
      "id": "R1-F10",
      "location": {
        "file": "docs/UMM-COMPATIBILITY.md",
        "line": 85
      },
      "priority": "P1",
      "requested_correction": "Document how to obtain or identify the shared audit tool and construct or select each profile. Supply its machine-generated Dice results and profile provenance through the review evidence route.",
      "requested_evidence": "Provide the frozen DLL digest, full component identities and hashes, dated owner-install measurement and newest-stable selection, and per-profile resolution results for 0.31.1, 0.32.4, owner 0.33.0, and 0.33.4. Retain the distinction between component-assembled and owner-install profiles.",
      "title": "The four-loader API audit has no reviewable output or runnable reproduction instructions",
      "trigger": "Verify the claimed static results or repeat the newer-loader assessment."
    },
    {
      "acceptance_ids": [
        "mission \u00a78"
      ],
      "actual": "The probe runbook says it compiles against installed Unity/UMM contracts. The changed Build-NativeThemeRuntimeProbe.ps1 now takes UnityModManager.dll from the verified baseline cache.",
      "blocking_rationale": "A current build instruction contradicts the changed reference source, which the mission expressly requires the documentation to explain.",
      "blocks_readiness": true,
      "category": "required_gap",
      "expected": "The probe instructions distinguish its installed Unity references from its pinned UMM compiler input.",
      "id": "R1-F11",
      "location": {
        "file": "docs/NATIVE-THEME-RUNTIME.md",
        "line": 23
      },
      "priority": "P2",
      "requested_correction": "Update the present-tense probe build description while preserving its historical runtime observations.",
      "requested_evidence": "Show that the revised instruction agrees with the probe compiler arguments and pin source.",
      "title": "The native-theme probe runbook still identifies installed UMM as its compiler reference",
      "trigger": "Follow the current documentation for the changed lab probe."
    }
  ],
  "prior_finding_assessments": [],
  "request_id": "kdr-umm0311-codex-umm-0311-baseline-20261123-20261123161619-b437-r01-cc943f",
  "round": 1,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The candidate declares UMM 0.31.1, selects pinned production references, adds build and ZIP checks, and reports native qualification as NOT RUN. The candidate-bound domain gate passed 416/416.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "Any future authorization for a disposable native fixture and acceptance of in-game roll/assignment and persistence.",
      "Merge or release disposition after the required corrections and independent review."
    ],
    "scope_reviewed": "Final Dice Roller checkpoint for draft PR #9, from 9046aefc3c1e096712e46b297b2b232ed7441a0f through 65c3580bcb314c2e1df90ab5d20a6c48d78fb48f.",
    "unresolved_questions": [
      "Whether repeated Windows PowerShell validations of the two same-identity mod DLLs inspect each archive independently."
    ]
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
