# Z mission state — Skills-counter refresh and forward-navigation integrity

Resumable mission record. Baseline: clean `main` at `f9f7cf31` (v0.1.4 testing
prerelease, release commit `748350bf`), mission branch `z/skills-counter-next-guard`.
This mission is **local only**: no push, merge, tag, publication, or promotion
is authorized for its candidate.

## Confirmed causes (IL-verified against the installed assembly)

1. **Stale badge.** The red remaining-points badge is repainted only by
   `CharBSkillsAllocator.FillLevelUpData()`, reached through the dirty Skills
   phase (`CharBPhase.UpdateData` fills selected+available+unlocked+dirty
   phases). A rolled-assignment change never marked the phase dirty, so the
   badge kept its previous number until an unrelated skill interaction.
2. **Stale forward gate.** Every forward route funnels through
   `CharacterBuildController.SetPhase(CharBPhase.Type)` (Next button,
   keyboard/gamepad submit through the same button, phase-menu chips, and
   `OnShow`→`ToNextPhase`), which enforces the per-phase `IsUnlocked` **field**.
   That cache is recomputed only by `DefinePhases`, which runs only inside
   `SetupUI` — invoked by native interactions, not by the mod's model refresh.
   The 0.1.4 derived-allowance repair made the live model correct
   (`LevelUpState.IsSkillPointsComplete()` is a live read of
   spent/total/capped ranks) but nothing recomputed the UI caches, so Next
   still allowed leaving Skills with an invalid allocation, and the following
   features phase filled from an unsettled selection state (the reported empty
   feats/traits page).

Not yet reproduced statically: the exact empty-page rendering inside the
features phase; it is downstream of the same invalid transition (live lanes).

## Implemented changes (branch `z/skills-counter-next-guard`)

- `SkillsPhaseSynchronizationService` (new): replays the exact native
  skill-click refresh — `DefineAvailibleData()` + `Skills.IsDirty = true` +
  `SetupUI()` — after every relevant change and on drawer close. Bounded to one
  native refresh per assignment revision (new `RollSession.AssignmentRevision`,
  bumped on roll-mode entry, replacement, rollback, restore, and rebind),
  idempotent, reentrancy-guarded; never simulates clicks or touches the action
  list, preview identity, RNG, or selections.
- Coordinator: synchronization call sites in the command success path, Return
  to Point Buy, verified-generation completion, the bounded restage, a per-frame
  pending check in `Update`, and `OnRollDrawerClosed()` (routed through
  `IRollUiCommandTarget`/`RollUiCommandRouter` from the drawer's Close
  callback).
- Veto-only guard: prefix on `CharacterBuildController.SetPhase` (the seventh
  patch; the six postfixes are unchanged). Blocks forward targets beyond Skills
  (`target > Skills`, `current <= Skills`) while an owned session is active and
  the live `IsSkillPointsComplete()` is false; synchronizes the page, shows
  native `BlinkMarks`, reports exact unspent/excess counts. Back and
  same-phase navigation always pass; no session means no veto; veto-only by
  construction.
- Contracts/resolver/verify script: `IsSkillPointsComplete`,
  `SkillPointsRemaining`, `DefineAvailibleData`, `SetupUI`, `SetPhase(int)`,
  `CharBPhase.IsDirty`, `CharBPhaseSkills.BlinkMarks`, Skills==5, plus IL
  ordering proofs (SetPhase enforces `IsUnlocked`; `SetupUI` calls
  `DefinePhases`; `IsSkillPointsComplete` reads spent/total fields).
- Repository validator: requires the new bridge token and exactly one
  `PatchPrefix` install.

## Test and evidence status

- Deterministic runner: **336/336** (328 prior + 8 new skills-navigation
  regressions: owner reproduction both directions, badge/displayed-state
  assertions, forward veto + scope + Back availability, close idempotence,
  failed-command settling, preview replacement, point-buy return).
- Repository validation 13/13; Python oracle 30/30; exact native contracts
  (including the new skills-page checks) PASS against the installed assembly;
  respec installed-assembly checks 11 PASS; Release build zero warnings/errors.

### Live lanes — NOT RUN

No game launch, installation, save operation, or desktop control was
authorized. Section L of `docs/SMOKE-TEST.md` records the exact remaining
procedure (owner reproduction in both directions, all forward routes,
provider/full-profile cases, final-character and save/reload persistence).
This mission is **implemented/source-qualified; live acceptance pending**.

## Distinct local candidate

Version bumped to `0.1.5` (the published `v0.1.4` assets are untouched).
Final candidate rebuilt from clean commit `cc519c03` (dirty=false, branch
`z/skills-counter-next-guard`): DLL SHA-256
`591ef6eeaa1d8074ab32f644657f0b6246120b55d637e51b1d26b5353cf042e7`; package
`artifacts/packages/KingmakerDiceRoller-0.1.5.zip` SHA-256
`1a3291231ac80e4ad4a83c7cfdc01adbf02bdecb606f1697a3f5cdee83ec493c`
(deterministic six-file allowlist, package-validated). No push, merge, tag,
or release was performed for this mission.

## Next steps

1. Owner-authorized live run of smoke-test sections K and L on a disposable
   fixture using the 0.1.5 candidate.
2. On acceptance: publish `v0.1.5` per the repository release procedure
   (testing prerelease first unless runtime lanes were executed; official
   promotion always requires owner confirmation).
