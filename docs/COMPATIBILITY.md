# Compatibility

## Owner acceptance of 0.1.3

Howie confirmed the released workflow works and requested official release.
This is release acceptance of the unchanged 0.1.3 package. The confirmation did
not identify every provider/character case or save/reload result; the service,
contract, and development-session live matrix below retains its recorded scope.
newman55 remains unimplemented, and no additional provider is newly qualified.

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

## 0.1.3 candidate: installed provider audit

DATA inspection on 2026-09-06 found Eddic 1.0; no Respecialization DLL or fork
was found in the installed profile or relevant test downloads. All installed
assemblies were scanned for native respec/selector/launch/copy references. Bag of
Tricks changes the first-level budget and native respec cost; it was not found
to implement another complete respec engine. Its default player/mercenary budget
fields are 25/20, but restoration always uses the actual observed budget.

UMM 0.33.0 has no saved in-game Params.xml in the inspected installation. Eddic,
Dice Roller, and Bag of Tricks have no Settings.xml there. Consequently **enabled
state and active patch ownership were not observed in a running game**. Defaults
in an assembly are not an active-provider observation. Eddic's only saved-setting
contract is its selector hotkey (default F2); it exposes no Original-score option.
The adapter checks Eddic's live Enabled field and exact inspected MVID at entry.

Call of the Wild's installed settings include `update_companions=true` and
`reduce_skill_points=true`; Tweak or Treat uses
`spell_slots_from_permanent_bonus_only=true` and
`reduce_spell_slots_from_ability_drain=true`. These strengthen the need for the
focused live race/class/skill check. No settings were changed.

Actual relevant assembly inventory (hashes are SHA-256; full local inventory
also includes every other installed DLL and configuration filename):

| UMM ID | Info version | Assembly version | SHA-256 |
|---|---|---|---|
| BagOfTricks | 1.16.4 | 1.0.0.0 | `d03626594ece0f339aeef03ec7259684af7ddeb8b2edf41936f144848059a0e6` |
| CallOfTheWild | 1.14.4c-2.1 | 1.0.0.0 | `4ebf8e1ed3e66ffed72ea33ea325595629423dacd5bffa23e3c9109144b26915` |
| CheatMenu | 1.2.3 | 1.0.0.0 | `7d659eb092073ab9e059414f8bfbdfe46991cede56b394f58f7ea2bcc1ff0845` |
| EddicKingmakerRespec | 1.0 | 1.0.0.0 | `385c1ed87b5acb70092875ce28527cefb5698c64b61d3f706a27dc37161fc4d6` |
| KingmakerDiceRoller | 0.1.2 | 0.1.2.0 | `962d5968d5021db2868d39104b4cbfc3911fe1de00ebde85974a1a2b1b977acd` |
| RacesUnleashed | 1.0.11 | 1.0.0.0 | `6d18168cb90ffe60931addc8ee11e42b3ef647ef0e6d4b7ce8980d44659f4cb0` |
| TweakOrTreat | 1.1.0 | 1.0.0.0 | `a518324e15632aba46d6c467b156a31e9afd282e9827dee3e79ad14673852b92` |

Other installed IDs were BetterVendors, CraftMagicItems, KingmakerBuffPlanner,
KingmakerBugfixes, KingmakerGunslinger, KingmakerLastAzlantiPreserver,
ProperFlanking2, and ZFavoredClass. No separate full-respec launch was established
for them. Gunslinger contains a disposable developer respec test entry; it is
not a player-selector provider and does not qualify this adapter's ownership.
Removed listings, save editors, and Wrath-only RespecMod/ToyBox are not targets.

| Provider / character / mode | Service evidence | Installed contracts | Live UI / final / save |
|---|---|---|---|
| Dice only: new main and recruited mercenary | PASS (existing regressions) | PASS, 2.1.7b | NOT RUN |
| Native: main / mercenary six-score respec | PASS | PASS, 2.1.7b | NOT RUN |
| Native: story companion recruitment-level retraining | PASS rejection | PASS, base allocator absent | NOT RUN; rolling intentionally NOT APPLICABLE |
| Eddic 1.0: main / mercenary / story six-score respec | PASS shared owned lifecycle | PASS exact MVID; selector/NPC share native path | NOT RUN |
| Eddic: remote companion / NPC entry | Ownership boundary covered | PASS party/remote and callback contracts | NOT RUN |
| newman55 Respecialization: all categories / Original setting | NOT RUN adapter | NOT RUN: DLL unavailable | NOT RUN |
| BagOfTricks 1.16.4 modified budgets | PASS exact-origin restoration | Inspected constructor budget postfix (priority 200) | NOT RUN |
| CotW 1.14.4c-2.1 race/class / full intended profile | Shared modifier/layout tests only | Inventory and relevant hooks inspected | NOT RUN |
| Ordinary level-up, pets, enemies, pregens, locked scores, unowned previews | PASS rejection | Native first-level/allocator contract checked | NOT RUN; rolling intentionally NOT APPLICABLE |
| Simultaneous respec engines | NOT RUN | NOT RUN | NOT RUN, separate conflict lane |

PASS in the service/contract columns is not runtime or compatibility
qualification. Eddic source provides supporting context at
[PartialRespec.cs](https://github.com/eddic-code/EddicKingmakerMods/blob/main/EddicKingmakerRespec/EddicKingmakerRespec/Patches/PartialRespec.cs);
the installed DLL is the implementation authority. The published
[newman55 documentation](https://www.nexusmods.com/pathfinderkingmaker/mods/7)
is not sufficient to invent its launch and replacement signatures.
