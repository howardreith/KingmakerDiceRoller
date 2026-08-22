# Compatibility policy

## General rule

No compatibility claim is made from code inspection alone. The mod minimizes
conflict by changing only one owned new-main-character session and by avoiding
normal plus/minus/cost methods.

## Current mod list

The detector recognizes likely IDs for Call of the Wild, Tweak or Treat, Races
Unleashed, Bag of Tricks, and common respec mods without hard assembly
references.

- **Call of the Wild, Tweak or Treat, Races Unleashed:** expected to be low-risk
  because this project neither creates blueprints nor changes race/class
  feature construction. They still require the smoke matrix.
- **Bag of Tricks:** explicitly unqualified. Bag of Tricks is known to alter
  character-creation point-buy behavior, so fixed-array entry, return to point
  buy, and subsequent plus/minus behavior must be tested with its actual
  settings. One live smoke with Bag of Tricks enabled proved that semantic
  pristine restoration eventually produced ordinary values, separate racial
  modifiers, 25 points, and native controls after phase re-entry. The open page
  remained stale until navigation, and alternative configured budgets were not
  tested, so this is not compatibility qualification.
- **Respec mods:** respec contexts are excluded. This mod is not a respec stat
  editor and should fail closed there.

## Qualification matrix

Run each row as a fresh process, not by hot-swapping assemblies:

| Configuration | New character | Back/forward | Race change | Return to point buy | Finish/save/reload |
|---|---:|---:|---:|---:|---:|
| Vanilla + UMM only | Required | Required | Required | Required | Required |
| Final mod list, Bag of Tricks off | Required | Required | Required | Required | Required |
| Final mod list, Bag of Tricks on/default | Required | Required | Required | Required | Required |
| Respec screen | Must remain vanilla/rejected | N/A | N/A | N/A | Existing save unchanged |

A detected mod name is diagnostic evidence, not proof of compatibility.
