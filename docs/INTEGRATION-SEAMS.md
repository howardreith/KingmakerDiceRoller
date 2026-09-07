# Kingmaker 2.1.7b integration seams

All production reflection contracts are resolved against the locally installed
`Assembly-CSharp.dll` before patches are installed.

Expected assembly evidence:

```text
MVID:    07fa1e4d-8618-41b3-9b8d-faa17d3b26f7
SHA-256: 3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb
```

## Context and stable ownership

```text
Game.Instance
Game.Instance.Player.MainCharacter
Game.Instance.UI.CharacterBuildController
CharacterBuildController.LevelUpController
LevelUpController.State
LevelUpController.Unit
LevelUpController.Preview
LevelUpState.Unit
LevelUpState.StatsDistribution
LevelUpState.IsFirstLevel
LevelUpState.CharBuildMode
LevelUpState.IsEmployee
Kingmaker.UnitLogic.UnitHelper.IsCustomCompanion(UnitDescriptor)
```

The stable owner for both accepted creation kinds is the exact controller
instance plus normalized controller source `UnitDescriptor`. State, preview
descriptor, distribution, and allocator are transient generations.

For a new campaign main character, `Player.MainCharacter` may be absent, may
normalize to the candidate, or may normalize to the stable controller source
for its owned preview. Unresolved or different identity fails closed.

For a mercenary, `Player.MainCharacter` must resolve successfully and remain a
different established descriptor. That relationship is necessary but never
sufficient: both exact custom-companion signals below must pass for the same
owned build.

```text
LevelUpState.IsEmployee == true
UnitHelper.IsCustomCompanion(LevelUpController.Unit) == true
```

Exact getter IL proves `LevelUpState.IsEmployee` calls
`UnitHelper.IsCustomCompanion(LevelUpState.Unit)`. Rechecking the stable source
prevents transient preview evidence from authorizing a different controller or
later build. Contract resolution verifies the instance/static shape, Boolean
return, exact `UnitDescriptor` argument, and the getter call token before any
patch is installed.

## Exact mercenary launch path

Exact 2.1.7b IL traces normal player recruitment as:

```text
Kingmaker.Designers.EventConditionActionSystem.Actions.CreateCustomCompanion.RunAction()
  -> Player.CreateCustomCompanion(Action, Nullable<Int32>, Boolean)
  -> ILevelUpInitiateUIHandler.HandleLevelUpStart(
       newCompanion.Descriptor, null, successCallback, CharBuildMode.CharGen)
```

Observed semantic relationships for that path:

```text
LevelUpState.CharBuildMode  = CharGen (numeric value 1)
LevelUpState.IsFirstLevel   = true
LevelUpController.Unit      = stable custom-companion source descriptor
LevelUpController.Preview   = current transient preview descriptor
LevelUpState.Unit           = current transient preview descriptor
Player.MainCharacter        = resolved, different campaign-main descriptor
StatsDistribution.Start     = invoked with the actual mercenary budget
```

The state/helper discriminator is sufficient without patching the launch path.
Cancellation and controller/source disappearance use the existing bounded
owner lifecycle. Mercenary completion now has two additional narrow delegated
postfixes at the authoritative native replay and final verification seams
described below.

New-main-character creation supports exact `CharGen` and its previously
qualified preview-time `LevelUp`. Mercenary creation supports only the observed
`CharGen` value. Respec uses the separate scoped contract below. `PreGen`, unknown
modes, and non-first-level progression remain rejected.

## Allocator model

```text
Kingmaker.UnitLogic.Class.LevelUp.StatsDistribution.Start(Int32)
StatsDistribution.IsComplete()
StatsDistribution.StatValues
StatsDistribution.Available
StatsDistribution.Points
StatsDistribution.TotalPoints
```

`Start(int)` is observed to capture the actual allocator budget and is invoked
normally during point-buy restoration so compatible patches can run. The mod
does not assume the mercenary's vanilla 20-point value and does not patch Add,
Remove, CanAdd, CanRemove, or cost methods. The origin also captures the exact
current base allocation, remaining/total points, allocator availability, and
preview values immediately before Roll or Recall. Reroll does not recapture it.

## Preview lifecycle

```text
LevelUpController.m_RecalculatePreview
LevelUpController.UpdatePreview()
```

Exact IL and live evidence show preview descriptors/states can be replaced
within one stable build. Rebind uses controller/source identity; it does not
generate a new array. Application verification resolves the controller's
current State and Preview after any replacement and compares both live
distribution and live unit base values.

## Authoritative mercenary finalization

Exact 2.1.7b IL establishes this synchronous completion order:

```text
LevelUpController.Commit()
  -> dispose LevelUpController.Preview.Unit
  -> ApplyLevelup(LevelUpController.Unit)
       -> new LevelUpState(target, prior mode)
       -> replay every ILevelUpAction.Check/Apply against target
  -> first-level SetupNewCharacher()
       -> insert LevelUpController.Unit through Player.CrossSceneState
          and Player.RemoteCompanions
  -> m_OnSuccess()
       -> supplied CreateCustomCompanion success callback
```

