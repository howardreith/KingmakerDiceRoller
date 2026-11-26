<!-- agent-review:v1 enrollment=kdr-umm0311-codex-umm-0311-baseline-20261123-20261123161619-b437 key=r07-review part=1/1 sha256=e5fae118003b2da9e156a516c981951e8c25e1438ef57f51ae9cd8ae23f1d555 -->
### Codex GPT-6.2 Astra — review — round 7 — candidate `d060b534ef`

**Decision:** `changes_required` — The production build and packaged DLL substantiate the UMM 0.31.1 baseline. Two configuration checks still make incorrect decisions about reusing that DLL, and the guide overstates one test fixture’s byte identity.

## Continuation for Claude

Keep the correction in receipt admission and its existing tests. Account for an explicitly imported parent configuration, or refuse that import when its inputs cannot be bound. Preserve reuse when a configuration file lies outside the production project’s inputs. Exercise both cases against an existing receipt before rebuilding. Correct the Managed fixture description, or restore and verify its original bytes if retaining the stronger claim. Use the existing domain gate and focused checks within the authorized budget. The additional UI and source-report binding covered by OWNER AMENDMENT 2 remains separate follow-up work.

#### Findings

**R7-F1** · P1 · defect · blocks readiness — An explicitly imported parent can change compilation without invalidating the receipt
- Location: `scripts/Common.ps1:276`; acceptance: mission §5, mission §6
- Trigger: A tracked root Directory.Build.targets imports its parent’s Directory.Build.targets. After building, the parent enables overflow checking only for real compilation.
- Expected: Package admission detects the changed material input or refuses the uncontrolled import.
- Actual: The search stops at the tracked root file. The fingerprint includes that file but omits its imported parent outside the checkout. The compiler-skipping evaluation also omits the production-only setting, so the old inventory and fingerprint still match. The previous guard refused this outside parent.
- Why it blocks / does not: Ordinary packaging can certify a frozen DLL after an active production compiler setting changes. This is a regression introduced by the correction to the configuration guard.
- Requested correction: Bind the parent reached by an explicit import, or fail closed on that import. Keep acceptance of a genuinely shadowed parent.
- Requested evidence: In the disposable checkout, use a tracked root targets file that imports a parent. After a clean build, change only the parent’s production-only overflow setting and show that ordinary packaging refuses the old receipt. Retain the shadowed-parent positive control.

**R6-F1** · P2 · defect · blocks readiness — Unrelated MSBuild configuration can still block build or package admission
- Location: `scripts/Common.ps1:349`; acceptance: mission §5, mission §6
- Trigger: After a clean build, add a nonignored src/Scratch/Directory.Build.targets beside the production project, then run ordinary Package.ps1.
- Expected: A configuration file outside the production project’s inputs leaves the existing receipt usable.
- Actual: The fingerprint at line 349 still hashes every nonignored file under src, including an unrelated sibling’s targets file. Separately, the per-user scan at line 300 rejects every ImportBefore or ImportAfter file under the user’s MSBuild directory, including hooks belonging to an inactive toolset.
- Why it blocks / does not: An unrelated file can still force a rebuild of the frozen candidate. An inactive per-user hook can prevent both the build and ordinary package admission.
- Requested correction: Exclude unrelated sibling configuration from the fingerprint and restrict the per-user hook check to locations applicable to the selected production toolset. Preserve refusal of active uncontrolled imports.
- Requested evidence: Package the existing DLL before rebuilding after adding src/Scratch/Directory.Build.targets. Show that an inactive hook in a disposable user profile permits the build and package, while the applicable untracked src/Directory.Build.targets is still refused.

