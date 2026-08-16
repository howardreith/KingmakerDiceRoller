# Deferred native UI design

The fixed-array gate intentionally uses only the UMM diagnostics panel. A native
character-generation UI should not be implemented until the integration seam is
runtime-qualified.

## Planned controls

- Preset selector: 4d6 drop lowest; 4d6 reroll ones drop lowest; 3d6; 2d6+6;
  1d20; custom expression.
- Explicit low-score policy: tabletop; reroll individual below minimum; reroll
  whole array below minimum.
- Roll button and current immutable array.
- Six assignment rows with position-based swap/up/down controls.
- Point-buy-equivalent label, clearly marked extended outside 7–18.
- History and saved arrays with validated schema.
- Return-to-point-buy recovery control.

## Behavioral constraints

A roll occurs only from an explicit user action. Phase rebuilds reuse the same
array. Duplicate values remain distinct by source position. The UI must display
both base assignment and racial modifiers without baking modifiers into the
stored array. No vanilla plus/minus control should remain active while roll mode
owns completion.

## Native integration research

Before implementation, decompile and verify the actual ability-phase view,
controller, bindings, prefab hierarchy, and refresh lifecycle from the target
2.1.7b assemblies/assets. Do not clone the Wrath mod's UI or assume WotR paths.
