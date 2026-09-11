# Character-build integrity

This document records the verified Kingmaker 2.1.7b lifecycle that governs
rolled starting scores, the shared derived-state transaction, authoritative
replay ownership, selection-reconciliation rules, rollback limits, and the
regression gates required for any future stat change. All native behavior below
was verified against the installed assembly (`Assembly-CSharp.dll` SHA-256
`3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`, MVID
`07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`) by IL inspection before the repair was
designed.

## Verified native lifecycle (2.1.7b)

Kingmaker records every player choice as an `ILevelUpAction` on
`LevelUpController.LevelUpActions`. The exact lifecycle is:

```text
UI choice -> LevelUpController.AddAction(action)
  -> action.Check(State, Preview)          (rejected in the UI if it fails)
  -> LevelUpActions.Add(action)
  -> action.Apply(State, Preview)
  -> State.OnApplyAction()

preview rebuild -> UpdatePreview()
  -> dispose old Preview; Preview = RequestPreview()   (clone of the SOURCE)
  -> ApplyLevelup(Preview):
       new LevelUpState(preview, mode)   [fresh StatsDistribution, all 10s]
       foreach action: Check -> fail: silently dropped; pass: Apply + OnApplyAction
       LevelUpActions = surviving actions only

authoritative completion -> Commit()
  -> ApplyLevelup(LevelUpController.Unit)   (the STABLE SOURCE, same replay)
  -> SetupNewCharacher() -> companion insertion -> m_OnSuccess()
```

Key IL-verified facts:

- `LevelUpState.OnApplyAction()` recomputes `TotalSkillPoints` from the cached
  `IntelligenceSkillPoints` field (minimum 1, plus `ExtraSkillPoints`) and calls
  `SpellSelectionData.UpdateMaxLevelSpells(unit)` per spellbook, which resizes
  the bonus-spell slot array from the live casting attribute.
- `LevelUpState.IntelligenceSkillPoints` is written in exactly one place: the
  `ApplySkillPoints` action's `Apply`, which reads the unit's live Intelligence
  through `LevelUpHelper.GetTotalIntelligenceSkillPoints` and records the
  granted total on `unit.Progression.TotalIntelligenceSkillPoints`. The action
  is created once by `LevelUpController.SelectClass`.
- `LevelUpState` is constructed in exactly two places: the `LevelUpController`
  constructor (on the initial preview) and `ApplyLevelup`. `ApplyLevelup` is
  called from exactly two places: `UpdatePreview` (always a fresh preview clone)
  and `Commit` (always the stable source). Therefore a `LevelUpState` whose unit
  is the stable controller source is uniquely the authoritative Commit replay
  state.
- Actions failing `Check` during any replay are silently dropped from
  `LevelUpActions` (`UberDebug.Log("Invalid action: ...")`). Stale point-buy
  `AddStatPoint`/`RemoveStatPoint` actions fail `Check` once the allocator is
  unavailable (`Available == false`) and are dropped natively.
- The skills page displays `State.SkillPointsRemaining` =
  `TotalSkillPoints - SpentSkillPoints`; both terms are caches, not live reads.

## Confirmed defects (first divergences)

1. **Stale derived allowances (all creation kinds).** Staging a rolled array
   wrote the distribution and preview unit but left `IntelligenceSkillPoints`
   cached from the class-selection-time Intelligence. Until an unrelated native
   rebuild, the skill-point allowance and bonus-spell slots shown to the player
   were computed from the wrong Intelligence. Selections made under those
   allowances disagreed with the authoritative replay, which recomputes them
   from the live scores.
2. **Mercenary authoritative replay on pre-roll scores.** The mercenary Commit
   replayed every action against the stable source's original scores; checks
   (`SelectSpell.CanSpendSlot`, `SelectFeature` prerequisites, skill-rank caps)
   validated choices against different values than the player saw. Six base
   values were then corrected after replay — masking the dropped selections
   (last spell picks in bonus slots were the reported symptom).
3. **Unverified new-main completion.** The commit-time constructor state was
   rebound and staged (scores did reach the replay), but nothing verified the
   final recipient, and the stale session leaked into the update loop, where it
   could log misleading application failures after a successful commit.

## Shared derived-state transaction

`DerivedStateRefreshService` is the single transaction that makes the staged
scores drive the native derived state for **every** supported starting-score
context (new main, mercenary, native/Eddic respec). It runs after staging on:

- explicit Roll, Reroll, reassignment (`TryMoveAssignment`),
- Use History and Recall,
- the bounded update-loop restage, and
- Return to Point Buy (restoring allowances that match the point-buy scores).

