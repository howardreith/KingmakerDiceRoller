# Changelog

## 0.0.1-alpha.1 — source candidate

- Initialized a standalone Kingmaker 2.1.7b UMM project.
- Added a pure deterministic dice-expression and roll-preset domain.
- Added immutable six-score arrays and duplicate-safe assignment swaps.
- Added explicit tabletop and Kingmaker-safe low-score policies.
- Added extended point-buy-equivalent reporting.
- Added a guarded fixed-array character-creation integration candidate.
- Added source, contract, build, package, install, and smoke-test tooling.
- Added grace-period release of stale character-creation sessions through the
  exact `LevelUpController.State` contract and UMM update callback.
- Added exact duplicate-safe package validation and transactional install
  rollback.
- Source-qualified 54 C# files, 48 C# behavior cases, and 25 executed Python
  oracle cases in the handoff environment.

No runtime or compatibility qualification is claimed for this source candidate.
