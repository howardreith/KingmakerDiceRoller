# 0.1.16 Value badge correction candidate

## Change

The owned native Value badge now displays `RollUiSnapshot.PointBuyEquivalent`
for an applied rolled assignment. The snapshot already calculates that value
from the active array; it is recomputed after Reroll, History, Recall, and
assignment updates. Roll-ready still shows a dash, and Return to Point Buy
restores the game's native Points caption and remaining budget. Details keeps
the arithmetic base-score total and the point-buy equivalent separately.

## Artifact and qualification

- Branch: `codex/native-stat-ui-v2`, continuing draft PR #7.
- Clean implementation commit: `8e9d60d00a232e327c4207317ce4cdee8b7ee49a`.
- Installable UMM ZIP: `artifacts/candidates/8e9d60d00a232e327c4207317ce4cdee8b7ee49a/KingmakerDiceRoller-0.1.16.zip`.
- ZIP SHA-256: `174e21f2c2ef0f65f0dbe66a8619dc3873f37daf49b504adce94e0ec57eb03ca`.
- DLL SHA-256: `bb06be08406ca755f6deba558c96aafb14a7923f0ae2b7901fc6e05926817046`.
- Clean-commit `Qualify.ps1 -Build -Package -Candidate`: PASS — source/release
  gate 20/20, deterministic C# runner 416/416, installed Kingmaker contracts,
  respec checks 11, native UI IL/contracts 30/30, zero-warning Release build,
  and strict six-entry package validation.
- Publication: review candidate only. The v0.1.15 testing alpha remains
  published and unchanged.
- Actual-game visual check: **NOT RUN**. The Windows computer-use process
  exited before window discovery. Inspect the badge in Kingmaker at
  1920×1200 and another UI scale; see `docs/SMOKE-TEST.md`.
