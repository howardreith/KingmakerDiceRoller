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

The state/helper discriminator is sufficient without a launch token, so no
fifth Harmony entrypoint exists. Completion and cancellation are handled by the
existing exact controller/source lifecycle: completion releases immediately;
controller/source disappearance is released after bounded liveness
confirmation; disable/unload clears the session and all owned UI.

New-main-character creation supports exact `CharGen` and its previously
qualified preview-time `LevelUp`. Mercenary creation supports only the observed
`CharGen` value. `PreGen`, `Respec`, unknown modes, and non-first-level
progression are rejected before a session can expose UI.

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
`m_RaceBonusContainer` supplies the preferred local anchor for the compact
collapsed access tab; an upper-right local fallback is bounded and fail-safe.
Repeated FillData calls cannot create a second owned panel. Phase exit, invalid
context, disable, and unload detach it.

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

The exact four postfix targets are resolved before install. No mercenary-launch
patch was added. Each patch bridge method delegates immediately to the
coordinator or panel host. Failure to resolve any required target,
custom-companion discriminator, or UI recovery contract prevents partial Roll
Mode integration.
