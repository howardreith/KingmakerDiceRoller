# Mr. Bard's Kingmaker Roll For Stats UI 0.2.1

Stable patch release, published with the owner's authorization before any
in-game test. Gameplay is unchanged from 0.2.0.

## What changed

- The declared Unity Mod Manager minimum is now **0.31.1** (0.2.0 declared
  0.32.0 and its DLL referenced UnityModManager 0.33.0.0).
- The DLL is compiled against hash-pinned **UnityModManager 0.31.1.0** and
  **0Harmony12 1.2.0.1** references, independent of the loader installed on the
  build machine. The build and package checks refuse a DLL compiled against any
  other UnityModManager.
- The UMM ID, DLL name, entry method, install folder and settings format are
  unchanged, so 0.2.1 installs over earlier versions.

## Loader compatibility evidence

| Item | Status |
|---|---|
| Declared minimum (`Info.json` `ManagerVersion`) | UMM 0.31.1 |
| Pinned compile baseline | UnityModManager 0.31.1.0, 0Harmony12 1.2.0.1 (hash-pinned) |
| Static loader API audit | Every referenced loader member resolved on UMM 0.31.1, 0.32.4, 0.33.0 and 0.33.4 (static only) |
| Out-of-game managed checks | None performed for this mod |
| Native gameplay and combined mod-stack testing | **NOT RUN** on any loader version |

The static audit shows that the referenced names and signatures exist in those
loader builds. It is not native compatibility testing, and it makes no claim
about future UMM versions.