**R7-F2** · P3 · evidence_gap · blocks readiness — The repository-local Managed test reused a modified Unity UI assembly
- Location: `scripts/Test-LoaderBaseline.ps1:665`; acceptance: mission §6, mission §8
- Trigger: Run the repository-local candidate case after the changed-Managed-reference negative control in the same suite.
- Expected: The reproduction guide and test description identify the actual input used for the positive control.
- Actual: The earlier negative control flips the last byte of UnityEngine.UI.dll at lines 356–358. Line 665 copies that directory into artifacts without restoring the file. The later case checks the Assembly-CSharp path and successful packaging. The guide calls the Managed copy byte-identical.
- Why it blocks / does not: The recorded case demonstrates the path-lookup fix, but its stated fixture identity is incorrect. Accurate reporting of the focused validation is required.
- Requested correction: Describe the existing run as a repository-local Assembly-CSharp path test and disclose the retained UI mutation. Alternatively, restore the original UI file and verify its digest before claiming byte identity in a new run.
- Requested evidence: An accurate annotation of the 3874fc2 result is sufficient. If retaining the byte-identical claim, provide the matching fixture digests and the focused result.

#### Prior finding assessments

- **R4-F1** — `fix_verified` — The tracked root Directory.Build.targets itself now contributes to the fingerprint. The recorded case refused the stale package, preserved the earlier receipt after the refused build, and reproduced the original DLL after restoration. R7-F1 concerns a separate file reached through an explicit import.
- **R5-F3** — `fix_verified` — The inventory reader requires /noconfig. The disposable compiler run retained that argument and reproduced the DLL despite its default response file. Removing the argument from the captured list produced the expected refusal.
- **R5-F5** — `rebuttal_accepted` — OWNER AMENDMENT 2 withdrew the requirement to extend qualification-report binding beyond the UMM baseline, agreed compatibility evidence and mission regressions. At the merge base, candidate packaging copied the UI and source reports without binding their Unity UI or source identities. The candidate preserves the DLL, game-reference and manifest checks, and independently verifies the packaged loader references. Further UI and source-report binding remains a valid separate follow-up.
- **R6-F1** — `fix_rejected` — The shadowed parent props and tools-directory targets cases now pass, and the active untracked src targets case is still refused. The recursive src fingerprint and per-user hook scan still treat unrelated configuration as applicable. The new positive control rebuilds before packaging and therefore does not establish reuse of the original receipt.
- **R6-F2** — `fix_verified` — Package.ps1 recognizes both the bare filename and a repository-relative path while requiring exactly one game reference and comparing its digest. The recorded local candidate used artifacts/managed copy/Assembly-CSharp.dll and published successfully. The conflicting respec report was refused. R7-F2 concerns the description of a different assembly in that fixture.
- **R6-F3** — `fix_verified` — The guide now places the overflow and BaseAddress controls in tracked Directory.Build.props and describes the GamePath.props refusal. The current index identifies 140/140 at 3874fc2 and distinguishes the historical 111/111 run.

#### Evidence assessment

- **Inspected:** The mission, both owner amendments, AGENTS.md, the six pending findings, the developer report, the supplied feature diff and the correction commits.; The production project, pin verifier, MSBuild targets, build and package scripts, standalone validator, qualification consumers, test fixtures, compatibility guide and versioned evidence.
- **Executed verification:** The controller receipt records 416/416 domain tests passing at d060b534ef7c2d44a75bad5d533191eb0dc6ceaa with exit code 0 and an unchanged worktree.; The structured suite output records 140/140 cases at 3874fc2. Direct MSBuild selected both pinned files, all 13 compiler references matched the receipt, and the standalone validator rejected the genuine 0.33.0 release DLL with only its manifest lowered. Git shows that only the evidence index and suite output changed afterward.; Read-only checks confirmed all 12 indexed evidence hashes and all 107 repository source hashes in the earlier build receipt. The build, package and static audit records agree on DLL SHA-256 634f22775c62bb34461fb202d85ab9d088f4e1cb1bd062e959812c8bcf9b12ee and MVID 7a6d0a75-f788-47a5-83c9-fed6fb14e3ff. The suite and package records agree on ZIP SHA-256 b8cb456b8061fc48bfa464bd5af66f368516ab24cbb22d3d1f2c6e376d436e81.
- **Source inspection only:** The newly identified configuration cases follow from the nearest-file search, the recursive file scans and the compiler-skipping receipt evaluation. These cases were not executed in the read-only review.; The Managed fixture discrepancy follows from the earlier byte mutation and the later copy of that same directory.
- **Required gaps:** Refuse stale receipt reuse when an admitted targets file explicitly imports a changed parent.; Preserve ordinary package reuse and build availability in the presence of unrelated MSBuild configuration.; Align the repository-local Managed fixture claim with the bytes used in the recorded run.
- **Limitations:** The controller withheld the mixed foreground transcript. The domain result has a candidate-bound controller receipt. The build, package and boundary-suite results were assessed from their structured records at the stated producing commits.; Native behavior is NOT RUN on 0.31.1, 0.32.4, the owner’s 0.33.0 and 0.33.4. The repository has no approved disposable game harness. The four-loader results establish static API resolution only. Combined-stack behavior is untested.; Interactive IDE behavior was unobserved. OWNER AMENDMENT 1 removed the need for an IDE-specific check here.

