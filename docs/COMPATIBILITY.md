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

The same observation applies to mercenary recruitment. Vanilla evidence may
show 20 points, but Dice Roller neither detects mercenaries by budget nor
hard-codes that value. A compatible altered mercenary budget must round-trip
from the captured live origin.

Roll Mode disables native spending independently of point costs. Informational
point-buy equivalent never drives restoration. Return to Point Buy invokes the
captured allocator budget normally so compatible Bag of Tricks patches can run,
then restores and verifies the captured allocation and fields on the newest
preview.

The mercenary persistence repair does not reference Bag of Tricks. Exact
Kingmaker IL shows the authoritative finalization lifecycle is the native
`Commit -> ApplyLevelup(Unit) -> SetupNewCharacher -> callback` path; the known
Bag of Tricks interaction is allocator-budget policy, not a separate Dice
Roller completion owner. This is a contract inference, not live compatibility
qualification. The focused Bag of Tricks gate must still prove the observed
budget/origin round-trip and final/reloaded mercenary base values.

Historical alpha.2 live evidence exists for default-budget main-character
entry/restoration with Bag of Tricks active. Repair-candidate mercenary
completion, alternative settings, save/reload, and the full matrix remain
unqualified.

## Call of the Wild

Dice Roller stores base ability arrays only. Race and heritage modifiers remain
on Kingmaker/CotW stats and are displayed through the native modifier column.
Preview rebuilds caused by added races reuse the current array and assignment.
No CotW blueprint, class, or assembly contract is imported.

A limited alpha.2 new-character entry smoke passed with Call of the Wild
installed. Repair-candidate main/mercenary added-race navigation, restoration,
completion, and the complete matrix remain unqualified.

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

For each configuration test main-character and mercenary PointBuy-first entry,
Roll/Reroll, assignment, history/recall, race change, exact restoration,
completion, and context isolation. Record Bag of Tricks' configured budget for
both creation kinds. Compatibility-qualified remains **No** until this evidence
exists.
