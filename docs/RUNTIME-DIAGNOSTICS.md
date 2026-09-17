# Runtime diagnostics

The UMM panel is an operational and recovery surface. The native ability page
is the primary product UI.

## UMM status

The panel reports:

- product version and current workflow mode;
- accepted/rejected context totals, verified preview applications, final
  mercenary PASS/FAIL totals, and releases;
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
accepted creation kind
mode
isFirstLevel
candidateMainFlag
candidatePlayerFlag
candidatePetFlag
candidateEnemyFlag
controllerStateMatches
controllerUnitMatches
controllerPreviewMatches
mainCharacterPresent
mainMatchesCandidate
mainMatchesControllerUnit
mainRelation
mercenaryStateEmployee
mercenaryStableOwnerCustom
mercenaryDiscriminator
stableOwnerSource
observed allocator budget/provenance
```

The mercenary discriminator is reported as exact
`LevelUpState.IsEmployee + UnitHelper.IsCustomCompanion(controller source)`
evidence, not a raw object. A rejected context records the first failed
requirement. Unresolved identity and `DifferentFromCandidate` without exact
mercenary evidence or exact respec ownership are rejections, never implicit acceptance.

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

## Mercenary finalization facts

Preview verification and final-character verification are separate events. A
successful mercenary completion produces one concise `FINAL PASS` record after
Kingmaker's success callback. A failure produces `FINAL FAIL`; it is never
reported as committed merely because the preview matched.

```text
creationKind=Mercenary
controller=<type@bounded identity>
source=<type@bounded identity>
preview=<type@bounded identity>
final=<type@bounded identity>
expectedBase=[STR,DEX,CON,INT,WIS,CHA]
observedFinalBase=[STR,DEX,CON,INT,WIS,CHA]
passed=true|false
failure=<first failed invariant, when present>
```

The authoritative-write hook is silent for preview targets and unrelated
controllers. A pre-callback ownership/application failure is bounded and logged
once by its deduplicated facts. The final record reads the accepted stable
descriptor; racial modifiers are intentionally absent from both arrays.

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

## Native panel presentation facts

The runtime records the compact **Roll Stats** horizontal geometry source as
`RacialBonusContainer`, `AllocatorFrame`, `AllocatorRegion`, or
`AbilityPhaseRoot`. Every source uses the same bounded bottom-center placement;
none is an upper-right fallback. Attachment and cleanup failures identify the
first exact UI contract that could not be resolved. The native player surface
intentionally does not show controller IDs, generation counters, or raw
objects.

The presentation model exposes these stable states for executable coverage:

```text
expandedSurfaceActive
expandedBackgroundActive
expandedContentActive
accessTabActive
expandedSurfaceBlocksRaycasts
accessTabBlocksRaycasts
ownedRootBlocksRaycasts=false
```

Collapsed mode must leave only the access tab active and raycastable. A panel
construction or binding failure destroys all owned UI and leaves vanilla Point
Buy untouched.

## Responsive layout facts

Layout diagnostics are emitted only when meaningful layout state changes:

```text
creationKind
profile=Wide|Compact
available parent width/height
effective Canvas scale
calculated panel width/height
body viewport height
measured preferred body height
scroll=true|false
expanded anchored position
access-tab anchored position
```

They do not expose screen titles or raw Unity/controller objects. The profile
comes from parent geometry, not character state. A small geometry/overflow
tolerance prevents per-frame log and layout flicker. Absence of a new line on
an unchanged frame is expected.

## Evidence collection

After a live attempt, fully exit Kingmaker and run:

```powershell
powershell -NoLogo -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\Collect-RuntimeEvidence.ps1
```

Inspect the complete chronological `output_log.txt`, not only the UMM panel's
recent excerpt. Evidence directories are ignored local artifacts. Never commit
logs, screenshots, saves, local paths, or copied game files.

## Respec absence and completion

Bounded diagnostics distinguish missing selector/provider contracts, locked-base
retrains, unowned original/source/preview, unresolved controller binding, inactive
allocator, and attachment failure. The panel host emits at most sixteen distinct
attachment diagnostics, not an error every frame. A hidden preferred racial
anchor still uses the established bottom-center fallback.

Panel attachment/lifecycle failures report the operation phase — `(construction)`
or `(rendering)` — with the complete exception, including its stack trace and
inner exceptions. Deduplication still bounds the log; silent frames do not mean
construction stopped. A deterministic owned-view construction failure is
additionally capped at three attempts per allocator/controller identity, so a
permanently failing rebuild stops reconstructing the panel every frame. Any
allocator or session-controller identity change reopens construction, and
readiness transients that fail before view construction are never counted
against that budget.

At an accepted launch, both available Harmony APIs report owners and priorities
for native respec, state construction, replay, and Commit. This is observation,
not an ordering override. Installed patch attributes alone are not evidence of
active patches in Unity.

A respec final PASS requires the exact callback completion observation, verified
source replay, and the original entity's current descriptor. Diagnostics retain
provider, original/source/recipient identities and expected/observed arrays.
Missing callback, replay overwrite, post-copy mismatch, or exception yields FAIL;
there are no delayed repairs. No such record proves UI clickability or save/reload.