The transaction reproduces exactly one full native replay of the
`ApplySkillPoints` bookkeeping against the live unit, then invokes the native
`LevelUpState.OnApplyAction()`:

```text
baseline = progression.TotalIntelligenceSkillPoints - state.IntelligenceSkillPoints
total    = LevelUpHelper.GetTotalIntelligenceSkillPoints(unit, state.NextLevel)
state.IntelligenceSkillPoints = total - baseline
progression.TotalIntelligenceSkillPoints = total
state.OnApplyAction()
```

This is idempotent (a repeated refresh recovers the same baseline), does not
rebuild the preview, does not touch the action list, and consumes no random
values. The pre-refresh values are captured so a failed or rolled-back command
also rolls back this semantic effect. A failed refresh fails the command
closed; it is never swallowed into a successful Roll or Return to Point Buy.

The refresh deliberately does **not** force a preview rebuild at roll time: a
rebuild replays the recorded actions, which would drop the player's point-buy
`AddStatPoint`/`RemoveStatPoint` actions (their `Check` fails while the rolled
allocator is disabled) and break later point-buy reconstruction.

## Authoritative replay ownership and order

**Mercenary (pre-replay commit ticket).** When a verified Roll-mode mercenary
session observes a `LevelUpState` constructed on its exact stable owner
(`LevelUpController.Unit`, mode `CharGen`, first level, custom-companion
evidence, active controller match), that state is uniquely the Commit replay
state. The one-use ticket:

1. captures the pre-commit distribution/source snapshot and the recorded action
   inventory,
2. stages the verified assignment and disables the allocator **before** native
   `Check/Apply` consume ability values,
3. at the `ApplyLevelup` postfix, verifies the replay retained the assignment
   (a mismatch is a failure; **no corrective late write**), and reports how many
   recorded actions failed native checks,
4. at the `Commit` postfix, verifies the final recipient and closes the session
   with one PASS/FAIL record.

Cancellation, owner loss, disable, duplicate commit, or an exception between
construction and completion expires the ticket and restores the exact captured
pre-commit state; no late write is permitted.

**New main character.** The commit-time constructor state is accepted through
the same-owner rebind path, which stages the verified assignment on the stable
owner before native action replay. The `Commit` postfix then audits the final
recipient's six base values (diagnostic PASS/FAIL, no writes) and closes the
session so no stale generation reaches the update loop.

**Respec.** Unchanged: the first-level commit ticket stages inside the exact
Commit invocation before action checks, and the native copy callback must
complete before the original entity's current descriptor is reread and
verified.

Starting-score authority closes at completion for every kind: later ordinary
level-ups (including native `SpendAttributePoint` at levels 4 and 8) construct
non-first-level states that never receive staging.

## Skills-page presentation and forward navigation

The Skills page caches its presentation. Exact IL establishes:

- The red remaining-points badge is repainted only by
  `CharBSkillsAllocator.FillLevelUpData()`, reached through
  `CharBPhaseSkills.FillData` when the phase is selected, available, unlocked,
  and **dirty** (`CharBPhase.UpdateData` clears the dirty flag as it fills).
- The per-phase `IsUnlocked` cache — what `ToNextPhase` and `SetPhase` actually
  enforce — is recomputed only by `DefinePhases`, which runs only inside
  `SetupUI`. Every forward route (Next button, keyboard/gamepad submit, the
  phase menu, `OnShow`) funnels through `SetPhase(CharBPhase.Type)`.
- `LevelUpState.IsSkillPointsComplete()` is the live completion predicate:
  overspending fails, an exact spend passes, and unspent points pass only when
  every skill rank is already at the level cap (a genuine native exception —
  preserved, not replaced by a blanket remaining==0 rule).

`SkillsPhaseSynchronizationService` therefore replays the exact native
skill-click refresh (`DefineAvailibleData` + `Skills.IsDirty = true` +
`SetupUI`) after every relevant change: Roll, Reroll, reassignment, History,
Recall, Return to Point Buy, verified preview replacement, bounded restage,
failed-command rollback, and drawer close (the Close callback routes through
the command layer, never reflects directly). Synchronization is keyed on the
session's assignment revision — rerolls share a preview generation, so the
generation alone is not a dirty signal — and is bounded to one native refresh
per revision and idempotent, so repeated open/close without changes is a
no-op. It never rerolls, allocates or refunds points, rebuilds the preview, or
clears selections; `DefineAvailibleData` rebuilds the feature-selection UI
from the live model exactly as a native click does.

