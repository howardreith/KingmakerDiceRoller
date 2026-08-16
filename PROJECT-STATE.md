# Project state

## Current phase

Phase 2 — fixed-array Kingmaker vertical slice, source implementation.

## Branch and commit

Branch: `pro/kingmaker-dice-roller-mvp`

The exact commit is written by `scripts/Build-Local.ps1` into build provenance.

## Implemented behavior

- Pure roll engine and presets.
- Immutable six-score arrays and duplicate-safe swaps.
- Explicit low-score policies and extended point-buy equivalent.
- Single-active-session ownership model.
- Fixed diagnostic array `16, 15, 14, 12, 10, 8`.
- Fail-closed context policy for new main-character creation only.
- Captured point-buy budget and explicit restoration path.
- Three narrow Harmony postfix surfaces: state construction, distribution
  start, and completion.
- UMM diagnostics and point-buy restoration control.

## Qualification

| Level | Status |
|---|---|
| Implemented | Yes — source candidate |
| Source-qualified | Pending final repository validator in this workspace |
| Build-qualified | No — requires Windows/.NET Framework/Kingmaker assemblies |
| Runtime-qualified | No |
| Compatibility-qualified | No |

## Known blockers

- Target GitHub repository does not yet exist and this environment has no
  authenticated GitHub CLI/write transport.
- This container has no .NET compiler, MSBuild, PowerShell, or Kingmaker
  assemblies.
- Exact game-member resolution must pass on Howie's Kingmaker 2.1.7b install.
- Native ability-score UI is intentionally deferred until the fixed-array gate.

## Required next live test

Build and package on Howie's Windows Kingmaker machine, install only the alpha
candidate, then execute `docs/SMOKE-TEST.md` beginning with vanilla new-character
creation and point-buy restoration.

## Important decisions

- Rewrote rather than ported the upstream static lifecycle.
- Uses position-based assignment so duplicate scores are never ambiguous.
- Does not replace vanilla plus/minus semantics.
- Does not hard-code a point-buy budget.
- Does not claim Bag of Tricks compatibility before live qualification.
