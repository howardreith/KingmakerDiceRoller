# Runtime diagnostics

## UMM panel

The panel reports current status, accepted/rejected context counts, application
count, contract MVID, detected compatibility-sensitive mods, and the last twelve
session events. It also exposes the recovery action to return an active session
to point buy.

Context decisions report stable facts for mode, first-level state, candidate
flags, controller state/unit/preview identity, main-character presence, and
whether the main descriptor matches the candidate or controller source. Raw
object contents are not logged, and repeated rejection details remain
deduplicated while the total rejection count continues increasing.

Preview-continuity events distinguish session opening, same-owner rebinding,
constructor-stage/deferred replacement, verified live application, and true
stable-owner release. Live verification reports only stable Boolean facts:
application generation, refresh-in-progress, pending replacement, same stable
owner, rebound preview, controller state/preview identity, distribution
identity/value match, and live unit-value match. `APPLY` is counted only after
the controller's current generation passes all checks, including allocator
suppression; matching detached objects are not sufficient.

Point-buy transitions report the immutable pristine capture generation, current
preview generation, whether a replacement snapshot already contained the roll,
session mode, observed allocator budget, live pristine distribution/unit/
allocator matches, and stable-owner roll suppression. A successful transition
uses `RESTORE` and states that pristine point buy was verified on the live
preview. Rolled values plus a full available budget are a hard verification
failure and are never reported as restored.

Presentation transitions separately report whether semantic point buy was
already verified, whether the exact native refresh was requested, the refresh
method/count, whether the Skills ability phase was active, whether its state,
distribution, source, and preview bindings match the session, the pre/post
generation, and post-refresh live-model verification. A presentation failure
does not undo safe PointBuy mode or revive the rolled completion override.

## Log phrases

Useful filters:

```text
Kingmaker Dice Roller
Contract:
Character-creation context rejected:
Fixed diagnostic array application verified against the live controller preview
same-owner preview generation
stable controller/source owner
Restore point-buy allocator
Verified pristine point-buy state on the live preview
pristineBaselineCaptured
candidateBaselineContaminated
rollSuppressedForStableOwner
semanticPointBuyVerified
presentationRefreshRequested
presentationRefreshMethod
presentationRefreshCount
activeAbilityPhaseFound
abilityPhaseStateMatchesSession
abilityPhaseDistributionMatchesSession
abilityPhaseViewModelMatchesSession
postRefreshLiveModelVerified
Enable failed closed
remains enabled to preserve recovery hooks
```

## Contract evidence

`scripts/Verify-KingmakerContracts.ps1` writes:

```text
artifacts/contracts/runtime-contracts.json
```

It records the Assembly-CSharp path, SHA-256, identity, MVID, exact signatures,
ability order, and resolved identity paths. A source archive must not contain
that local report.

## Build evidence

`artifacts/build-provenance.json` records branch, commit, dirty status, DLL hash,
and game-assembly identity. `artifacts/packages/package-manifest.json` records
package contents and hashes.

## Runtime evidence

`scripts/Collect-RuntimeEvidence.ps1` copies available player/UMM logs into an
ignored timestamped directory. Add screenshots and written observations there.
Do not commit saves, logs, screenshots, local paths, or generated reports.
For Kingmaker 2.1.7b it also checks the live LocalLow `output_log.txt` path.
