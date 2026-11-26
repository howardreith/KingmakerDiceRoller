<!-- agent-review:v1 enrollment=kdr-umm0311-codex-umm-0311-baseline-20261123-20261123161619-b437 key=r08-review part=1/1 sha256=4720a91b23afc1ea3c8206cb53315921e4c05f2d654d405897b2009adc59b299 -->
### Codex GPT-6.2 Astra — review — round 8 — candidate `1a713be7b2`

**Decision:** `changes_required` — The build and package records establish the UMM 0.31.1.0 compile baseline. Two prior fixes are verified. Ordinary packaging can still accept a stale DLL when MSBuild selects a sibling targets file, and the configuration guard can refuse valid builds.

Continuation for Claude

Keep the correction within the receipt guard and its existing loader-baseline tests. Bind the targets file selected by tracked configuration, or refuse that redirection at build and package admission. Apply the configuration filter to shadowed root files, normalize the selected MSBuild path before identifying its toolset, and let an empty ImportGroup pass while retaining the check for nested imports. Use the existing disposable checkout to demonstrate stale-receipt refusal and valid reuse before rebuilding. Record the producing commit and run the configured domain gate on any corrected candidate within the existing controller budget. The gameplay code and native qualification are outside these corrections.

#### Findings

**R8-F1** · P1 · defect · blocks readiness — The fingerprint can omit a targets file selected by tracked configuration
- Location: `scripts/Common.ps1:385`; acceptance: mission §5, mission §6, OWNER AMENDMENT 2
- Trigger: Set DirectoryBuildTargetsPath in tracked Directory.Build.props to src/Scratch/Directory.Build.targets. Build with a neutral tracked targets file, then change only that file to enable overflow checking during real compilation and package the existing DLL.
- Expected: The selected targets file contributes to the receipt, or the guard explicitly refuses the redirection.
- Actual: The guard checks DirectoryBuildTargetsPath only in the environment. A property in tracked Directory.Build.props passes its XML check. The directory walk misses the selected sibling, and line 385 removes it from the fingerprint. Because package evaluation sets SkipCompilerExecution=true, the production-only change also leaves the evaluated compiler arguments unchanged. Before this round the fingerprint included that file under src.
- Why it blocks / does not: Ordinary Package.ps1 can certify a frozen DLL after an active compiler setting changes. This defeats the mission's material-input requirement and regresses the previous fingerprint.
- Requested correction: Bind the targets file MSBuild actually selects, or refuse the selector that makes the directory walk inaccurate. Preserve acceptance of an unused sibling file.
- Requested evidence: Use the disposable checkout to show that ordinary packaging rejects the old receipt after only the selected file changes, or that build and package admission both refuse the redirection. Retain the unused-sibling positive control.

**R6-F1** · P2 · defect · blocks readiness — Inactive configuration can still block a valid build or invalidate its receipt
- Location: `scripts/Common.ps1:299`; acceptance: mission §5, mission §6, OWNER AMENDMENT 2
- Trigger: Supply the selected Current MSBuild executable through MSBUILD_EXE_PATH using forward slashes with a 15.0 hook present. A second case is adding a root Directory.Build.targets after building with a nearer tracked src/Directory.Build.targets.
- Expected: Equivalent paths to the same MSBuild executable select the same hooks. A targets file shadowed for the production project leaves its existing receipt usable.
- Actual: Resolve-MSBuild returns an existing MSBUILD_EXE_PATH verbatim. Line 299 recognizes only backslashes, so the forward-slash spelling selects the recursive all-toolsets hook scan. Separately, the filter at line 385 applies only below src and retains a root targets file even when the nearer file shadows it.
- Why it blocks / does not: An inactive user hook can still block the supported wrapper. A shadowed root file can still force a rebuild before ordinary packaging.
- Requested correction: Normalize the resolved executable path before matching its toolset. Apply the same applicability test to root and nested Directory.Build files while retaining active-input checks.
- Requested evidence: Package before rebuilding with the Current executable supplied using forward slashes and only the inactive hook present. Also show reuse after adding an empty root targets file shadowed by an already tracked src/Directory.Build.targets. Keep the active-hook and applicable-untracked-file refusal controls.

