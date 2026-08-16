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

Kingmaker.UnitLogic.UnitDescriptor
  Stats.GetStat(StatType).BaseValue : Int32

Kingmaker.EntitySystem.Stats.StatTypeHelper.Attributes
  Strength, Dexterity, Constitution, Intelligence, Wisdom, Charisma

Kingmaker.Game.Instance.LevelUpController
  State : LevelUpState
  m_RecalculatePreview : Boolean
  UpdatePreview() : Void
```

The context policy additionally requires readable Boolean identity paths for
main-character, player-faction, pet, and enemy status. Candidate paths are
resolved conservatively on the runtime unit object.

## Why reflection is used

Harmony still targets exact `MethodBase` objects, but the production code does
not compile business logic against unstable game members. The resolver verifies
all signatures once, caches the members, and refuses installation when any
required seam differs. This makes a 2.1.7b mismatch a visible enable failure
rather than a partially working character creator.

## Ordering

All three postfixes use `Priority.VeryLow`. In particular, the `Start(int)`
postfix observes the final allocator call and records the actual budget. During
point-buy restoration, normal modded allocator patches run before this mod's
postfix suppresses fixed-array reapplication.

## Not patched

`StatsDistribution.Add`, `Remove`, `CanAdd`, `CanRemove`, point-cost methods,
race/class feature application, companion level-up, respec controllers, and
save serialization are intentionally untouched.
