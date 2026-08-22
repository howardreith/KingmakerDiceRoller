using System;
using System.Collections;
using System.Collections.Generic;
using KingmakerDiceRoller.CharacterCreation;
using KingmakerDiceRoller.Domain;
using KingmakerDiceRoller.Integration;
using KingmakerDiceRoller.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace KingmakerDiceRoller.UI
{
    public sealed class NativeRollPanelHost
    {
        public const string OwnedPanelName = "KingmakerDiceRoller.NativeRollPanel";
        private static readonly AbilityScore[] Abilities =
        {
            AbilityScore.Strength,
            AbilityScore.Dexterity,
            AbilityScore.Constitution,
            AbilityScore.Intelligence,
            AbilityScore.Wisdom,
            AbilityScore.Charisma
        };

        private readonly RollUiCommandRouter commands;
        private readonly RollPanelPresenter presenter;
        private readonly NativeAbilityControlService nativeControls;
        private readonly Func<KingmakerContracts> contractsProvider;
        private readonly IModLogger logger;
        private readonly NativePanelAttachmentLifecycle lifecycle = new NativePanelAttachmentLifecycle();
        private readonly List<AssignmentWidgets> assignmentRows = new List<AssignmentWidgets>();
        private object attachedAllocator;
        private GameObject root;
        private GameObject body;
        private bool collapsed;
        private bool rendering;
        private TextMeshProUGUI modeLabel;
        private TextMeshProUGUI presetLabel;
        private TextMeshProUGUI policyLabel;
        private TextMeshProUGUI minimumLabel;
        private TextMeshProUGUI summaryLabel;
        private TextMeshProUGUI historyLabel;
        private TextMeshProUGUI savedLabel;
        private TextMeshProUGUI errorLabel;
        private TextMeshProUGUI statusLabel;
        private GameObject minimumRow;
        private GameObject customRow;
        private TMP_InputField customInput;
        private Button minimumDown;
        private Button minimumUp;
        private Button rollButton;
        private Button rerollButton;
        private Button returnButton;
        private Button useHistoryButton;
        private Button storeButton;
        private Button recallButton;
        private Button deleteButton;
        private Button collapseButton;
        private TextMeshProUGUI collapseButtonLabel;

        public NativeRollPanelHost(
            RollUiCommandRouter commands,
            RollPanelPresenter presenter,
            NativeAbilityControlService nativeControls,
            Func<KingmakerContracts> contractsProvider,
            IModLogger logger)
        {
            this.commands = commands ?? throw new ArgumentNullException(nameof(commands));
            this.presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            this.nativeControls = nativeControls ?? throw new ArgumentNullException(nameof(nativeControls));
            this.contractsProvider = contractsProvider ?? throw new ArgumentNullException(nameof(contractsProvider));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool IsAttached => root != null;
        public int AttachmentCount { get; private set; }

        public void OnAbilityAllocatorFilled(object allocator)
        {
            try
            {
                KingmakerContracts contracts = contractsProvider();
                bool eligible = contracts != null && IsEligibleAllocator(allocator, contracts);
                NativePanelAttachmentAction action = lifecycle.Observe(eligible, allocator);
                if (!eligible)
                {
                    if (action == NativePanelAttachmentAction.Detach) DetachCore(contracts);
                    return;
                }
                EnsureAttached(allocator, contracts);
                Render(contracts);
            }
            catch (Exception exception)
            {
                logger.Exception("Attach or refresh native Dice Roller panel", exception);
                Detach(contractsProvider());
            }
        }

        public void Update()
        {
            KingmakerContracts contracts = contractsProvider();
            try
            {
                object characterBuild;
                bool active;
                object phase;
                object allocator;
                if (contracts == null ||
                    !contracts.TryGetAbilityPhasePresentationContext(
                        out characterBuild,
                        out active,
                        out phase,
                        out allocator) ||
                    !active || allocator == null || !commands.CanAttachNativePanel)
                {
                    if (lifecycle.Observe(false, null) == NativePanelAttachmentAction.Detach)
                    {
                        DetachCore(contracts);
                    }
                    return;
                }
                lifecycle.Observe(true, allocator);
                EnsureAttached(allocator, contracts);
                Render(contracts);
            }
            catch (Exception exception)
            {
                logger.Exception("Observe native Dice Roller panel lifecycle", exception);
                Detach(contracts);
            }
        }

        public void Detach(KingmakerContracts contracts)
        {
            lifecycle.Reset();
            DetachCore(contracts);
        }

        private void DetachCore(KingmakerContracts contracts)
        {
            nativeControls.RestoreOwnedStates(contracts);
            attachedAllocator = null;
            assignmentRows.Clear();
            modeLabel = null;
            presetLabel = null;
            policyLabel = null;
            minimumLabel = null;
            summaryLabel = null;
            historyLabel = null;
            savedLabel = null;
            errorLabel = null;
            statusLabel = null;
            minimumRow = null;
            customRow = null;
            customInput = null;
            minimumDown = null;
            minimumUp = null;
            rollButton = null;
            rerollButton = null;
            returnButton = null;
            useHistoryButton = null;
            storeButton = null;
            recallButton = null;
            deleteButton = null;
            collapseButton = null;
            collapseButtonLabel = null;
            body = null;
            if (root != null) Object.Destroy(root);
            root = null;
        }

        private bool IsEligibleAllocator(object allocator, KingmakerContracts contracts)
        {
            if (allocator == null || !commands.CanAttachNativePanel) return false;
            object characterBuild;
            bool active;
            object phase;
            object currentAllocator;
            return contracts.TryGetAbilityPhasePresentationContext(
                    out characterBuild,
                    out active,
                    out phase,
                    out currentAllocator) &&
                active &&
                ReferenceEquals(allocator, currentAllocator);
        }

        private void EnsureAttached(object allocator, KingmakerContracts contracts)
        {
            if (root != null && ReferenceEquals(attachedAllocator, allocator)) return;
            DetachCore(contracts);
            var behaviour = allocator as MonoBehaviour;
            if (behaviour == null)
            {
                throw new InvalidOperationException("The exact native ability allocator is not a MonoBehaviour.");
            }

            TextMeshProUGUI nativeText = contracts.AbilityAllocatorMainLabelField.GetValue(allocator) as TextMeshProUGUI;
            Image nativeFrame = contracts.AbilityAllocatorFrameField.GetValue(allocator) as Image;
            Button nativeButton = ResolveNativeButton(allocator, contracts);
            if (nativeText == null || nativeFrame == null || nativeButton == null)
            {
                throw new InvalidOperationException("Native text, frame, or button styling could not be resolved.");
            }

            root = NewUiObject(OwnedPanelName, behaviour.gameObject.layer);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.SetParent(behaviour.transform.parent, false);
            rootRect.anchorMin = new Vector2(1f, 1f);
            rootRect.anchorMax = new Vector2(1f, 1f);
            rootRect.pivot = new Vector2(1f, 1f);
            rootRect.anchoredPosition = new Vector2(-18f, -18f);
            rootRect.sizeDelta = new Vector2(470f, 650f);

            Image background = root.AddComponent<Image>();
            CopyImage(nativeFrame, background, new Color(1f, 1f, 1f, 0.96f));
            var vertical = root.AddComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(14, 14, 10, 12);
            vertical.spacing = 4f;
            vertical.childControlWidth = true;
            vertical.childForceExpandWidth = true;
            vertical.childControlHeight = true;
            vertical.childForceExpandHeight = false;

            GameObject titleRow = CreateHorizontal(root.transform, 30f);
            CreateLabel(titleRow.transform, "Rolled Ability Scores", nativeText, 20f, TextAlignmentOptions.Left);
            collapseButton = CreateButton(titleRow.transform, "Hide", nativeText, nativeButton, 74f, () =>
            {
                collapsed = !collapsed;
                if (body != null) body.SetActive(!collapsed);
                if (collapseButtonLabel != null) collapseButtonLabel.text = collapsed ? "Show" : "Hide";
            });
            collapseButtonLabel = collapseButton.GetComponentInChildren<TextMeshProUGUI>();

            body = NewUiObject("Body", root.layer);
            body.transform.SetParent(root.transform, false);
            var bodyLayout = body.AddComponent<VerticalLayoutGroup>();
            bodyLayout.spacing = 3f;
            bodyLayout.childControlWidth = true;
            bodyLayout.childForceExpandWidth = true;
            bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandHeight = false;
            body.AddComponent<LayoutElement>().flexibleHeight = 1f;

            modeLabel = CreateLabel(body.transform, string.Empty, nativeText, 16f, TextAlignmentOptions.Left, 22f);
            CreateConfigurationRows(nativeText, nativeButton);
            CreateActionRows(nativeText, nativeButton);
            CreateAssignmentRows(nativeText, nativeButton);
            summaryLabel = CreateLabel(body.transform, string.Empty, nativeText, 15f, TextAlignmentOptions.Left, 38f);
            CreateHistoryRows(nativeText, nativeButton);
            CreateSavedRows(nativeText, nativeButton);
            errorLabel = CreateLabel(body.transform, string.Empty, nativeText, 14f, TextAlignmentOptions.Left, 22f);
            errorLabel.color = new Color(1f, 0.45f, 0.35f, 1f);
            statusLabel = CreateLabel(body.transform, string.Empty, nativeText, 13f, TextAlignmentOptions.Left, 28f);

            attachedAllocator = allocator;
            AttachmentCount++;
        }

        private void CreateConfigurationRows(TextMeshProUGUI nativeText, Button nativeButton)
        {
            GameObject presetRow = CreateHorizontal(body.transform, 28f);
            CreateButton(presetRow.transform, "<", nativeText, nativeButton, 38f,
                () => Execute(RollUiCommand.PreviousPreset));
            presetLabel = CreateLabel(presetRow.transform, string.Empty, nativeText, 16f, TextAlignmentOptions.Center);
            CreateButton(presetRow.transform, ">", nativeText, nativeButton, 38f,
                () => Execute(RollUiCommand.NextPreset));

            GameObject policyRow = CreateHorizontal(body.transform, 28f);
            CreateButton(policyRow.transform, "<", nativeText, nativeButton, 38f,
                () => Execute(RollUiCommand.PreviousPolicy));
            policyLabel = CreateLabel(policyRow.transform, string.Empty, nativeText, 15f, TextAlignmentOptions.Center);
            CreateButton(policyRow.transform, ">", nativeText, nativeButton, 38f,
                () => Execute(RollUiCommand.NextPolicy));

            minimumRow = CreateHorizontal(body.transform, 28f);
            minimumDown = CreateButton(minimumRow.transform, "-", nativeText, nativeButton, 38f,
                () => Execute(RollUiCommand.DecreaseMinimum));
            minimumLabel = CreateLabel(minimumRow.transform, string.Empty, nativeText, 16f, TextAlignmentOptions.Center);
            minimumUp = CreateButton(minimumRow.transform, "+", nativeText, nativeButton, 38f,
                () => Execute(RollUiCommand.IncreaseMinimum));

            customRow = CreateHorizontal(body.transform, 48f);
            CreateLabel(customRow.transform, "Custom\n4d[6]kh3", nativeText, 13f, TextAlignmentOptions.Left, 95f);
            customInput = CreateInput(customRow.transform, nativeText, nativeButton);
            customInput.onValueChanged.AddListener(value =>
            {
                if (!rendering) commands.SetCustomExpression(value);
            });
        }

        private void CreateActionRows(TextMeshProUGUI nativeText, Button nativeButton)
        {
            GameObject actions = CreateHorizontal(body.transform, 32f);
            rollButton = CreateButton(actions.transform, "Roll", nativeText, nativeButton, 105f,
                () => Execute(RollUiCommand.Roll));
            rerollButton = CreateButton(actions.transform, "Reroll", nativeText, nativeButton, 105f,
                () => Execute(RollUiCommand.Reroll));
            returnButton = CreateButton(actions.transform, "Point Buy", nativeText, nativeButton, 145f,
                () => Execute(RollUiCommand.ReturnToPointBuy));
        }

        private void CreateAssignmentRows(TextMeshProUGUI nativeText, Button nativeButton)
        {
            for (int index = 0; index < Abilities.Length; index++)
            {
                AbilityScore ability = Abilities[index];
                GameObject row = CreateHorizontal(body.transform, 25f);
                TextMeshProUGUI value = CreateLabel(row.transform, string.Empty, nativeText, 16f, TextAlignmentOptions.Left);
                Button up = CreateButton(row.transform, "Up", nativeText, nativeButton, 55f,
                    () => Execute(RollUiCommand.MoveUp, ability));
                Button down = CreateButton(row.transform, "Down", nativeText, nativeButton, 65f,
                    () => Execute(RollUiCommand.MoveDown, ability));
                assignmentRows.Add(new AssignmentWidgets(row, value, up, down));
            }
        }

        private void CreateHistoryRows(TextMeshProUGUI nativeText, Button nativeButton)
        {
            historyLabel = CreateLabel(body.transform, string.Empty, nativeText, 13f, TextAlignmentOptions.Left, 20f);
            GameObject row = CreateHorizontal(body.transform, 27f);
            CreateButton(row.transform, "Previous", nativeText, nativeButton, 95f,
                () => Execute(RollUiCommand.PreviousHistory));
            CreateButton(row.transform, "Next", nativeText, nativeButton, 75f,
                () => Execute(RollUiCommand.NextHistory));
            useHistoryButton = CreateButton(row.transform, "Use", nativeText, nativeButton, 75f,
                () => Execute(RollUiCommand.UseHistory));
        }

        private void CreateSavedRows(TextMeshProUGUI nativeText, Button nativeButton)
        {
            savedLabel = CreateLabel(body.transform, string.Empty, nativeText, 13f, TextAlignmentOptions.Left, 20f);
            GameObject row = CreateHorizontal(body.transform, 27f);
            storeButton = CreateButton(row.transform, "Store", nativeText, nativeButton, 75f,
                () => Execute(RollUiCommand.StoreCurrent));
            CreateButton(row.transform, "<", nativeText, nativeButton, 38f,
                () => Execute(RollUiCommand.PreviousSaved));
            CreateButton(row.transform, ">", nativeText, nativeButton, 38f,
                () => Execute(RollUiCommand.NextSaved));
            recallButton = CreateButton(row.transform, "Recall", nativeText, nativeButton, 78f,
                () => Execute(RollUiCommand.RecallSaved));
            deleteButton = CreateButton(row.transform, "Delete", nativeText, nativeButton, 78f,
                () => Execute(RollUiCommand.DeleteSaved));
        }

        private void Render(KingmakerContracts contracts)
        {
            if (root == null) return;
            RollPanelModel model = presenter.Present(commands.Snapshot);
            rendering = true;
            try
            {
                modeLabel.text = model.Mode;
                presetLabel.text = model.Preset;
                policyLabel.text = model.Policy;
                minimumLabel.text = model.Minimum;
                minimumDown.interactable = model.MinimumEnabled;
                minimumUp.interactable = model.MinimumEnabled;
                minimumRow.SetActive(true);
                customRow.SetActive(model.CustomVisible);
                if (customInput.text != model.CustomExpression) customInput.text = model.CustomExpression;
                rollButton.interactable = model.CanRoll;
                rerollButton.interactable = model.CanReroll;
                returnButton.interactable = model.CanReturnToPointBuy;
                for (int index = 0; index < assignmentRows.Count; index++)
                {
                    AssignmentWidgets widgets = assignmentRows[index];
                    bool available = index < model.AssignmentRows.Count;
                    widgets.Root.SetActive(available);
                    if (!available) continue;
                    RollPanelAssignmentRow row = model.AssignmentRows[index];
                    widgets.Value.text = row.Label + "   " + row.Value;
                    widgets.Up.interactable = row.CanMoveUp;
                    widgets.Down.interactable = row.CanMoveDown;
                }
                summaryLabel.text = model.Summary;
                historyLabel.text = model.History;
                useHistoryButton.interactable = model.CanUseHistory;
                savedLabel.text = model.Saved;
                storeButton.interactable = model.CanStore;
                recallButton.interactable = model.CanRecall;
                deleteButton.interactable = commands.Snapshot.SavedCount > 0;
                errorLabel.text = model.Error;
                statusLabel.text = model.Status;
                body.SetActive(!collapsed);
            }
            finally
            {
                rendering = false;
            }

            if (commands.Snapshot.Mode == RollSessionMode.Roll)
            {
                string error;
                if (!nativeControls.TrySuppressForRoll(
                    commands.ActiveSession,
                    contracts,
                    out error))
                {
                    logger.Warning("Native Roll Mode control suppression failed: " + error);
                }
            }
        }

        private void Execute(RollUiCommand command, AbilityScore ability = AbilityScore.Strength)
        {
            string error;
            if (!commands.Execute(command, ability, out error) && !string.IsNullOrWhiteSpace(error))
            {
                logger.Warning("Native Dice Roller command failed: " + error);
            }
            Render(contractsProvider());
        }

        private static Button ResolveNativeButton(object allocator, KingmakerContracts contracts)
        {
            IList entries = contracts.AbilityAllocatorStatEntriesField.GetValue(allocator) as IList;
            if (entries == null || entries.Count == 0 || entries[0] == null) return null;
            return contracts.ScoreEntryUpButtonField.GetValue(entries[0]) as Button;
        }

        private static GameObject NewUiObject(string name, int layer)
        {
            var value = new GameObject(name, typeof(RectTransform));
            value.layer = layer;
            return value;
        }

        private static GameObject CreateHorizontal(Transform parent, float height)
        {
            GameObject row = NewUiObject("Row", parent.gameObject.layer);
            row.transform.SetParent(parent, false);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 5f;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = false;
            row.AddComponent<LayoutElement>().preferredHeight = height;
            return row;
        }

        private static TextMeshProUGUI CreateLabel(
            Transform parent,
            string text,
            TextMeshProUGUI source,
            float fontSize,
            TextAlignmentOptions alignment,
            float preferredHeight = -1f)
        {
            GameObject gameObject = NewUiObject("Label", parent.gameObject.layer);
            gameObject.transform.SetParent(parent, false);
            var label = gameObject.AddComponent<TextMeshProUGUI>();
            label.font = source.font;
            label.fontSharedMaterial = source.fontSharedMaterial;
            label.color = source.color;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.enableWordWrapping = true;
            label.text = text;
            var layout = gameObject.AddComponent<LayoutElement>();
            layout.flexibleWidth = 1f;
            if (preferredHeight > 0f) layout.preferredHeight = preferredHeight;
            return label;
        }

        private static Button CreateButton(
            Transform parent,
            string text,
            TextMeshProUGUI nativeText,
            Button nativeButton,
            float width,
            Action action)
        {
            GameObject gameObject = NewUiObject("Button." + text, parent.gameObject.layer);
            gameObject.transform.SetParent(parent, false);
            Image image = gameObject.AddComponent<Image>();
            Image sourceImage = nativeButton.targetGraphic as Image;
            if (sourceImage != null) CopyImage(sourceImage, image, sourceImage.color);
            var button = gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = nativeButton.transition;
            button.colors = nativeButton.colors;
            button.spriteState = nativeButton.spriteState;
            button.onClick.AddListener(() => action());
            var layout = gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.minWidth = width;
            TextMeshProUGUI label = CreateLabel(
                gameObject.transform,
                text,
                nativeText,
                14f,
                TextAlignmentOptions.Center);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            return button;
        }

        private static TMP_InputField CreateInput(
            Transform parent,
            TextMeshProUGUI nativeText,
            Button nativeButton)
        {
            GameObject gameObject = NewUiObject("CustomExpression", parent.gameObject.layer);
            gameObject.transform.SetParent(parent, false);
            Image image = gameObject.AddComponent<Image>();
            Image sourceImage = nativeButton.targetGraphic as Image;
            if (sourceImage != null) CopyImage(sourceImage, image, new Color(1f, 1f, 1f, 0.75f));
            var input = gameObject.AddComponent<TMP_InputField>();
            input.targetGraphic = image;

            GameObject viewportObject = NewUiObject("Viewport", gameObject.layer);
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            viewport.SetParent(gameObject.transform, false);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(8f, 3f);
            viewport.offsetMax = new Vector2(-8f, -3f);
            viewportObject.AddComponent<RectMask2D>();

            TextMeshProUGUI text = CreateLabel(
                viewport,
                string.Empty,
                nativeText,
                15f,
                TextAlignmentOptions.Left);
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            TextMeshProUGUI placeholder = CreateLabel(
                viewport,
                "4d[6]kh3",
                nativeText,
                15f,
                TextAlignmentOptions.Left);
            placeholder.color = new Color(nativeText.color.r, nativeText.color.g, nativeText.color.b, 0.45f);
            RectTransform placeholderRect = placeholder.rectTransform;
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = Vector2.zero;
            placeholderRect.offsetMax = Vector2.zero;

            input.textViewport = viewport;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.lineType = TMP_InputField.LineType.SingleLine;
            gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            return input;
        }

        private static void CopyImage(Image source, Image target, Color color)
        {
            target.sprite = source.sprite;
            target.material = source.material;
            target.type = source.type;
            target.preserveAspect = source.preserveAspect;
            target.color = color;
        }

        private sealed class AssignmentWidgets
        {
            internal AssignmentWidgets(GameObject root, TextMeshProUGUI value, Button up, Button down)
            {
                Root = root;
                Value = value;
                Up = up;
                Down = down;
            }

            internal GameObject Root { get; }
            internal TextMeshProUGUI Value { get; }
            internal Button Up { get; }
            internal Button Down { get; }
        }

    }
}
