# Kingmaker Dice Roller 0.1.0

Kingmaker Dice Roller adds an explicit rolled-ability workflow to the native
character-creation screen for two exact player contexts: a new custom campaign
main character and the player-initiated custom mercenary recruitment flow.

## Highlights

- Adds responsive Wide and Compact right-side panel layouts. Wide layouts keep
  the ordinary workflow visible without scrolling; Compact layouts use
  progressive disclosure and scroll only when measured content overflows.
- Provides explicit Roll and Reroll commands, duplicate-safe score assignment,
  History, persistent Saved arrays, and Return to Point Buy.
- Preserves the allocation and actual budget observed from the live point-buy
  allocator before Roll or Recall, including partially spent and mod-adjusted
  budgets, then restores that exact origin on Return to Point Buy.
- Rolls only after a player command. Preview, race, phase, allocator, geometry,
  and UI rebuilds never generate an array.
- Leaves completed characters with ordinary Kingmaker base ability values and
  creates no save-owned blueprint, fact, buff, component, or unit part.
- Continues to reject ordinary level-up, companion progression, respec,
  pregenerated selection, pets, enemies, unresolved ownership, and unknown
  character-build contexts.

## Supported environment and compatibility

- Pathfinder: Kingmaker 2.1.7b
- Unity Mod Manager 0.32.x
- Harmony12
- .NET Framework 4.7.2

Bag of Tricks and Call of the Wild are detected but never modified. The live
point-buy budget is observed rather than assumed, and racial modifiers remain
separate from rolled base values. Focused testing of the exact `0.1.0` artifact
is still required; this candidate does not claim exhaustive third-party
compatibility or completed in-game runtime qualification.
