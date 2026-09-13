# Native UI style reference

This is the Dice Roller 0.1.7 activation repair of the existing 0.1.6 style, against
Kingmaker 2.1.7b, Unity 2018.4.10f1. Assembly-CSharp MVID:
`07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`; SHA-256:
`3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`.
Qualification is recorded separately in
[the activation repair handoff](../CODEX-NATIVE-THEME-ACTIVATION-STATE.md).
Do not call fallback or compiled geometry a native-aesthetic pass.

## Resolution and availability

`UI/NativeBookTheme.cs` obtains the exact allocator's ancestor
`CharacterBuildController`. Its Unity adapter invokes the production
`NativeThemeResolver`; deterministic tests use that same resolver with an
opaque node adapter. Unity-dependent validation is additionally exercised by
the temporary runtime probe, outside the distributed mod.

**Locator semantics matter:** the table below contains hierarchy paths.
`Next/Complete text` is instead one **literal direct child name** of Action.
Use `RequirePath` for the table and `RequireLiteralChild` for that label.
Do not pass this literal name to `Transform.Find`: `/` becomes a separator.
Read-only decoding of both current PC scenes confirms the literal child and
TMP component, with no intervening `Next` object. Exact ordinal direct-child
matching includes inactive nodes, rejects ambiguous children/components, and
never substitutes arbitrary text. Paths walk bounded direct-child segments
(maximum depth 32, 512 children per segment). Diagnostics distinguish missing
children/components and include locator kind, actual owner breadcrumbs and at
most six candidate child names.

No native objects are renamed, no callbacks cloned, and no scene-wide search
is performed. Cache liveness uses Unity object identity and destruction
semantics, including ownership ancestry; destroyed or detached donors lose
only their affected capability.

All eight paths and their required components were decoded in both PC scenes:
BuildSettings scene `level18` (`Assets/Scenes/MainMenu/MainMenu.unity`) and
`level11` (`Assets/UI/UI_Ingame_Scene.unity`). Their character-build hierarchies
already contain the donors, including inactive appearance/book subtrees.
This establishes serialized availability without visiting Inventory or loading
a campaign; actual fresh-launch initialization/rendering remains a live gate.
Gamepad-specific scenes and other game builds are not newly qualified.

Paths relative to `CharacterBuildController`:

| Key | Exact path |
| --- | --- |
| Paper | `Body/Content/ClothColorSelector/PrimarySelectorPlace/ColorSelector/Background` |
| Action | `Body/Bottom (1)/ButtonsPlace/BackButton` |
| Heading | `Body/Content/SkillsMiddleScoresAllocator/Content/STR Background/Labels/SHORT` |
| Body | `Body/Content/Book/Image_Book/Container_SpellsLeft/Spells_Container/SpellBookItem (2)/Item/Body/NamePlace/LabelName` |
| Selector | `Body/Content/RaceRightSide/Head/SequentialSelector/SequentialSelector/GameObject/Frame/Label` |
| Input | `Body/Content/CharacterMiddleSide/CharacterName/PointsBox/Bg` |
| Scroll | `Body/Content/SkillsLeftSide/MartiaAndSaves/SpellTable/DescriptionView/Scrollbar Vertical` |
| Rule | `Body/Content/RaceRightSide/Constitution/DescriptionView/Decor (1)` |

## Paper and ornament

Paper is an Image using `dialogue_backsheet` (sharedassets6.assets sprite 339),
858 x 551 pixels, Sliced, borders **L268/B169/R258/T165**, white vertex color,
default UI material, filled center. The same sprite appears on the native
DialogMessageBox. Direct pixel inspection shows mottled paper, uneven/curled
edges and baked depth; its material alone would not provide that texture.