**R8-F2** · P2 · defect · blocks readiness — An empty ImportGroup prevents a build
- Location: `scripts/Common.ps1:281`; acceptance: mission §5, OWNER AMENDMENT 2
- Trigger: Run Build-Local.ps1 with a tracked Directory.Build.props containing an empty ImportGroup.
- Expected: An empty import group is accepted. A group containing an uncontrolled import is refused.
- Actual: The XPath matches ImportGroup itself. An empty group therefore produces the unsupported-import error before compilation. The same XPath already finds an actual Import nested inside a group.
- Why it blocks / does not: The round-8 guard prevents a previously valid build even though the configuration introduces no external input.
- Requested correction: Test for actual Import elements, including nested ones, and allow an empty ImportGroup.
- Requested evidence: Show a successful build and matching package with a tracked empty group, alongside refusal when the group contains the parent import.

**R8-F3** · P3 · optional_polish · non-blocking — The hook fixture assumes that Current is always active
- Location: `scripts/Test-LoaderBaseline.ps1:396`; acceptance: mission §6, mission §8
- Trigger: Use the boundary suite with a versioned 15.0 MSBuild executable.
- Expected: The active fixture directory corresponds to the toolset selected for the run.
- Actual: The fixture hard-codes 15.0 as inactive and Current as active, including in the expected refusal message. Those roles reverse if the selected toolset is 15.0.
- Why it blocks / does not: The recorded Windows run used Current and its result remains valid. Portability of this fixture to another toolset is a follow-up.
- Requested correction: Derive the active fixture directory from the selected toolset and choose a different directory for the inactive control.
- Requested evidence: If addressed, record the selected toolset and the two fixture directory names in the existing case.

#### Prior finding assessments

- **R6-F1** — `fix_rejected` — The e89e49e record demonstrates pre-rebuild reuse with src/Scratch/Directory.Build.targets and an inactive 15.0 hook on the measured Current path. The same inactive-hook refusal remains reachable with a forward-slash spelling of that executable. The fingerprint also retains a shadowed repository-root targets file.
- **R7-F1** — `fix_verified` — Build and package admission now refuse the explicit parent Import in the tracked root targets file. The recorded refusal preserved the DLL and receipt, and removing the import restored package acceptance without rebuilding. R8-F1 involves a separate selection mechanism.
- **R7-F2** — `fix_verified` — The guide and test comment now correctly identify the repository-local Assembly-CSharp path check and disclose the retained one-bit UnityEngine.UI.dll mutation, including for the historical 3874fc2 run. This matches the fixture construction.

#### Evidence assessment

- **Inspected:** The effective mission, OWNER AMENDMENT 1 and OWNER AMENDMENT 2, AGENTS.md, feature diff, developer report, transcript notice and pending findings ledger.; The production project, loader pins and MSBuild targets, build and package entry points, standalone validator, correction diff, regression fixtures and compatibility records.
- **Executed verification:** The controller receipt and matching log digest confirm that scripts/Test-Domain.ps1 passed 416/416 at 1a713be7b2020265225b80db96d93006968efd8a.; The committed loader-baseline.json contains 147 distinct passing cases from e89e49e. Its clean Release build produced DLL SHA-256 634f22775c62bb34461fb202d85ab9d088f4e1cb1bd062e959812c8bcf9b12ee and ZIP SHA-256 b8cb456b8061fc48bfa464bd5af66f368516ab24cbb22d3d1f2c6e376d436e81. Git confirms that only the evidence index and suite record changed between that commit and the candidate.; The suite records acceptance of the baseline archive and standalone rejection of the genuine 0.33.0-compiled v0.2.0 DLL with only its packaged manifest lowered. The earlier full build and package records retain their producing commit, 8c198e0.; I verified the supplied mission and amendment hashes and all 12 indexed evidence-file digests. Read-only PowerShell checks confirmed the path-filter, toolset-regex and XML-selector results cited in the findings.
- **Source inspection only:** I traced the consequences of the selector results through Build-Local.ps1, Assert-PinnedBuildReceipt and ordinary Package.ps1. The new failure scenarios were not executed end to end in the reviewer snapshot.; The static audit records resolution of seven types and 25 members for the same DLL on UMM 0.31.1, 0.32.4, the owner's measured 0.33.0 and the selected 0.33.4. I inspected the audit implementation and records.
- **Required gaps:** Ordinary package admission must reject reuse after a material change to a selected sibling targets file.; Valid builds and receipts must survive the inactive configurations identified in R6-F1 and the empty import group in R8-F2.
- **Limitations:** The foreground transcript was withheld. The loader suite is supported by its structured output at e89e49e. The domain gate has a controller receipt at the exact candidate.; The read-only review precluded rerunning builds or packaging. Native roll, assignment, persistence, mercenary and respec behavior remain unobserved on all four loader rows. The repository documents a live-install runtime route and lacks an approved disposable harness.

