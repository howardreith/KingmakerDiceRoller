# Native UI style reference

Source-qualified against Kingmaker 2.1.7b, Assembly-CSharp MVID
`07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`, SHA-256
`3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`.
Unity is 2018.4.10f1. All live visual/audio/interaction observations are **NOT RUN**.

## Verified first slice

`NativeBookTheme` resolves the exact current allocator's ancestor
`CharacterBuildController`. Paths below are relative to that controller.
Serialized PC main-menu (`level18`) and in-game (`level11`) scene metadata
contain the same donor paths. No Inventory visit or arbitrary loaded-object
search is required. Fresh-launch rendering still needs live confirmation.

| Role | Donor and contract |
| --- | --- |
| Paper | `Body/Content/ClothColorSelector/PrimarySelectorPlace/ColorSelector/Background`: Image, Sliced, white, default UI material; `dialogue_backsheet`, sharedassets6 sprite 339, 858×551, borders L268/B169/R258/T165. Same artwork as native DialogMessageBox background. Its mottling, irregular edges, curled ends and baked shadow were inspected directly. |
| Action | `Body/Bottom (1)/ButtonsPlace/BackButton`: ButtonPF with Image `button_normal`; SpriteSwap to `button_hover`, `button_pressed`, `button_disable`. All 449×71 with 16-pixel borders. No Animator required. |
| Button label | Action donor's `Next/Complete text`: NexusSerif-Regular SDF, shared `NexusSerif-Regular SDF - ButtonLabel`, 22 native units, UpperCase, pale gold, zero character/word/line spacing. |
| Paper heading | `Body/Content/SkillsMiddleScoresAllocator/Content/STR Background/Labels/SHORT`: NexusSerif, `Simple Color` material, Bold + UpperCase, reddish brown (150/62/27), 20 native units. |
| Click | `Game.Instance.UI.Common.UISound.Play(UISoundType.ButtonClick)`; the native manager resolves its serialized sound entry and calls Wwise on Common's GameObject. No new emitter or audio asset. |

The new Unity Button receives copied image and sprite-state properties and a
new click event with exactly one mod-owned listener. It never receives the
native object, controller, event listeners, localization components, or
bindings. CharacterBuild.Initialize binds Back/Next navigation at runtime:
those callbacks must never be copied. ButtonPF itself adds right-click and
disabled cues and does not add sound to inherited Submit. A plain Button with
one onClick audio/command route deliberately avoids those mismatches; installed
Unity Button IL routes left-click and Submit once through guarded Press.
No hover sound is added.

The paper uses a uniformly half-scaled, double-sized sliced Image so its
borders remain proportional at panel sizes. Unity 2018's Image has no
pixelsPerUnitMultiplier; no guessed property or mutated sprite is needed.
A second noninteractive paper silhouette provides restrained additional depth.
Both decorations ignore layout and remain outside the inner viewport mask.
All shared artwork/materials are borrowed, never changed or destroyed.

The resolver is bounded to one attempt per attachment. Missing/changed
resources produce a bounded diagnostic and the usable prior solid
presentation. Fallback is **not native-aesthetic acceptance**. Cosmetic/audio
failure has no coordinator, RNG, assignment or point-buy recovery authority.

Rejected donors: allocator m_Frame is curved, not paper; its material alone
cannot supply texture. Inventory/Spellbook full books are oversized and do
not establish fresh-launch availability. Message-box constructors own a modal
lifecycle and are not an editor host. No foreign mod is required.

## Evidence boundary

Local ignored evidence: `artifacts/native-ui-reskin/` contains scene metadata,
donor inventories, inspected original sprites, IL records and the first-slice
build. The supplied twelve reference images remain outside Git. These are
resource/source inspections, never candidate game captures.
