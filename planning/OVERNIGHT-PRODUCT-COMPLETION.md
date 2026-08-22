# Overnight product completion

## Authoritative baseline

- Branch: `pro/kingmaker-dice-roller-mvp`
- Starting commit: `8f78d8243ed27ed2cbb3fadafd890aba172975aa`
- Starting divergence: `0 0`
- Starting worktree: clean
- Target version: `0.1.0-alpha.1`
- Game contract: Pathfinder: Kingmaker 2.1.7b, Assembly-CSharp MVID
  `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`
- Assembly-CSharp SHA-256:
  `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`

## Product invariants

- A valid owned new-main-character session begins in ordinary PointBuy mode.
- Dice are consumed only by an explicit Roll or Reroll command.
- Preview, phase, race, class, portrait, and UI reconstruction never generate
  an array.
- Roll mode owns one immutable raw array plus a position-based permutation and
  disables the native allocator without patching its add/remove/cost methods.
- The point-buy origin is captured immediately before each PointBuy-to-Roll
  transition and is never replaced by a rolled preview generation.
- Every live mutation is transactional and verified against the current
  controller state, preview, distribution, unit values, allocator state, and
  native presentation binding.
- Return to Point Buy restores the exact captured allocation and observed
  budget on the newest preview and refreshes the open page immediately.
- Racial modifiers remain Kingmaker-owned and are never stored in raw arrays,
  history, or saved slots.
- Ordinary progression, companions, pets, enemies, mercenaries, pregens,
  respec, unsupported modes, and unresolved ownership fail closed.
- No game-save-owned content or third-party-mod mutation is introduced.

## Selected native UI seam

The exact 2.1.7b desktop path is:

```text
Game.Instance.UI.CharacterBuildController
  CurrentPhase == CharBPhase.Type.Skills
  Skills : CharBPhaseSkills
    AbilityScoresAllocator : CharBAbilityScoresAllocator
```

`CharBAbilityScoresAllocator.FillData()` is the proven native refresh seam.
The panel host will attach to the allocator's exact GameObject/Transform and
will use code-owned Unity objects styled from local native controls. Lifecycle
observation will use a narrow delegated patch where exact IL supports it, with
a bounded UMM-update attachment observer as the documented fallback. It will
never use direct screen-coordinate drawing as the primary interface.

## Checkpoints

1. **Complete — product architecture:** PointBuy-first session lifecycle,
   command boundary, workflow snapshot, exact transition-time point-buy origin,
   and no-automatic-roll regressions. The focused source/build gate passes with
   155 compiled behavior cases and 25 Python oracle cases.
2. **In progress — non-UI workflow:** configuration, Roll/Reroll, point-buy
   origin, assignment, history, saved catalog, totals, transactional
   application.
3. **Pending — native panel:** exact contracts, attach/detach, commands,
   allocator-control ownership, refresh/rebind, fixture coverage.
4. **Pending — hardening/version/docs:** settings migration, compatibility,
   diagnostics, user guide, 0.1.0-alpha.1 metadata.
5. **Pending — final gates:** clean qualification, package, transactional
   install, Git synchronization, morning handoff.

## Commands and gates

At every checkpoint: focused tests, repository validation, `git diff --check`,
explicit-path staging, commit, normal push, fetch, and `0 0` divergence.
The final gate is:

```powershell
powershell -NoLogo -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\Qualify.ps1 -Build -Package
```

followed by installer WhatIf, transactional install, hash/allowlist checks,
other-mod fingerprints, and a clean-commit qualification rerun.

## Evidence and risks

- Latest live evidence: ignored directory `artifacts/runtime-evidence/20260821-231158`.
  Commit `8f78d824...` visibly passed immediate fixed-array-to-point-buy page
  synchronization; the log independently verified native state, distribution,
  source, and preview bindings.
- Highest implementation risk: exact native panel layout and cleanup on Unity
  2018 prefab instances. Mitigation: inspect exact IL and serialized field
  contracts, create only code-owned child objects, attach once by marker, and
  restore every captured native control state.
- Human visual/layout testing and the named compatibility matrix remain morning
  gates; source/build success will not be reported as runtime qualification.

## Resume marker

- Current commit: pending Phase A checkpoint based on `8f78d8243ed27ed2cbb3fadafd890aba172975aa`.
- Next unfinished phase: complete and harden the non-UI command workflow.
