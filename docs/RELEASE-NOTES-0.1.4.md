# Kingmaker Dice Roller 0.1.4

This release repairs rolled-stat **character-build integrity**: everything
contingent on ability scores now agrees with the array the mod applies. It was
driven by reports that skill-point allowances were wrong for the rolled
Intelligence, advancement could pass with inconsistent allocation, feat
eligibility drifted, and starting spells — especially the final selections —
went missing from the completed spellbook.

Exact Kingmaker 2.1.7b IL inspection confirmed three defects, and all three are
fixed:

- **Stale native allowances.** Kingmaker caches the Intelligence-derived skill
  points and bonus-spell slots on `LevelUpState` and recomputes them from that
  cache. Staging a rolled array previously left the cache computed from the
  pre-roll Intelligence. A shared native refresh now runs for every supported
  context (new main, mercenary, native/Eddic respec) on Roll, Reroll,
  reassignment, History, Recall, and Return to Point Buy, so the scores used to
  calculate allowances and validate selections are the same ones the
  authoritative replay uses.
- **Mercenary completion validated against the wrong scores.** The native
  commit replayed every recorded choice against the pre-roll scores and then
  corrected six numbers afterward — masking dropped spells and mis-validated
  feat prerequisites. The verified assignment is now staged on the exact
  stable owner **before** native checks run, the replay is verified without
  corrective late writes, dropped selections are reported as definitive
  failures, and an interrupted commit restores the exact pre-commit state.
- **Unverified new-main completion.** The final recipient is now audited at
  commit time and the session is closed cleanly.

328/328 C# service cases (12 new build-integrity regressions) and 30/30 Python
cases pass. Exact Kingmaker 2.1.7b contracts — including the new
derived-allowance members — and eleven respec contract groups were verified
against the installed assembly. Release compilation and package validation
pass. The verified native lifecycle is documented in
`docs/CHARACTER-BUILD-INTEGRITY.md`.

**Testing prerelease.** Interactive gameplay, save/reload, and provider-matrix
lanes were not executed by the agent and remain recorded as NOT RUN
(`docs/SMOKE-TEST.md`, section K). `v0.1.3` stays the latest official release
until the owner confirms this workflow in play; promotion will change release
metadata only and reuses these exact package bytes.

newman55 Respecialization is still **not integrated**: its exact DLL remains
unavailable. Bag of Tricks, Call of the Wild, remote companions, and the full
mod profile still need their focused live checks.

Built against the existing UMM 0.33.0 and Harmony12 installation; no loader or
runtime-library downgrade is required.
