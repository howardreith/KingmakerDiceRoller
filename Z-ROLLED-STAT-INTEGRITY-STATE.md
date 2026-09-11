# Z mission state — Repair rolled-stat character-build integrity

Resumable mission record. Baseline: clean `main` at `f4eba6d` (released 0.1.3),
mission branch `z/rolled-stat-build-integrity`.

## Baseline evidence

- Game install: `Assembly-CSharp.dll` SHA-256
  `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`, matching
  the pinned 2.1.7b contract in `docs/INTEGRATION-SEAMS.md`.
- Baseline qualification before any change: repository validation 13/13,
  Python oracle 30/30, deterministic C# runner 316/316, exact native contracts,
  respec contract checks 11, Release build zero warnings/errors. Baseline DLL
  bytes matched released 0.1.3 (`7f1265ae…`).
- Local IL inspection tooling (ignored, under `artifacts/il/`) disassembled the
  installed `LevelUpState`, `LevelUpController`, `StatsDistribution`,
  `ApplySkillPoints`, `SpendSkillPoint`, `SelectSpell`, `SelectFeature`,
  `SpellSelectionData`, `LevelUpHelper`, and UI phase classes. No game binary
  was committed.

## Proven versus suspected causes

Confirmed by IL evidence (see `docs/CHARACTER-BUILD-INTEGRITY.md`):

1. `LevelUpState.IntelligenceSkillPoints` is written only by the
   `ApplySkillPoints` action (created once at class selection);
   `OnApplyAction()` recomputes `TotalSkillPoints` and per-spellbook
   `UpdateMaxLevelSpells` from that cache. Staging scores without redoing the
   bookkeeping leaves UI allowances computed from the pre-roll Intelligence —
   the reported too-high/too-low skill points and the selection disagreement.
2. `LevelUpController.ApplyLevelup` silently drops every action whose `Check`
   fails during replay, and the mercenary `Commit` replay previously ran
   against the pre-roll source scores before a late six-value correction —
   the reported lost starting spells (bonus-slot capacity shrinks under
   `UpdateMaxLevelSpells` when the casting stat is read stale) and affected
   feat prerequisites.
3. New-main completion already staged through the commit-time rebind, but had
   no final-recipient verification and leaked a stale session.

Remaining hypotheses (not confirmed without live runs):

- The exact "blank traits page" trigger. Native phase gating
  (`CharBPhaseSkills.CheckAvailible`, `DefineAvailibleData`) depends on the
  same stale caches, so the refresh is expected to resolve it, but the precise
  provider interaction was not reproduced statically. Live verification
  remains open.
- Vanilla itself keeps `IntelligenceSkillPoints` stale between class selection
  and the first rebuild (transient quirk); Dice Roller now refreshes eagerly,
  which is stricter than vanilla but consistent with what every replay
  computes.

## Implemented changes (branch `z/rolled-stat-build-integrity`)

- `Integration/KingmakerContracts.cs` + `KingmakerContractResolver.cs`: seven
  shared derived-allowance/replay-inventory contracts (NextLevel,
  IntelligenceSkillPoints, OnApplyAction, GetTotalIntelligenceSkillPoints,
  Progression, TotalIntelligenceSkillPoints, LevelUpActions), fail-closed at
  enable time; `scripts/Verify-KingmakerContracts.ps1` proves the same shapes
  against the installed assembly (including that `OnApplyAction` reads the
  cached allowance).
- `CharacterCreation/DerivedStateRefreshService.cs` (new): shared transaction —
  replays the native `ApplySkillPoints` bookkeeping against the live unit,
  invokes `OnApplyAction`, verifies readback, and exposes snapshot rollback.
- `CharacterCreationCoordinator.cs`: refresh runs for all kinds on Roll,
  Reroll, reassignment, History, Recall, bounded restage, and Return to Point
  Buy; failed refreshes fail the command with full semantic rollback;
  mercenary pre-replay ticket wiring; new-main commit audit + session close;
  mercenary ticket expiry in the update loop.
- `CharacterCreation/MercenaryFinalizationService.cs`: late-stat correction
  replaced by a one-use pre-replay commit ticket (exact-owner, first-level,
  CharGen, custom-companion evidence) with dropped-action evidence, verify-only
  post-replay/post-commit seams, and exact pre-commit restore on interruption.
- `Patches/KingmakerPatchBridge.cs`: the `ApplyLevelup` postfix captures
  `__result` (surviving actions).
- `RespecOwnership`/`NativeRespecContracts`/`NativeRespecEntryService`: the
  respec-only derived-refresh indirection removed; respec uses the shared
  service.
- `tools/validate_repository.py`: qualification gates updated to the new
  lifecycle (pre-replay guards, shared refresh presence, derived-allowance
  contracts).

## Tests and evidence status

- Deterministic runner: **328/328** (316 baseline + 12 new build-integrity
  regressions; respec derived-stats test reworked to the shared service).
- Repository validation 13/13; Python oracle 30/30; exact native contracts
  (incl. new derived-allowance checks) PASS against the installed assembly;
  respec installed-assembly checks 11 PASS; Release build zero warnings/errors.
- Candidate DLL SHA-256 `e96d3389075c5bcafa277fc41598727bca2ee4cc98473a8cb8b55af06dffdc0a`
  (see `artifacts/build-provenance.json`).

### Live lanes — NOT RUN

No game launch, installation, save operation, or desktop control was
authorized in this session. Per the guarded procedure in
`docs/SMOKE-TEST.md`, the following require an owner-nominated disposable
fixture: interactive new-main and mercenary creation (skill allowance
display, feat/trait pages, starting spells), INT increase/decrease after
allocation, trait-provider scenario, race/class/archetype Back/Next,
Bag of Tricks/CotW profiles, native/Eddic respec, save/reload including
without Dice Roller, and full-profile passes. This mission is therefore
**implemented/source-qualified; live acceptance pending**.

## Decisions

- No preview rebuild is forced at roll time: a rebuild would natively drop the
  recorded point-buy `AddStatPoint`/`RemoveStatPoint` actions (allocator
  disabled) and break point-buy reconstruction after Return to Point Buy. The
  narrow refresh covers the caches without touching the action list.
- Post-replay dropped actions at Commit are reported as definitive failures;
  no post-`SetupNewCharacher` rollback is attempted on a real committed
  character.
- `LevelUpState`-on-source is treated as commit-unique (IL-proven: only
  `ApplyLevelup` from `Commit` constructs there) instead of adding a new
  Harmony patch.

## Next steps

1. Owner-authorized live smoke test per `docs/SMOKE-TEST.md` on a disposable
   fixture; capture the FINAL PASS/FAIL records and allowance displays for
   new-main, mercenary, and respec lanes.
2. Save/exit/restart/reload verification, including loading without Dice
   Roller.
3. Provider-matrix runs (Bag of Tricks budgets, Call of the Wild, the actual
   trait provider used in the report) in fresh processes.
4. On live acceptance: version bump, release-notes entry, owner-authorized
   publication (not authorized by this mission).
