using System;
using Kingmaker.UI;
using Kingmaker.UI.LevelUp;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingmakerDiceRoller.UI
{
    // The Unity adapter validates borrowed data; the shared resolver owns locator
    // semantics and independent capability transactions. No native events are copied.
    internal sealed class NativeBookTheme
    {
        internal readonly NativeThemeResolution Resources;
        internal Transform Owner => Resources.Owner as Transform;
        internal string OwnerLocation => new NativeUiDonorLookup(new UnitySource()).Location(Owner);
        private NativeBookTheme(NativeThemeResolution resources) { Resources = resources; }

        internal static Transform FindOwner(MonoBehaviour allocator)
        {
            CharacterBuildController owner = allocator == null ? null : allocator.GetComponentInParent<CharacterBuildController>();
            return owner == null ? null : owner.transform;
        }

        internal static NativeBookTheme Resolve(MonoBehaviour allocator)
        {
            if (typeof(CharacterBuildController).Assembly.ManifestModule.ModuleVersionId !=
                new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7"))
                throw new InvalidOperationException("Unqualified native UI assembly.");
            return new NativeBookTheme(NativeThemeResolver.Resolve(FindOwner(allocator), new UnitySource()));
        }

        private sealed class UnitySource : INativeThemeSource
        {
            public bool IsAlive(object value) { return value is UnityEngine.Object && (UnityEngine.Object)value != null; }
            public bool SameNode(object first, object second) { return (UnityEngine.Object)first == (UnityEngine.Object)second; }
            public string Name(object node) { return ((Transform)node).name; }
            public object Parent(object node) { return ((Transform)node).parent; }
            public int ChildCount(object node) { return ((Transform)node).childCount; }
            public object Child(object node, int index) { return ((Transform)node).GetChild(index); }
            public object[] Components(object node, NativeThemeComponent component)
            {
                Type type;
                switch (component)
                {
                    case NativeThemeComponent.Image: type = typeof(Image); break;
                    case NativeThemeComponent.Button: type = typeof(Button); break;
                    case NativeThemeComponent.Text: type = typeof(TextMeshProUGUI); break;
                    case NativeThemeComponent.Input: type = typeof(TMP_InputField); break;
                    case NativeThemeComponent.Scrollbar: type = typeof(Scrollbar); break;
                    default: throw new ArgumentOutOfRangeException(nameof(component));
                }
                return ((Transform)node).GetComponents(type);
            }
            public void Validate(NativeThemeCapability capability, object[] values)
            {
                switch (capability)
                {
                    case NativeThemeCapability.Paper:
                        RequireImage((Image)values[0], "dialogue_backsheet", new Vector4(268, 169, 258, 165));
                        break;
                    case NativeThemeCapability.Buttons:
                        var action = (Button)values[0];
                        Image image = action.targetGraphic as Image;
                        RequireImage(image, "button_normal", new Vector4(16, 16, 16, 16));
                        RequireSprite(action.spriteState.highlightedSprite, "button_hover", new Vector4(16, 16, 16, 16));
                        RequireSprite(action.spriteState.pressedSprite, "button_pressed", new Vector4(16, 16, 16, 16));
                        RequireSprite(action.spriteState.disabledSprite, "button_disable", new Vector4(16, 16, 16, 16));
                        if (action.transition != Selectable.Transition.SpriteSwap || image.transform != action.transform)
                            throw new InvalidOperationException("Native action artwork/transition contract changed.");
                        break;
                    case NativeThemeCapability.Heading:
                    case NativeThemeCapability.Body:
                    case NativeThemeCapability.ButtonText:
                    case NativeThemeCapability.Selector:
                        var text = (TextMeshProUGUI)values[0];
                        if (text.font == null || text.fontSharedMaterial == null)
                            throw new InvalidOperationException("Native " + capability + " font/shared material unavailable.");
                        break;
                    case NativeThemeCapability.Input:
                        RequireImage((Image)values[0], "SlotBackgroundFilled", new Vector4(9, 9, 8, 9), 100f);
                        RequireImage((Image)values[1], "map_cameraframe", new Vector4(7, 7, 7, 7));
                        break;
                    case NativeThemeCapability.Scrollbar:
                        RequireImage((Image)values[0], "SliderArea", new Vector4(0, 62, 0, 52));
                        RequireImage((Image)values[1], "ScrollBar_Big_Handler", new Vector4(0, 48, 0, 45));
                        if (((Scrollbar)values[2]).transition != Selectable.Transition.ColorTint)
                            throw new InvalidOperationException("Native scrollbar transition contract changed.");
                        break;
                    case NativeThemeCapability.Ornament:
                        RequireImage((Image)values[0], "blockscroll_bottom", new Vector4(20, 0, 20, 0));
                        break;
                    default: throw new ArgumentOutOfRangeException(nameof(capability));
                }
            }
        }

        private static void RequireImage(Image image, string name, Vector4 border, float pixelsPerUnit = 200f)
        {
            if (image == null || image.type != Image.Type.Sliced)
                throw new InvalidOperationException("Native sliced Image unavailable: " + name);
            RequireSprite(image.sprite, name, border, pixelsPerUnit);
        }
        private static void RequireSprite(Sprite sprite, string name, Vector4 border, float pixelsPerUnit = 200f)
        {
            if (sprite == null || !string.Equals(sprite.name, name, StringComparison.Ordinal) || sprite.texture == null ||
                sprite.border != border || sprite.pixelsPerUnit != pixelsPerUnit)
                throw new InvalidOperationException("Unqualified native UI sprite: " + name);
        }

        internal static void ApplyButton(Button source, Button button, Image image)
        {
            CopyImage((Image)source.targetGraphic, image);
            image.raycastTarget = true;
            button.targetGraphic = image;
            button.spriteState = source.spriteState;
            button.colors = source.colors;
            button.transition = Selectable.Transition.SpriteSwap;
        }

        internal static void CopyImage(Image source, Image target)
        {
            target.sprite = source.sprite;
            target.material = source.material;
            target.type = source.type;
            target.color = source.color;
            target.fillCenter = source.fillCenter;
            target.preserveAspect = source.preserveAspect;
            target.raycastTarget = false;
        }

        internal static void CopyText(TextMeshProUGUI source, TextMeshProUGUI target)
        {
            target.font = source.font;
            target.fontSharedMaterial = source.fontSharedMaterial;
            target.fontStyle = source.fontStyle;
            target.characterSpacing = source.characterSpacing;
            target.wordSpacing = source.wordSpacing;
            target.lineSpacing = source.lineSpacing;
            target.color = source.color;
            target.richText = false;
            target.raycastTarget = false;
        }

        internal static void PlayClick()
        {
            // This is ButtonPF's ordinary UI route. Using the new Button.onClick
            // also covers Submit without ButtonPF's right-click/disabled cues.
            UICommon common = Kingmaker.Game.Instance?.UI?.Common;
            if (common == null || common.UISound == null)
                throw new InvalidOperationException("Native UI click sound is unavailable.");
            common.UISound.Play(UISoundType.ButtonClick);
        }
    }
}