The mod owns separate PaperShadow and Paper images under a transparent bounded
interactive surface. Both ignore layout and reject raycasts. They are centered,
double-sized and uniformly scaled to 0.5, retaining effective borders
L67/B42.25/R64.5/T41.25 without modifying shared assets: this sprite uses
200 pixels per unit, and the serialized CanvasScalerWorkaround reference is
100 pixels per unit. Its reference resolution is 1920x1080; actual runtime
canvas sizes and scale factors remain unobserved. Supported compact paper
starts at 380 x 500; preferred wide is 600 x 728, compact 460 x 650, clamped to
the existing parent and navigation insets. Their widths exceed the effective
131.5-unit horizontal border sum. Extreme smaller parents still clamp safely but
are not an aesthetic acceptance claim. Unity 2018 Image has no
`pixelsPerUnitMultiplier`; adding such an assumed API would not compile.

A second silhouette at (2,-3), brown alpha 0.24, adds modest depth. The native
`blockscroll_bottom` rule (sharedassets4 sprite 458, 147 x 11, borders
L20/B0/R20/T0, Sliced) appears once beneath the fixed heading. No outer
RectMask2D squares off these decorative edges. Only the inner scroll body and
input viewport are masked. Nothing is rendered or hit-tested as a full-screen
veil or complete second book.

## Buttons and activation

The Action source has an Image, `Kingmaker.UI.Constructor.ButtonPF` (a Unity
Button subclass) and TooltipTrigger. Copy only Image and Selectable properties.
Do not copy the GameObject or any behaviour/event/tooltip binding.

| State | Sprite in sharedassets4.assets | Geometry |
| --- | --- | --- |
| Normal | `button_normal`, 561 | 449 x 71, borders 16 on every side |
| Highlighted | `button_hover`, 408 | same |
| Held | `button_pressed`, 316 | same; visibly inset/darker artwork |
| Disabled | `button_disable`, 343 | same |

The native transition is **SpriteSwap**, Image type Sliced; no Animator is
required. All owned buttons use that state set, not custom dark tints.
Collapsed Roll Stats is 164 x 38; Close 76 x 34; ordinary rows/buttons 34 high.
Small selectors are 42 wide, minimum controls 44, assignment Up/Down 64. These
remain above the 16-unit sum of the effective borders (200 sprite PPU /
100 canvas reference PPU). The six assignment rows
say **Up/Down**, preserving permutation meaning rather than native score-buying
plus/minus semantics. Native camp/directional icon buttons were inspected but
not imported: ambiguous stat arrows and unrelated callback machinery offer no
benefit over explicit assignment text and the compact common frame.

ButtonPF declares no Awake/OnEnable/OnSubmit override in this build. It adds
right-click handling, hover hooks and disabled/right-click audio. Serialized
Back onClick is empty, but `CharacterBuildController.Initialize` adds navigation
listeners at runtime. Empty persistent listeners are therefore not cloning
permission. The native score-entry buttons also own score-changing routes.

`CreateButton` instead constructs a fresh plain Unity Button, replaces
`onClick` with a **new** ButtonClickedEvent and adds exactly one owned listener.
The listener dispatches feedback and the intended action through
`NativeUiPresentation.Activate`. All workflow actions still use
`RollUiCommandRouter`. Installed Unity Button IL sends pointer click and Submit
through Press, which checks active/interactable and invokes the event once.
The normal Selectable machinery handles held visuals and cancellation. Live
held/drag-away/submit/focus behavior must still be tested in the engine.

Interactability/visibility changes are applied only when their values change;
no render-time theme/state recreation or selection reset suppresses a held
press. Opening selects Close; closing restores focus to Roll Stats. No new
keybinding or Back/Next route is introduced. Close calls the existing
`NotifyDrawerClosed` synchronization once through the shared presentation seam.

## Typography

All sources use `NexusSerif-Regular SDF` (sharedassets0 font 237). Materials are
shared read-only and retain authored face/underlay/shader settings. All four
inspected materials have `_OutlineWidth=0`, `_OutlineSoftness=0`, white face
color and zero face dilate. Do not set TMP outline properties or `fontMaterial`
on these controls; that can introduce per-object materials or alter styling.

