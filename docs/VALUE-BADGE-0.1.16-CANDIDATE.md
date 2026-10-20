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
- Clean implementation commit, ZIP/DLL hashes, and qualification results:
  recorded after clean candidate packaging.
- Publication: review candidate only. The v0.1.15 testing alpha remains
  published and unchanged.
- Actual-game visual check: **NOT RUN** until the badge is inspected in
  Kingmaker at 1920×1200 and another UI scale. See `docs/SMOKE-TEST.md`.
