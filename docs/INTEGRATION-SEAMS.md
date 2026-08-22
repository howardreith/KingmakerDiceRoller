# Kingmaker integration seams

## Required exact contracts

The runtime resolver and offline PowerShell probe both require:

```text
Kingmaker.UnitLogic.Class.LevelUp.LevelUpState
  .ctor(UnitDescriptor, LevelUpState.CharBuildMode)
  Unit
  StatsDistribution
  IsFirstLevel

Kingmaker.UnitLogic.Class.LevelUp.StatsDistribution
  Start(Int32) : Void
  IsComplete() : Boolean
  StatValues : IDictionary-compatible
  Available : Boolean
  Points : Int32
  TotalPoints : Int32

Kingmaker.UnitLogic.UnitDescriptor
  Stats.GetStat(StatType).BaseValue : Int32

Kingmaker.EntitySystem.Stats.StatTypeHelper.Attributes
  Strength, Dexterity, Constitution, Intelligence, Wisdom, Charisma

Kingmaker.Game.Instance.UI.CharacterBuildController.LevelUpController
  State : LevelUpState
  Unit : UnitDescriptor
  Preview : UnitDescriptor
  m_RecalculatePreview : Boolean
  UpdatePreview() : Void

Kingmaker.Game.Instance.Player.MainCharacter
  MainCharacter : UnitReference
  Value : UnitEntityData
  Descriptor : UnitDescriptor

Kingmaker.Game.Instance.UI.CharacterBuildController
  CurrentPhase : Nullable<CharBPhase.Type>
  Skills : CharBPhaseSkills

Kingmaker.UI.LevelUp.Phase.CharBPhase.Type
  Skills

Kingmaker.UI.LevelUp.Phase.CharBPhaseSkills
  AbilityScoresAllocator : CharBAbilityScoresAllocator

Kingmaker.UI.LevelUp.CharBAbilityScoresAllocator
  FillData() : Void
  m_Unit : UnitEntityData
  m_PreviewUnit : UnitEntityData

Kingmaker.UnitLogic.UnitDescriptor
  Unit : UnitEntityData
```

The context policy additionally requires readable Boolean identity paths for
main-character, player-faction, pet, and enemy status. Candidate paths are
resolved conservatively on the runtime unit object.

In the exact 2.1.7b new-game lifecycle, `Player.MainCharacter` holds the
controller's source `Unit`, while `LevelUpState.Unit` is the separately
deserialized controller `Preview`. A valid new-game relation therefore permits
either direct descriptor identity or main-character identity with the
controller source when the candidate is owned by that controller. A different
main descriptor is rejected, and unresolved identity fails closed.

`LevelUpController.UpdatePreview()` is synchronous but its object identity is
not stable. It replaces `Preview`, invokes `new LevelUpState(Preview, mode)`,
assigns the returned state only after that constructor (and its Harmony postfix)
returns, then replays level-up actions. Preview descriptors are deserialized
clones and differ by reference. `LevelUpController.Unit` remains the stable
source descriptor across these generations.

Accordingly, a session owns the exact controller/source pair and rebinds its
state, preview, distribution, and generation rollback snapshot when another
accepted constructor belongs to that pair. Its pristine first-generation
point-buy origin and immutable assignment do not rebind. Fixed-array staging
does not invoke `UpdatePreview()`. The actual controller state/preview, both
value stores, and disabled allocator state must agree before an application is
recorded. Explicit refresh is reserved for point-buy restoration and has a
nested-call guard.

## Why reflection is used

Harmony still targets exact `MethodBase` objects, but the production code does
not compile business logic against unstable game members. The resolver verifies
all signatures once, caches the members, and refuses installation when any
required seam differs. This makes a 2.1.7b mismatch a visible enable failure
rather than a partially working character creator.

## Ordering

All three postfixes use `Priority.VeryLow`. In particular, the `Start(int)`
postfix observes the final allocator call and records the actual budget. Exact
2.1.7b IL shows that `Start(int)` sets `Available`, `Points`, and `TotalPoints`;
it does not reset the six score values. During point-buy restoration, normal
modded allocator patches run before this mod's postfix observes the restoring
mode and leaves fixed-array staging suppressed. The pristine allocation is then
restored and verified on the live preview.

The open ability page is refreshed only after semantic restoration has entered
durable PointBuy mode. Exact 2.1.7b `CharBAbilityScoresAllocator.FillData()` IL
rereads `LevelUpController.State`, its `StatsDistribution`, the source and
preview units, racial modifiers, point totals/costs, and add/remove
availability. Calling that exact native method is narrower than another
`UpdatePreview()` and preserves Owlcat's presentation lifecycle. The call is
limited to one attempt per session generation, rejects nested invocation, and
is followed by reference checks of the current state/distribution and the
allocator's `m_Unit`/`m_PreviewUnit` bindings.

The character-build controller sets `LevelUpController` to null from its hide
and dispose lifecycle. Session liveness therefore follows controller/source
identity and tolerates transient null or replaced `State`/`Preview` values while
that stable pair remains active.

## Not patched

`StatsDistribution.Add`, `Remove`, `CanAdd`, `CanRemove`, point-cost methods,
race/class feature application, companion level-up, respec controllers, and
save serialization are intentionally untouched.