| Role | Source/shared material | Source -> owned size; style and color |
| --- | --- | --- |
| Heading | Heading; `NexusSerif-Regular SDF - Simple Color` (sharedassets4 material 14) | 20 -> title 24 / section 20; Bold + UpperCase; reddish brown RGB 150/62/27; no underlay |
| Body, assignments, history, input | Body; `... - Books` (sharedassets0 material 3) | 18 -> 18; Regular; source near-charcoal, owned dark brown RGB 40/17/9 from the native selector palette; authored faint white underlay retained |
| Status/error | Body / Books | 16; Regular; dark brown or error red-brown; wrapping, measured height |
| Button | Action child `Next/Complete text`; `... - ButtonLabel` (sharedassets4 material 10) | 22 -> 18; UpperCase; native pale gold RGBA (1,.899,.684,1); authored brown underlay retained |
| Selector | Selector; `... - SpellbookBookmark` (sharedassets4 material 15) | 28 -> 18; Regular; RGB 40/17/9, native parchment underlay; wraps and grows at narrow widths |

Character/word/line spacing are all zero in these donors and copied. Normal
text is never globally scaled to squeeze the panel. Labels reject raycasts and
rich text is disabled for mod/user strings. Summary, history, saved and errors
use preferred text height; long selectors grow their row instead of shrinking
or hiding the selected rule. Saved actions occupy two compact rows to preserve
reachability at constrained widths.

The native font asset's fallback chain remains intact; no extracted fonts,
foreign localization component or new localization table ships. Native donors
may already reflect the selected language at attachment. Glyph coverage,
language changes while attached, translations and controller-only scenes have
not been exercised; do not claim localization or input-method expansion.

## Input and scrollbar

Input's Image is `SlotBackgroundFilled` (sharedassets4 sprite 344, 190 x 46,
borders 9/9/8/9, 100 PPU), tinted by the donor; child `Frame` uses `map_cameraframe`
(sprite 571, 46 x 30, borders 7). The frame and other selected sprites use 200 PPU. Both are Sliced. A fresh TMP_InputField owns
its own text, placeholder and masked viewport. Copy only selection color
(1,.985,.656,.753), caret color (.196,.196,.196,1) and blink rate .85 from the
native `InputField` child. Its original select/deselect callbacks invoke
`CharBNameInput.OnFocus/Lost` and must never be retained.

Scroll's Sliced Image uses `SliderArea` (sprite 386, 54 x 1446, borders
0/62/0/52); `Sliding Area/Handle` uses `ScrollBar_Big_Handler` (sprite 301,
32 x 156, borders 0/48/0/45). A fresh Scrollbar gets only those images and native
ColorTint states (white / .96 highlight / .784 press), attached to the owned
vertical ScrollRect. Width is 20; track and handle are explicit hit targets.
It appears only on measured overflow. Very small thumbs, long errors and
real CanvasScaler geometry remain part of live visual review.

## Click audio and dynamic panels

Actual ordinary route:

```text
accepted owned Button.onClick
  NativeUiPresentation.Activate
    Game.Instance.UI.Common.UISound.Play(UISoundType.ButtonClick)
      UISoundManager.Play(type, Game.UI.Common.gameObject)
        authored sound entry -> AkSoundEngine.PostEvent
    intended mod action once
```

`UISoundType.ButtonClick` is 0. Main-menu Common's UISound points at
sharedassets6 object 5354 (`UISoundManager`); entry 0 is **ButtonClick**. Installed
Wwise metadata places event 4051332235 in `Main.bnk`, event path
`Interface/Main/ButtonClick`, authored as OneShot. No bank is loaded by the mod.
Native SoundSettingsController uses **AudioLevel** for master volume and
ToggleMuteMasterVolume; no separate UI-volume slider was found there. The owned
call preserves the native event's mixing and emitter. The final Wwise bus graph
and audible master mute/volume response are not live-verified; test beside Back
using native master controls. No custom audio, emitter, UI-volume bypass,
retained ButtonPF sound helper, hover sound or second Submit sound is added.
Feedback occurs once at accepted activation, before the mod action; failures
log once per distinct attachment diagnostic and do not prevent that action.
Disabled/canceled pointer behavior itself needs the engine lane.

