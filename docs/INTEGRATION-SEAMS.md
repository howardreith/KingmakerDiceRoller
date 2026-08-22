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
```

The stable owner is the exact controller instance plus normalized controller
source `UnitDescriptor`. State, preview descriptor, and distribution are
transient generations. `Player.MainCharacter` may be absent, may normalize to
the candidate/controller preview relation, or may be a different established
descriptor. Unresolved or different identity fails closed.

Supported modes are exact Kingmaker `CharGen` and the observed preview-time
`LevelUp`; `PreGen`, `Respec`, unknown modes, and non-first-level progression
are rejected before a session can expose UI.

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
does not patch Add, Remove, CanAdd, CanRemove, or cost methods.

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
CharBScoresEntry.UpButton : UnityEngine.UI.Button
CharBScoresEntry.DownButton : UnityEngine.UI.Button
UnityEngine.UI.Selectable.interactable : readable/writable Boolean
UnitDescriptor.Unit : UnitEntityData
```

Exact IL shows `CharBPhaseSkills.FillData(UnitDescriptor)` calls the allocator's
parameterless `FillData()`. The allocator binds current controller source and
preview entities, reads the current distribution, updates base/modifier rows,
updates points, and sets native button availability.

The panel uses a postfix on that exact parameterless method. A data-only
presenter is rendered into code-owned Unity objects styled from the local
allocator label, frame, and button. Repeated FillData calls cannot create a
second owned panel. Phase exit, invalid context, disable, and unload detach it.

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

The exact four postfix targets are resolved before install. Each patch bridge
method delegates immediately to the coordinator or panel host. Failure to
resolve any required target or UI recovery contract prevents partial Roll Mode
integration.