#### Summary

- **Delivered behavior:** Info.json declares UMM 0.31.1. The recorded Release DLL references UnityModManager 0.31.1.0 and Harmony12 1.2.0.1, and the standalone validator rejects the legacy manifest-only downgrade. Compatibility evidence is static for all four loader versions.
- **Scope reviewed:** Dice Roller draft PR #9 at the fixed final candidate, with emphasis on the three pending findings, the changed configuration guard and its build, package and test consumers.
- **Pending later-phase work:** none
- **Unresolved questions:** none
- **Remaining owner review:** Disposition of draft PR #9 under the existing controller budget.; Whether to pursue disposable native qualification in a separate task.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: revision 2, sha256 `91d17a7803771af4f86658385d844edd7374dca390c475ddce990f50272d869d`.

Codex CLI `0.170.0`, thread `01a21243-1318-7123-852a-6acf1f60f239`, duration 803.5s, response sha256 `08ded96c9dc309a313c244c0f86c2958b32521ab7474f1e61aeff8a109a45e2d`, request `kdr-umm0311-codex-umm-0311-baseline-20261123-20261123161619-b437-r08-7915d3`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "1a713be7b2020265225b80db96d93006968efd8a",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The build and package records establish the UMM 0.31.1.0 compile baseline. Two prior fixes are verified. Ordinary packaging can still accept a stale DLL when MSBuild selects a sibling targets file, and the configuration guard can refuse valid builds.\n\nContinuation for Claude\n\nKeep the correction within the receipt guard and its existing loader-baseline tests. Bind the targets file selected by tracked configuration, or refuse that redirection at build and package admission. Apply the configuration filter to shadowed root files, normalize the selected MSBuild path before identifying its toolset, and let an empty ImportGroup pass while retaining the check for nested imports. Use the existing disposable checkout to demonstrate stale-receipt refusal and valid reuse before rebuilding. Record the producing commit and run the configured domain gate on any corrected candidate within the existing controller budget. The gameplay code and native qualification are outside these corrections.",
  "evidence_assessment": {
    "executed_verification": [
      "The controller receipt and matching log digest confirm that scripts/Test-Domain.ps1 passed 416/416 at 1a713be7b2020265225b80db96d93006968efd8a.",
      "The committed loader-baseline.json contains 147 distinct passing cases from e89e49e. Its clean Release build produced DLL SHA-256 634f22775c62bb34461fb202d85ab9d088f4e1cb1bd062e959812c8bcf9b12ee and ZIP SHA-256 b8cb456b8061fc48bfa464bd5af66f368516ab24cbb22d3d1f2c6e376d436e81. Git confirms that only the evidence index and suite record changed between that commit and the candidate.",
      "The suite records acceptance of the baseline archive and standalone rejection of the genuine 0.33.0-compiled v0.2.0 DLL with only its packaged manifest lowered. The earlier full build and package records retain their producing commit, 8c198e0.",
      "I verified the supplied mission and amendment hashes and all 12 indexed evidence-file digests. Read-only PowerShell checks confirmed the path-filter, toolset-regex and XML-selector results cited in the findings."
    ],
    "inspected": [
      "The effective mission, OWNER AMENDMENT 1 and OWNER AMENDMENT 2, AGENTS.md, feature diff, developer report, transcript notice and pending findings ledger.",
      "The production project, loader pins and MSBuild targets, build and package entry points, standalone validator, correction diff, regression fixtures and compatibility records."
    ],
    "limitations": [
      "The foreground transcript was withheld. The loader suite is supported by its structured output at e89e49e. The domain gate has a controller receipt at the exact candidate.",
      "The read-only review precluded rerunning builds or packaging. Native roll, assignment, persistence, mercenary and respec behavior remain unobserved on all four loader rows. The repository documents a live-install runtime route and lacks an approved disposable harness."
    ],
    "required_gaps": [
      "Ordinary package admission must reject reuse after a material change to a selected sibling targets file.",
      "Valid builds and receipts must survive the inactive configurations identified in R6-F1 and the empty import group in R8-F2."
    ],
    "source_inspection_only": [
      "I traced the consequences of the selector results through Build-Local.ps1, Assert-PinnedBuildReceipt and ordinary Package.ps1. The new failure scenarios were not executed end to end in the reviewer snapshot.",
      "The static audit records resolution of seven types and 25 members for the same DLL on UMM 0.31.1, 0.32.4, the owner's measured 0.33.0 and the selected 0.33.4. I inspected the audit implementation and records."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76",
        "OWNER AMENDMENT 2"
      ],
      "actual": "The guard checks DirectoryBuildTargetsPath only in the environment. A property in tracked Directory.Build.props passes its XML check. The directory walk misses the selected sibling, and line 385 removes it from the fingerprint. Because package evaluation sets SkipCompilerExecution=true, the production-only change also leaves the evaluated compiler arguments unchanged. Before this round the fingerprint included that file under src.",
      "blocking_rationale": "Ordinary Package.ps1 can certify a frozen DLL after an active compiler setting changes. This defeats the mission's material-input requirement and regresses the previous fingerprint.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The selected targets file contributes to the receipt, or the guard explicitly refuses the redirection.",
      "id": "R8-F1",
      "location": {
        "file": "scripts/Common.ps1",
        "line": 385
      },
      "priority": "P1",
      "requested_correction": "Bind the targets file MSBuild actually selects, or refuse the selector that makes the directory walk inaccurate. Preserve acceptance of an unused sibling file.",
      "requested_evidence": "Use the disposable checkout to show that ordinary packaging rejects the old receipt after only the selected file changes, or that build and package admission both refuse the redirection. Retain the unused-sibling positive control.",
      "title": "The fingerprint can omit a targets file selected by tracked configuration",
      "trigger": "Set DirectoryBuildTargetsPath in tracked Directory.Build.props to src/Scratch/Directory.Build.targets. Build with a neutral tracked targets file, then change only that file to enable overflow checking during real compilation and package the existing DLL."
    },
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76",
        "OWNER AMENDMENT 2"
      ],
      "actual": "Resolve-MSBuild returns an existing MSBUILD_EXE_PATH verbatim. Line 299 recognizes only backslashes, so the forward-slash spelling selects the recursive all-toolsets hook scan. Separately, the filter at line 385 applies only below src and retains a root targets file even when the nearer file shadows it.",
      "blocking_rationale": "An inactive user hook can still block the supported wrapper. A shadowed root file can still force a rebuild before ordinary packaging.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Equivalent paths to the same MSBuild executable select the same hooks. A targets file shadowed for the production project leaves its existing receipt usable.",
      "id": "R6-F1",
      "location": {
        "file": "scripts/Common.ps1",
        "line": 299
      },
      "priority": "P2",
      "requested_correction": "Normalize the resolved executable path before matching its toolset. Apply the same applicability test to root and nested Directory.Build files while retaining active-input checks.",
      "requested_evidence": "Package before rebuilding with the Current executable supplied using forward slashes and only the inactive hook present. Also show reuse after adding an empty root targets file shadowed by an already tracked src/Directory.Build.targets. Keep the active-hook and applicable-untracked-file refusal controls.",
      "title": "Inactive configuration can still block a valid build or invalidate its receipt",
      "trigger": "Supply the selected Current MSBuild executable through MSBUILD_EXE_PATH using forward slashes with a 15.0 hook present. A second case is adding a root Directory.Build.targets after building with a nearer tracked src/Directory.Build.targets."
    },
    {
      "acceptance_ids": [
        "mission \u00a75",
        "OWNER AMENDMENT 2"
      ],
      "actual": "The XPath matches ImportGroup itself. An empty group therefore produces the unsupported-import error before compilation. The same XPath already finds an actual Import nested inside a group.",
      "blocking_rationale": "The round-8 guard prevents a previously valid build even though the configuration introduces no external input.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "An empty import group is accepted. A group containing an uncontrolled import is refused.",
      "id": "R8-F2",
      "location": {
        "file": "scripts/Common.ps1",
        "line": 281
      },
      "priority": "P2",
      "requested_correction": "Test for actual Import elements, including nested ones, and allow an empty ImportGroup.",
      "requested_evidence": "Show a successful build and matching package with a tracked empty group, alongside refusal when the group contains the parent import.",
      "title": "An empty ImportGroup prevents a build",
      "trigger": "Run Build-Local.ps1 with a tracked Directory.Build.props containing an empty ImportGroup."
    },
    {
      "acceptance_ids": [
        "mission \u00a76",
        "mission \u00a78"
      ],
      "actual": "The fixture hard-codes 15.0 as inactive and Current as active, including in the expected refusal message. Those roles reverse if the selected toolset is 15.0.",
      "blocking_rationale": "The recorded Windows run used Current and its result remains valid. Portability of this fixture to another toolset is a follow-up.",
      "blocks_readiness": false,
      "category": "optional_polish",
      "expected": "The active fixture directory corresponds to the toolset selected for the run.",
      "id": "R8-F3",
      "location": {
        "file": "scripts/Test-LoaderBaseline.ps1",
        "line": 396
      },
      "priority": "P3",
      "requested_correction": "Derive the active fixture directory from the selected toolset and choose a different directory for the inactive control.",
      "requested_evidence": "If addressed, record the selected toolset and the two fixture directory names in the existing case.",
      "title": "The hook fixture assumes that Current is always active",
      "trigger": "Use the boundary suite with a versioned 15.0 MSBuild executable."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_rejected",
      "finding_id": "R6-F1",
      "rationale": "The e89e49e record demonstrates pre-rebuild reuse with src/Scratch/Directory.Build.targets and an inactive 15.0 hook on the measured Current path. The same inactive-hook refusal remains reachable with a forward-slash spelling of that executable. The fingerprint also retains a shadowed repository-root targets file."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R7-F1",
      "rationale": "Build and package admission now refuse the explicit parent Import in the tracked root targets file. The recorded refusal preserved the DLL and receipt, and removing the import restored package acceptance without rebuilding. R8-F1 involves a separate selection mechanism."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R7-F2",
      "rationale": "The guide and test comment now correctly identify the repository-local Assembly-CSharp path check and disclose the retained one-bit UnityEngine.UI.dll mutation, including for the historical 3874fc2 run. This matches the fixture construction."
    }
  ],
  "request_id": "kdr-umm0311-codex-umm-0311-baseline-20261123-20261123161619-b437-r08-7915d3",
  "round": 8,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "Info.json declares UMM 0.31.1. The recorded Release DLL references UnityModManager 0.31.1.0 and Harmony12 1.2.0.1, and the standalone validator rejects the legacy manifest-only downgrade. Compatibility evidence is static for all four loader versions.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "Disposition of draft PR #9 under the existing controller budget.",
      "Whether to pursue disposable native qualification in a separate task."
    ],
    "scope_reviewed": "Dice Roller draft PR #9 at the fixed final candidate, with emphasis on the three pending findings, the changed configuration guard and its build, package and test consumers.",
    "unresolved_questions": []
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