DialogMessageBox is evidence/donor only: Initialize registers the global
instance, subscribes EventBus, binds Yes/No/OK and initializes WindowAnimator;
Show/Hide manage a veil and Escape registration. That shared modal lifecycle
is unsuitable for this nonmodal editor. No dialog constructor or foreign mod
is used as an ownership shortcut.

## Failure, cleanup and adding controls

The host constructs owned controls once with functional fallback and inactive
owned paper/decor images. `NativeThemeBindings` applies validated presentation
to those same controls; it never registers listeners, dispatches commands,
edits input drafts or rebuilds the session. The nine independent capabilities
are Paper, Buttons, Heading, Body, ButtonText, Selector, Input, Scrollbar and
Ornament. Each validates all its required resources before becoming available.
Missing optional styling retains valid paper, buttons and typography. Button
artwork does not depend on action-label typography.

`FullyThemed` means all nine intended capabilities validated and applied;
`PartiallyThemed` means one through eight; `Fallback` means zero. Application
failure rejects only the affected capability. Logs report actual state and
specific reasons, suppressing unchanged details. A missing game-contract MVID
still fails closed. Shared sprites/fonts/materials are never modified/destroyed.

Recovery allows initial resolution plus **at most two** subsequent existing
`CharBAbilityScoresAllocator.FillData` postfix notifications. Installed IL
verifies `SetupUI -> CharBPhase.UpdateData -> CharBPhaseSkills.FillData ->
CharBAbilityScoresAllocator.FillData`. Own skill synchronization shares this
route; attempts are consumed before resolving and reentrancy is blocked.
Repeated notifications cannot reset the budget. Ordinary Update only checks
cached liveness. Actual teardown, new allocator or replaced owner starts a new
budget. Unchanged valid resources are not reapplied on optional retries.

Recovery retains widget identity, listeners, selected saved array, roll history,
assignments, RNG consumption and unfinished custom input. Tests exercise actual
workflow/coordinator/navigation boundaries as well as the production resolver.
Teardown clears cached donors, bindings and owned widgets; only the owned root
is destroyed, and the existing input-blocking/native-control cleanup remains.

Rebinding preserves same-owner expansion/disclosures. No new textures,
materials, emitters, native behaviours or persistent listeners are created.
Existing native control suppression/restoration and context admission stay in
their original services.

To add another consistent control: use the existing CreateButton/CreateLabel
helper and command router; give it a measured/bounded layout role; keep text and
decoration noninteractive. For a new donor, first inspect its exact path in both
PC scenes, serialized state/artwork/borders/material and behaviour dependencies;
then extend the narrow resolver, contract evidence and smoke matrix. Never clone
a whole controller or copy onClick. Keep decorations outside the body mask and
messages inside measured content. Rerun the existing deterministic runner and
native UI IL verifier, then qualify the exact candidate visually when authorized.

Rejected sources include the allocator's curved/oval m_Frame, a material without
paper pixels, full Inventory/Spellbook book frames, modal message-box ownership,
and native navigation/stat-changing behaviours. The original twelve owner
screenshots were all inspected; near-duplicate stills were not treated as proven
hover/pressed states or audio evidence.

Local ignored evidence is under `artifacts/native-ui-reskin/`: scene component
metadata, `full-inventory.txt`, `font-materials.txt`, sprite inspections, native IL
and build logs. Serialized component reads used version-specific type trees with
strict byte consumption for the selected donors; unrelated undecodable components
were recorded rather than used. `artifacts/contracts/native-ui-contracts.json`
records the reproducible 16 installed/candidate IL checks. None of this is a
candidate game capture, and no extracted artwork is committed or packaged.
