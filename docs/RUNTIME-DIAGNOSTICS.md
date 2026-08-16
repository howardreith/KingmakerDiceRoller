# Runtime diagnostics

## UMM panel

The panel reports current status, accepted/rejected context counts, application
count, contract MVID, detected compatibility-sensitive mods, and the last twelve
session events. It also exposes the recovery action to return an active session
to point buy.

## Log phrases

Useful filters:

```text
Kingmaker Dice Roller
Contract:
Character-creation context rejected:
Fixed diagnostic array is active
Restore point-buy allocator
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
