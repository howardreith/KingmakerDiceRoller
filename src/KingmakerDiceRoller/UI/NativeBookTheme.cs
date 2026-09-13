using System;
using Kingmaker.UI;
using Kingmaker.UI.LevelUp;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingmakerDiceRoller.UI
{
    // Read-only presentation donors, resolved once per owned allocator attachment.
    // Exact 2.1.7b paths exist in both PC main-menu and in-game UI scenes.
    // No donor GameObject, behaviour, event, material or animation is cloned.
    internal sealed class NativeBookTheme
    {
        internal const string PaperPath =
            "Body/Content/ClothColorSelector/PrimarySelectorPlace/ColorSelector/Background";
        internal const string ActionPath = "Body/Bottom (1)/ButtonsPlace/BackButton";
        internal const string HeadingPath =
            "Body/Content/SkillsMiddleScoresAllocator/Content/STR Background/Labels/SHORT";

        internal const string BodyPath =
            "Body/Content/Book/Image_Book/Container_SpellsLeft/Spells_Container/SpellBookItem (2)/Item/Body/NamePlace/LabelName";
        internal const string SelectorPath =
            "Body/Content/RaceRightSide/Head/SequentialSelector/SequentialSelector/GameObject/Frame/Label";
        internal const string InputPath = "Body/Content/CharacterMiddleSide/CharacterName/PointsBox/Bg";
        internal const string ScrollPath =
            "Body/Content/SkillsLeftSide/MartiaAndSaves/SpellTable/DescriptionView/Scrollbar Vertical";
        internal const string RulePath = "Body/Content/RaceRightSide/Constitution/DescriptionView/Decor (1)";

        internal readonly Image Paper;
        internal readonly Button Action;
        internal readonly TextMeshProUGUI Heading;
        internal readonly TextMeshProUGUI ButtonLabel;

        internal readonly TextMeshProUGUI Body;
        internal readonly TextMeshProUGUI Selector;
        internal readonly Image InputBackground;
        internal readonly Image InputFrame;
        internal readonly TMP_InputField Input;
        internal readonly Scrollbar Scroll;
        internal readonly Image ScrollTrack;
        internal readonly Image ScrollHandle;
        internal readonly Image Rule;

        private NativeBookTheme(Transform owner, Image paper, Button action, TextMeshProUGUI heading, TextMeshProUGUI buttonLabel)
        {
            Paper = paper;
            Action = action;
            Heading = heading;
            ButtonLabel = buttonLabel;
            Body = Require<TextMeshProUGUI>(owner, BodyPath);
            Selector = Require<TextMeshProUGUI>(owner, SelectorPath);
            InputBackground = Require<Image>(owner, InputPath);
            InputFrame = Require<Image>(owner, InputPath + "/Frame");
            Input = Require<TMP_InputField>(owner, InputPath + "/InputField");
            Scroll = Require<Scrollbar>(owner, ScrollPath);
            ScrollTrack = Require<Image>(owner, ScrollPath);
            ScrollHandle = Require<Image>(owner, ScrollPath + "/Sliding Area/Handle");
            Rule = Require<Image>(owner, RulePath);
            RequireSprite(InputBackground.sprite, "SlotBackgroundFilled", new Vector4(9, 9, 8, 9), 100f);
            RequireSprite(InputFrame.sprite, "map_cameraframe", new Vector4(7, 7, 7, 7));
            RequireSprite(ScrollHandle.sprite, "ScrollBar_Big_Handler", new Vector4(0, 48, 0, 45));
            RequireSprite(ScrollTrack.sprite, "SliderArea", new Vector4(0, 62, 0, 52));
            RequireSprite(Rule.sprite, "blockscroll_bottom", new Vector4(20, 0, 20, 0));
            if (InputBackground.type != Image.Type.Sliced || InputFrame.type != Image.Type.Sliced ||
                ScrollTrack.type != Image.Type.Sliced || ScrollHandle.type != Image.Type.Sliced ||
                Rule.type != Image.Type.Sliced || Scroll.transition != Selectable.Transition.ColorTint)
                throw new InvalidOperationException("Native input/scroll/ornament presentation contract changed.");
            if (Body.font == null || Body.fontSharedMaterial == null ||
                Selector.font == null || Selector.fontSharedMaterial == null)
                throw new InvalidOperationException("Native body/selector typography is unavailable.");
        }

        internal static NativeBookTheme Resolve(MonoBehaviour allocator)
        {
            if (typeof(CharacterBuildController).Assembly.ManifestModule.ModuleVersionId !=
                new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7"))
                throw new InvalidOperationException("Unqualified native UI assembly.");
            CharacterBuildController owner = allocator.GetComponentInParent<CharacterBuildController>();
            if (owner == null) throw new InvalidOperationException("Native UI owner is unavailable.");
            Image paper = Require<Image>(owner.transform, PaperPath);
            Button action = Require<Button>(owner.transform, ActionPath);
            TextMeshProUGUI heading = Require<TextMeshProUGUI>(owner.transform, HeadingPath);
            TextMeshProUGUI label = Require<TextMeshProUGUI>(action.transform, "Next/Complete text");
            RequireSprite(paper.sprite, "dialogue_backsheet", new Vector4(268, 169, 258, 165));
            RequireSprite((action.targetGraphic as Image)?.sprite, "button_normal", new Vector4(16, 16, 16, 16));
            RequireSprite(action.spriteState.highlightedSprite, "button_hover", new Vector4(16, 16, 16, 16));
            RequireSprite(action.spriteState.pressedSprite, "button_pressed", new Vector4(16, 16, 16, 16));
            RequireSprite(action.spriteState.disabledSprite, "button_disable", new Vector4(16, 16, 16, 16));
            if (paper.type != Image.Type.Sliced || ((Image)action.targetGraphic).type != Image.Type.Sliced || action.transition != Selectable.Transition.SpriteSwap ||
                heading.font == null || heading.fontSharedMaterial == null ||
                label.font == null || label.fontSharedMaterial == null)
                throw new InvalidOperationException("Native paper/button/text presentation contract changed.");
            return new NativeBookTheme(owner.transform, paper, action, heading, label);
        }

        internal static T Require<T>(Transform owner, string path) where T : Component
        {
            Transform child = owner.Find(path);
            T value = child == null ? null : child.GetComponent<T>();
            if (value == null) throw new InvalidOperationException("Missing native UI donor: " + path);
            return value;
        }

        private static void RequireSprite(Sprite sprite, string name, Vector4 border, float pixelsPerUnit = 200f)
        {
            if (sprite == null || sprite.name != name || sprite.texture == null || sprite.border != border ||
                sprite.pixelsPerUnit != pixelsPerUnit)
                throw new InvalidOperationException("Unqualified native UI sprite: " + name);
        }

        internal void ApplyButton(Button button, Image image)
        {
            CopyImage((Image)Action.targetGraphic, image);
            image.raycastTarget = true;
            button.targetGraphic = image;
            button.spriteState = Action.spriteState;
            button.colors = Action.colors;
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