#### Summary

- **Delivered behavior:** Dice Roller declares UMM 0.31.1 and compiles against UnityModManager 0.31.1.0 and Harmony12 1.2.0.1. The standalone validator accepts the baseline package and rejects the genuine newer-compiled release DLL with a lowered manifest. Static checks resolved seven types and 25 members on all four loader profiles.
- **Scope reviewed:** The final Dice Roller candidate for draft PR #9 under owner amendment revision 2, including the six pending findings and the affected build, receipt, package and evidence paths.
- **Pending later-phase work:** none
- **Unresolved questions:** none
- **Remaining owner review:** The owner controls any future native qualification, merge or release decision.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: revision 2, sha256 `91d17a7803771af4f86658385d844edd7374dca390c475ddce990f50272d869d`.

Codex CLI `0.170.0`, thread `01a20f7a-1296-73e3-ad54-fda59160008d`, duration 623.1s, response sha256 `4c7d0c8634d16b45fd58f0f4627fa9df789a41e052aa6cd38487cf870036033e`, request `kdr-umm0311-codex-umm-0311-baseline-20261123-20261123161619-b437-r07-91a26c`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "d060b534ef7c2d44a75bad5d533191eb0dc6ceaa",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The production build and packaged DLL substantiate the UMM 0.31.1 baseline. Two configuration checks still make incorrect decisions about reusing that DLL, and the guide overstates one test fixture\u2019s byte identity.\n\n## Continuation for Claude\n\nKeep the correction in receipt admission and its existing tests. Account for an explicitly imported parent configuration, or refuse that import when its inputs cannot be bound. Preserve reuse when a configuration file lies outside the production project\u2019s inputs. Exercise both cases against an existing receipt before rebuilding. Correct the Managed fixture description, or restore and verify its original bytes if retaining the stronger claim. Use the existing domain gate and focused checks within the authorized budget. The additional UI and source-report binding covered by OWNER AMENDMENT 2 remains separate follow-up work.",
  "evidence_assessment": {
    "executed_verification": [
      "The controller receipt records 416/416 domain tests passing at d060b534ef7c2d44a75bad5d533191eb0dc6ceaa with exit code 0 and an unchanged worktree.",
      "The structured suite output records 140/140 cases at 3874fc2. Direct MSBuild selected both pinned files, all 13 compiler references matched the receipt, and the standalone validator rejected the genuine 0.33.0 release DLL with only its manifest lowered. Git shows that only the evidence index and suite output changed afterward.",
      "Read-only checks confirmed all 12 indexed evidence hashes and all 107 repository source hashes in the earlier build receipt. The build, package and static audit records agree on DLL SHA-256 634f22775c62bb34461fb202d85ab9d088f4e1cb1bd062e959812c8bcf9b12ee and MVID 7a6d0a75-f788-47a5-83c9-fed6fb14e3ff. The suite and package records agree on ZIP SHA-256 b8cb456b8061fc48bfa464bd5af66f368516ab24cbb22d3d1f2c6e376d436e81."
    ],
    "inspected": [
      "The mission, both owner amendments, AGENTS.md, the six pending findings, the developer report, the supplied feature diff and the correction commits.",
      "The production project, pin verifier, MSBuild targets, build and package scripts, standalone validator, qualification consumers, test fixtures, compatibility guide and versioned evidence."
    ],
    "limitations": [
      "The controller withheld the mixed foreground transcript. The domain result has a candidate-bound controller receipt. The build, package and boundary-suite results were assessed from their structured records at the stated producing commits.",
      "Native behavior is NOT RUN on 0.31.1, 0.32.4, the owner\u2019s 0.33.0 and 0.33.4. The repository has no approved disposable game harness. The four-loader results establish static API resolution only. Combined-stack behavior is untested.",
      "Interactive IDE behavior was unobserved. OWNER AMENDMENT 1 removed the need for an IDE-specific check here."
    ],
    "required_gaps": [
      "Refuse stale receipt reuse when an admitted targets file explicitly imports a changed parent.",
      "Preserve ordinary package reuse and build availability in the presence of unrelated MSBuild configuration.",
      "Align the repository-local Managed fixture claim with the bytes used in the recorded run."
    ],
    "source_inspection_only": [
      "The newly identified configuration cases follow from the nearest-file search, the recursive file scans and the compiler-skipping receipt evaluation. These cases were not executed in the read-only review.",
      "The Managed fixture discrepancy follows from the earlier byte mutation and the later copy of that same directory."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76"
      ],
      "actual": "The search stops at the tracked root file. The fingerprint includes that file but omits its imported parent outside the checkout. The compiler-skipping evaluation also omits the production-only setting, so the old inventory and fingerprint still match. The previous guard refused this outside parent.",
      "blocking_rationale": "Ordinary packaging can certify a frozen DLL after an active production compiler setting changes. This is a regression introduced by the correction to the configuration guard.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Package admission detects the changed material input or refuses the uncontrolled import.",
      "id": "R7-F1",
      "location": {
        "file": "scripts/Common.ps1",
        "line": 276
      },
      "priority": "P1",
      "requested_correction": "Bind the parent reached by an explicit import, or fail closed on that import. Keep acceptance of a genuinely shadowed parent.",
      "requested_evidence": "In the disposable checkout, use a tracked root targets file that imports a parent. After a clean build, change only the parent\u2019s production-only overflow setting and show that ordinary packaging refuses the old receipt. Retain the shadowed-parent positive control.",
      "title": "An explicitly imported parent can change compilation without invalidating the receipt",
      "trigger": "A tracked root Directory.Build.targets imports its parent\u2019s Directory.Build.targets. After building, the parent enables overflow checking only for real compilation."
    },
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76"
      ],
      "actual": "The fingerprint at line 349 still hashes every nonignored file under src, including an unrelated sibling\u2019s targets file. Separately, the per-user scan at line 300 rejects every ImportBefore or ImportAfter file under the user\u2019s MSBuild directory, including hooks belonging to an inactive toolset.",
      "blocking_rationale": "An unrelated file can still force a rebuild of the frozen candidate. An inactive per-user hook can prevent both the build and ordinary package admission.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A configuration file outside the production project\u2019s inputs leaves the existing receipt usable.",
      "id": "R6-F1",
      "location": {
        "file": "scripts/Common.ps1",
        "line": 349
      },
      "priority": "P2",
      "requested_correction": "Exclude unrelated sibling configuration from the fingerprint and restrict the per-user hook check to locations applicable to the selected production toolset. Preserve refusal of active uncontrolled imports.",
      "requested_evidence": "Package the existing DLL before rebuilding after adding src/Scratch/Directory.Build.targets. Show that an inactive hook in a disposable user profile permits the build and package, while the applicable untracked src/Directory.Build.targets is still refused.",
      "title": "Unrelated MSBuild configuration can still block build or package admission",
      "trigger": "After a clean build, add a nonignored src/Scratch/Directory.Build.targets beside the production project, then run ordinary Package.ps1."
    },
    {
      "acceptance_ids": [
        "mission \u00a76",
        "mission \u00a78"
      ],
      "actual": "The earlier negative control flips the last byte of UnityEngine.UI.dll at lines 356\u2013358. Line 665 copies that directory into artifacts without restoring the file. The later case checks the Assembly-CSharp path and successful packaging. The guide calls the Managed copy byte-identical.",
      "blocking_rationale": "The recorded case demonstrates the path-lookup fix, but its stated fixture identity is incorrect. Accurate reporting of the focused validation is required.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "The reproduction guide and test description identify the actual input used for the positive control.",
      "id": "R7-F2",
      "location": {
        "file": "scripts/Test-LoaderBaseline.ps1",
        "line": 665
      },
      "priority": "P3",
      "requested_correction": "Describe the existing run as a repository-local Assembly-CSharp path test and disclose the retained UI mutation. Alternatively, restore the original UI file and verify its digest before claiming byte identity in a new run.",
      "requested_evidence": "An accurate annotation of the 3874fc2 result is sufficient. If retaining the byte-identical claim, provide the matching fixture digests and the focused result.",
      "title": "The repository-local Managed test reused a modified Unity UI assembly",
      "trigger": "Run the repository-local candidate case after the changed-Managed-reference negative control in the same suite."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R4-F1",
      "rationale": "The tracked root Directory.Build.targets itself now contributes to the fingerprint. The recorded case refused the stale package, preserved the earlier receipt after the refused build, and reproduced the original DLL after restoration. R7-F1 concerns a separate file reached through an explicit import."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R5-F3",
      "rationale": "The inventory reader requires /noconfig. The disposable compiler run retained that argument and reproduced the DLL despite its default response file. Removing the argument from the captured list produced the expected refusal."
    },
    {
      "assessment": "rebuttal_accepted",
      "finding_id": "R5-F5",
      "rationale": "OWNER AMENDMENT 2 withdrew the requirement to extend qualification-report binding beyond the UMM baseline, agreed compatibility evidence and mission regressions. At the merge base, candidate packaging copied the UI and source reports without binding their Unity UI or source identities. The candidate preserves the DLL, game-reference and manifest checks, and independently verifies the packaged loader references. Further UI and source-report binding remains a valid separate follow-up."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R6-F1",
      "rationale": "The shadowed parent props and tools-directory targets cases now pass, and the active untracked src targets case is still refused. The recursive src fingerprint and per-user hook scan still treat unrelated configuration as applicable. The new positive control rebuilds before packaging and therefore does not establish reuse of the original receipt."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R6-F2",
      "rationale": "Package.ps1 recognizes both the bare filename and a repository-relative path while requiring exactly one game reference and comparing its digest. The recorded local candidate used artifacts/managed copy/Assembly-CSharp.dll and published successfully. The conflicting respec report was refused. R7-F2 concerns the description of a different assembly in that fixture."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R6-F3",
      "rationale": "The guide now places the overflow and BaseAddress controls in tracked Directory.Build.props and describes the GamePath.props refusal. The current index identifies 140/140 at 3874fc2 and distinguishes the historical 111/111 run."
    }
  ],
  "request_id": "kdr-umm0311-codex-umm-0311-baseline-20261123-20261123161619-b437-r07-91a26c",
  "round": 7,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "Dice Roller declares UMM 0.31.1 and compiles against UnityModManager 0.31.1.0 and Harmony12 1.2.0.1. The standalone validator accepts the baseline package and rejects the genuine newer-compiled release DLL with a lowered manifest. Static checks resolved seven types and 25 members on all four loader profiles.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "The owner controls any future native qualification, merge or release decision."
    ],
    "scope_reviewed": "The final Dice Roller candidate for draft PR #9 under owner amendment revision 2, including the six pending findings and the affected build, receipt, package and evidence paths.",
    "unresolved_questions": []
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
