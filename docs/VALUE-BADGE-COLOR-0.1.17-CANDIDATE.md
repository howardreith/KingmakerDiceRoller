# 0.1.17 Value badge color candidate

## Cause and correction

Read-only IL inspection of installed Kingmaker 2.1.7b shows the ability
allocator's `FillData()` calls `SetupVisualMarks()`. That method assigns
`m_TextDisableColor` to the native Points numeral when the allocator is
complete. During Roll Mode, a page round trip rebuilds the mod's owned
Value text from that gray native source. The mod now reads the exact
`m_TextAvailibalePointsColor` and `m_LabelAvailibalePointsColor` fields
through cached contracts and applies them to its owned numeral and caption.
It does not change the native Points text or allocator behavior.

## Artifact and qualification

- Branch: `codex/native-stat-ui-v2`, continuing draft PR #7.
- Clean implementation commit, ZIP/DLL hashes, and qualification results:
  recorded after clean candidate packaging.
- Publication: review candidate only; v0.1.16 remains published unchanged.
- Actual-game visual check: **NOT RUN**. Use the scenario in
  `docs/SMOKE-TEST.md` after installation.