Because a stale enabled button must not authorize leaving an invalid phase,
`CharacterBuildController.SetPhase` carries a veto-only prefix (the single
forward funnel). While an owned session is active, any forward target beyond
Skills (`target > Skills`, `current <= Skills`) is blocked when the live
`IsSkillPointsComplete()` is false; the handler synchronizes the page, shows
the native `BlinkMarks`, and reports the exact unspent/excess amount. Back and
same-phase navigation always pass; the guard can only veto, never permit; with
no owned session it is inert; and disable/cancel/owner loss remove it with the
session. Zero displayed points alone never forces success — native
requirements still govern every other phase.

## Selection reconciliation rules

- The shared refresh makes the live allowances agree with the staged scores at
  selection time, so native UI gating validates every choice under the final
  scores.
- Invalidating a prior choice by lowering a score is handled by the native
  engine: the next replay re-`Check`s every recorded action and drops invalid
  ones with its own `Invalid action` log. Dice Roller never forces a check to
  succeed, never re-grants dropped selections, and never manually grants
  skills/features/spells after replay.
- At the authoritative mercenary replay, dropped actions are counted against
  the pre-replay inventory and produce a definitive FAIL record (for example,
  "2 of 3 recorded level-up actions failed native replay checks under the
  rolled scores"). Because native has already applied the surviving actions
  when this is observed, the failure is reported accurately rather than
  "fixed": no unproven blanket rollback of a real committed character is
  attempted.
- Ability-distribution completion stays narrowly scoped
  (`StatsDistribution.IsComplete` override only). Skills, features, spells, and
  phase progression are never force-completed.

## Rollback limits

- Command rollback (failed Roll/Reroll/Reassign/History/Recall) restores the
  exact generation snapshot (distribution values, unit base values, allocator
  state) **and** the derived-allowance caches.
- A failed derived refresh after a successful point-buy restoration keeps the
  durable restoration but reports the command as failed.
- An interrupted mercenary commit restores the exact pre-commit source state
  (the source was not yet inserted through companion ownership).
- After native `SetupNewCharacher`/serialization, no rollback is attempted;
  mismatches are reported as post-commit failures with full evidence.

## Regression gates for future stat changes

Any change that touches starting scores must keep these deterministic gates
green (runner: `scripts/Test-Domain.ps1`):

- `RolledIntelligenceRefreshesNativeSkillAllowance` and
  `DerivedAllowanceRefreshIsIdempotentAcrossRerolls` — allowances follow the
  staged scores through the native formula.
- `FailedDerivedRefreshFailsRollAndRollsBack` and
  `DerivedRefreshFailureOnPointBuyReturnIsReported` — a broken refresh fails
  closed with full semantic rollback.
- `MercenaryCommitStagesScoresBeforeNativeReplay`,
  `MercenaryReplayOverwriteFailsWithoutCorrectiveWrite`,
  `MercenaryDroppedSelectionsFailCompletion`,
  `MercenaryInterruptedCommitRestoresExactPreCommitState`, and
  `MercenaryCatchUpLevelNeverStagesStartingScores` — pre-replay ownership,
  no corrective writes, dropped-selection evidence, exact interrupted-state
  restore, and closed catch-up authority.
- `NewMainCommitStagesAndVerifiesAuthoritativeScores` — the new-main seam
  stages before replay and verifies the final recipient.
- `UpdateRestageAlsoRefreshesDerivedAllowances` — the bounded restage path
  cannot reintroduce stale caches.
- `RespecDerivedStatsRefreshOnRollAndReturn` — the respec path shares the same
  transaction (no optional-adapter dependency).
- `SpentBudgetRerollRefreshesBadgeAndBlocksNext`,
  `OverspendAfterLowerIntelligenceBlocksForward`,
  `RepeatedDrawerCloseWithoutChangesIsHarmless`,
  `ForwardGuardIsScopedToOwnedSessionAndSkillsPhase`,
  `RerollWithIdenticalScoresStillSynchronizesExactlyOnce`,
  `DrawerCloseSettlesSynchronizationAfterFailedCommand`,
  `PreviewReplacementSynchronizesSkillsPage`, and
  `PointBuyReturnSynchronizesSkillsPageForPointBuyBudget` — the skills badge,
  navigation veto, scoping, boundedness, and drawer-close guarantees. A refresh
  counter alone is never the assertion: the displayed badge value and the
  allow/block decisions are.

Installed-assembly gates: `scripts/Verify-KingmakerContracts.ps1` proves the
derived-allowance members (including that `OnApplyAction` reads the cached
`IntelligenceSkillPoints`) and `scripts/Verify-RespecContracts.ps1` proves the
respec adapter surface against the built DLL.
