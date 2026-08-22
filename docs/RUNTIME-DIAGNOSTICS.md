# Runtime diagnostics

The UMM panel is an operational and recovery surface. The native ability page
is the primary product UI.

## UMM status

The panel reports:

- product version and current workflow mode;
- accepted/rejected context totals, verified applications, and releases;
- exact contract resolution and Assembly-CSharp MVID;
- detected compatibility mods and warnings;
- history/saved-array counts;
- emergency Return to Point Buy while Roll Mode is active;
- recent deduplicated facts when verbose diagnostics are enabled.

Rejection counts include repeated observations, but visible rejection messages
are deduplicated by stable reason. This prevents constructor noise from hiding
the decisive invariant.

## Context facts

Context diagnostics use stable Boolean relations instead of object dumps:

```text
mode
isFirstLevel
candidateMainFlag
candidatePlayerFlag
controllerStateMatches
controllerUnitMatches
controllerPreviewMatches
mainCharacterPresent
mainMatchesCandidate
mainMatchesControllerUnit
mainRelation
```

Unresolved identity is a rejection, never an implicit acceptance.

## Session and application facts

Key fields include:

```text
mode
pointBuyOriginCaptured
pointBuyOriginGeneration
currentGeneration
applicationGeneration
candidateBaselineContaminated
pendingReplacementObserved
reboundPreview
sameStableOwner
rollSuppressedForStableOwner
```

A successful Roll/Reroll/Recall/reassignment reports that the live controller
model, allocator, native controls, and presentation were verified before the
workflow commit. A same-owner preview replacement is reported as a rebind, not
as a second session or new roll.

## Point-buy restoration facts

Restoration distinguishes semantic safety from presentation synchronization:

```text
semanticPointBuyVerified
presentationRefreshRequested
presentationRefreshMethod
presentationRefreshCount
activeAbilityPhaseFound
abilityPhaseStateMatchesSession
abilityPhaseDistributionMatchesSession
abilityPhaseViewModelMatchesSession
postRefreshGeneration
postRefreshLiveModelVerified
allocatorBudget
liveDistributionMatchesPointBuyOrigin
liveUnitMatchesPointBuyOrigin
mode=PointBuy
rollSuppressedForStableOwner=true
```

If semantic restoration succeeds but native refresh fails, the safe PointBuy
model is retained and the failure is reported. Stale labels alone do not cause
rolled values to be reapplied.

## Saved-data warnings

Malformed or unsupported saved-array records are skipped individually during
load. Each skipped slot produces a concise warning without dumping serialized
contents. Valid records continue to load.

## Evidence collection

After a live attempt, fully exit Kingmaker and run:

```powershell
powershell -NoLogo -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\Collect-RuntimeEvidence.ps1
```

Inspect the complete chronological `output_log.txt`, not only the UMM panel's
recent excerpt. Evidence directories are ignored local artifacts. Never commit
logs, screenshots, saves, local paths, or copied game files.