`UpdatePreview()` also calls `ApplyLevelup`, but supplies a replaceable Preview
descriptor. Its fresh `LevelUpState` owns a fresh `StatsDistribution`; matching
that distribution and preview therefore proves presentation only. The previous
implementation wrote the rolled assignment to those transient objects but did
not encode it in a native `ILevelUpAction`. When `Commit()` discarded Preview
and replayed actions against `LevelUpController.Unit`, the stable mercenary's
original allocation became authoritative. That is the exact point at which the
rolled preview was superseded.

The repair uses a postfix on private `ApplyLevelup(UnitDescriptor)` after all
native actions have replayed and before `SetupNewCharacher` or the success
callback. It writes only six `BaseValue` fields and only when all of these still
hold:

```text
session.CreationKind == Mercenary (immutable)
controller == session.Controller == active LevelUpController
target == session.StableOwner == LevelUpController.Unit == LevelUpState.Unit
mode == CharGen (numeric 1)
IsFirstLevel == true
IsEmployee == true
UnitHelper.IsCustomCompanion(target) == true
current preview generation already verified the exact assignment
```

Preview-target calls to the same method are ignored. New-main-character
creation shares Kingmaker's native `Commit`/replay mechanics, but this repair is
not applied to its distinct creation kind; its existing path remains unchanged.

A postfix on `Commit()` runs after the supplied success callback and reads that
same stable descriptor. It emits exactly one final PASS record only when all six
base values match. A mismatch emits a final FAIL record and the transient
session is still cleared; no session is retained to conceal an incomplete
commit. Duplicate observations are idempotent. Cancellation or loss of exact
ownership before the authoritative write cannot trigger a late commit.

Race and heritage modifiers are never copied into the six base values. No
custom persistence is needed: after native companion insertion, ordinary
Kingmaker descriptor/save data owns the result. Bag of Tricks is not referenced
by this lifecycle seam; Dice Roller only observes its effect, if any, on the
allocator budget. That narrow compatibility statement still requires live
confirmation.

## Native ability page

The exact path is:

```text
Game.Instance.UI.CharacterBuildController
  CurrentPhase == CharBPhase.Type.Skills
  Skills : Kingmaker.UI.LevelUp.CharBPhaseSkills
    AbilityScoresAllocator : Kingmaker.UI.LevelUp.CharBAbilityScoresAllocator
```

Verified members:

```text
CharBAbilityScoresAllocator.FillData() : void
CharBAbilityScoresAllocator.m_Unit : UnitEntityData
CharBAbilityScoresAllocator.m_PreviewUnit : UnitEntityData
CharBAbilityScoresAllocator.m_StatEntries : List<CharBScoresEntry>
CharBAbilityScoresAllocator.m_MainLabel : TMPro.TextMeshProUGUI
CharBAbilityScoresAllocator.m_Frame : UnityEngine.UI.Image
CharBAbilityScoresAllocator.m_RaceBonusContainer : UnityEngine.GameObject
CharBScoresEntry.UpButton : UnityEngine.UI.Button
CharBScoresEntry.DownButton : UnityEngine.UI.Button
UnityEngine.UI.Selectable.interactable : readable/writable Boolean
UnitDescriptor.Unit : UnitEntityData
```

Exact IL shows `CharBPhaseSkills.FillData(UnitDescriptor)` calls the allocator's
parameterless `FillData()`. The allocator binds current controller source and
preview entities, reads the current distribution, updates base/modifier rows,
updates points, and sets native button availability.

The panel uses a postfix on that exact parameterless method for both accepted
creation kinds. A data-only presenter is rendered into code-owned Unity objects
using the local allocator font and UI materials. Geometry never inspects the
visible character-screen title. `m_Frame` remains a verified material/style
source but its oval sprite is never used as the product-panel shape. The exact
`m_RaceBonusContainer` supplies preferred horizontal geometry for the compact
collapsed access tab when it is active and usable. Otherwise the host selects
the verified allocator frame, allocator `RectTransform`, or ability-phase root.
In every case it uses local Canvas coordinates to center the tab horizontally,
place it above the conservative bottom-navigation inset, and clamp it inside
safe bounds. There is no upper-right fallback. Repeated FillData calls cannot
create a second owned panel. Phase exit, invalid context, disable, and unload
detach it.

The owned root has no `Graphic`. Expanded and collapsed children are mutually
exclusive, so a collapsed view leaves no full-panel raycast target over native
Skills, Back, or Next controls.

## Presentation synchronization

Both directions use the same proven native refresh:

```text
semantic writes -> CharBAbilityScoresAllocator.FillData() -> exact binding and
live model/control verification
```

Roll synchronization additionally verifies all 12 native plus/minus controls
are non-interactable. Point-buy synchronization verifies current state,
distribution, source entity, preview entity, restored model values, allocator
fields, and active Skills phase.

No `UpdatePreview()` loop is used for UI-only synchronization. No visible label
is directly edited as a substitute for semantic state.

