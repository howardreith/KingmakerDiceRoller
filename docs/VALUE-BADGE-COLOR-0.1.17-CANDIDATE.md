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
- Clean implementation commit: `2f480e0f3c55986756d761e5d793aeb04582e771`.
- Installable UMM ZIP: `artifacts/candidates/2f480e0f3c55986756d761e5d793aeb04582e771/KingmakerDiceRoller-0.1.17.zip`.
- ZIP SHA-256: `1ea0856d5440e20390ca734093c051990ef54dbf006616d64c851a87df84f213`.
- DLL SHA-256: `a19236cf8ac6538eb68f10158feb8b8fc7fe3b967f558f47e04eaf19751318ad`.
- Clean-commit `Qualify.ps1 -Build -Package -Candidate`: PASS — release
  gate 20/20, deterministic C# runner 416/416, installed Kingmaker contracts,
  respec checks 11, native UI IL/contracts 30/30, zero-warning Release build,
  and strict six-entry package validation.
- Publication: review candidate only; v0.1.16 remains published unchanged.
- Actual-game visual check: **NOT RUN**. The Windows computer-use process
  exited before window discovery. Use the scenario in `docs/SMOKE-TEST.md`
  after installation.
