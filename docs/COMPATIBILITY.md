# Compatibility

## Policy

Optional mods are detected by UMM ID/assembly evidence but never referenced as
compile-time dependencies and never modified. A detector result is a warning,
not runtime qualification.

## Bag of Tricks

Dice Roller observes `StatsDistribution.Start(int)` and current allocator
fields. It does not assume a 25-point budget. The point-buy origin records the
live total, remaining points, allocation, availability, and budget provenance
immediately before Roll Mode.

Roll Mode disables native spending independently of point costs. Informational
point-buy equivalent never drives restoration. Return to Point Buy invokes the
captured allocator budget normally so compatible Bag of Tricks patches can run,
then restores and verifies the captured allocation and fields on the newest
preview.

Positive live evidence exists for default-budget entry/restoration with Bag of
Tricks active, but alternative budgets/settings and the full matrix remain
unqualified.

## Call of the Wild

Dice Roller stores base ability arrays only. Race and heritage modifiers remain
on Kingmaker/CotW stats and are displayed through the native modifier column.
Preview rebuilds caused by added races reuse the current array and assignment.
No CotW blueprint, class, or assembly contract is imported.

A limited new-character entry smoke passed with Call of the Wild installed.
Added-race navigation, restoration, and the complete matrix remain unqualified.

## Gunslinger and other class mods

The workflow has no class-content, firearm, or blueprint dependency. It is a
separate assembly and must never be merged into Gunslinger/Tabletop Expansion.
Class-phase rebuilds may rebind the current preview but may not generate a roll.

## Required live matrix

Run a fully fresh process for:

1. Dice Roller alone.
2. Dice Roller + Call of the Wild.
3. Dice Roller + Bag of Tricks.
4. Dice Roller + both.
5. The intended full mod list.

For each configuration test PointBuy-first entry, Roll/Reroll, assignment,
history/recall, race change, exact restoration, completion, and context
isolation. Record Bag of Tricks' configured budget. Compatibility-qualified
remains **No** until this evidence exists.