## Harmony surface

The exact six postfix targets are resolved before install:

```text
LevelUpState(UnitDescriptor, CharBuildMode)
StatsDistribution.Start(Int32)
StatsDistribution.IsComplete()
CharBAbilityScoresAllocator.FillData()
LevelUpController.ApplyLevelup(UnitDescriptor)
LevelUpController.Commit()
```

No mercenary-launch method is patched. Each patch bridge method delegates
immediately to the coordinator or panel host. Contract resolution proves the
`Commit -> ApplyLevelup(Unit) -> SetupNewCharacher -> m_OnSuccess` token order
and action replay shape before installing any hook. Failure to resolve a target,
custom-companion discriminator, finalization order, or UI recovery contract
prevents partial Roll Mode integration.

## Scoped native/Eddic respec (0.1.3 candidate)

The optional `NativeRespecContracts` cache pins the inspected 2.1.7b MVID and
exact member shapes. Failure removes only this adapter's patches. Eddic 1.0 is
identified by loaded assembly MVID plus its live `Main.Enabled` field. Installation
alone never establishes an active provider. No new patch ordering is imposed.

Observed PC lifecycle:

```text
CharSelectWindow.OnButtonOk (shown selector, exact CurrentCharacter)
  -> Player.RespecCompanion(original entity, provider/NPC success action)
    -> CreateUnitVacuum -> newUnit (separate rebuild source)
    -> native closure stores original unit, newUnit, Player, onSuccess
    -> CharacterBuildController.HandleLevelUpStart(newUnit.Descriptor, null,
         native copy callback, Respec)
       -> LevelUpController.Start -> constructor -> state/preview
       -> bind CharacterBuildController.LevelUpController -> Show
```

The selector prefix creates an exact-entity synchronous invocation scope; launch
postfix admission consumes it. Constructor-time incomplete binding cannot become
a permanent rejection. Unrelated constructors and nested pets cannot consume a
selected respec. A thrown selector call expires its scope on stack unwind, even
though Harmony12 has no finalizer API. The scope is tested on desktop .NET with
DATA's Harmony12 alone and together with the installed HarmonyLib. Unity/Mono
behavior remains a live gate.

Admission additionally requires original party/remote ownership, a source at
level zero, native `Respec`, `IsFirstLevel`, a separate owned preview, and an
available six-score distribution. It never uses name, portrait, blueprint, point
budget, or main-character flags to join original and clone. Eddic suspends the
story companion's class-level floor and reapplies race through native actions;
Dice Roller changes no identity/race/class phase flags. Without that change,
native recruitment-level story builds do not reopen starting allocation.

Commit uses a separate one-use ticket and immediately releases the active roll
session. The fresh source `LevelUpState` constructor inside that exact Commit
invocation receives the array and disabled point-buy distribution **before**
`ILevelUpAction.Check/Apply`. This preserves native skill/feature checks that
read ability values; preview reconstruction uses the same early staging for its
owned generation. Explicit Roll/reassignment/Point Buy return invokes cached
`LevelUpState.OnApplyAction()` to refresh native derived skill/spell limits.

`ApplyLevelup` postfix verifies the source without rewriting it. Native Commit
then performs first-level setup and invokes the cached native respec callback:

```text
newUnit.PreSave -> PrepareRespec -> JObject.FromObject(newUnit)
  -> JsonConvert.PopulateObject(..., original unit)
  -> PostLoad / native restoration -> success action
  -> IUnitChangedAfterRespecHandler(original unit)
```

An exact callback postfix records completion. Commit postfix requires that
observation plus matching source replay and the original entity's **current**
descriptor values. Matching an obsolete source or coincidental preview alone
cannot pass. The initial ticket expires before CharacterBuildController starts
catch-up `HandleLevelUpStart(Unit)` in ordinary LevelUp mode. Native
`SpendAttributePoint` later increments BaseValue normally. No initial-array
write is permitted on those levels.

`PrepareRespec` clears inventory/body references, not six ability BaseValues.
Fixed racial effects remain native modifiers; `SelectRaceStat.Apply` adds a
racial modifier rather than adding to BaseValue. Dice Roller writes no facts,
unit parts, blueprints, save metadata, or provider retention settings.

Cancel, loss of original/source ownership, provider disable, Dice disable, and
exceptions close transient scopes. A failed source write restores only the exact
captured source/state snapshot. Exceptions after native serialization may already
have changed provider-owned data; Dice Roller reports failure and never attempts
arbitrary live-character rollback. Eddic/native cancellation behavior itself is
not rewritten.

`Verify-RespecContracts.ps1` resolves production contracts and checks native IL
instruction order; its evidence is separate from service fixtures and live Unity.
newman55 replacement/Original-score/legacy-mode launch and recipient methods
remain unresolved because its exact DLL was unavailable. No guessed adapter or
Wrath API was added. Console `RetrainVm.Confirm` was identified but this candidate
uses the existing PC ability-panel/selector path only.
