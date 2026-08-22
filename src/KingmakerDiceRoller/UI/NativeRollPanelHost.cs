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

        private static readonly Color Parchment = new Color(0.91f, 0.84f, 0.69f, 0.98f);
        private static readonly Color BodyText = new Color(0.176f, 0.11f, 0.067f, 1f);
        private static readonly Color HeadingText = new Color(0.31f, 0.09f, 0.055f, 1f);
        private static readonly Color ButtonSurface = new Color(0.34f, 0.12f, 0.075f, 0.98f);
        private static readonly Color ButtonText = new Color(0.98f, 0.91f, 0.74f, 1f);
        private static readonly Color ErrorText = new Color(0.48f, 0.04f, 0.025f, 1f);

        private readonly RollUiCommandRouter commands;
        private readonly RollPanelPresenter presenter;
        private readonly NativeAbilityControlService nativeControls;
        private readonly Func<KingmakerContracts> contractsProvider;
        private readonly IModLogger logger;
        private readonly NativePanelAttachmentLifecycle lifecycle = new NativePanelAttachmentLifecycle();
        private readonly NativeRollPanelState panelState = new NativeRollPanelState();
        private readonly NativeRollPanelLayoutSpec layout = NativeRollPanelLayoutSpec.Default;
        private readonly List<AssignmentWidgets> assignmentRows = new List<AssignmentWidgets>();

        private object attachedAllocator;
        private bool? accessTabAnchoredToRaceBonus;
        private GameObject root;
        private GameObject expandedSurface;
        private GameObject accessTab;
        private GameObject advancedDisclosure;
        private GameObject advancedContent;
        private GameObject minimumSection;
        private GameObject customSection;
        private GameObject assignmentSection;
        private GameObject summarySection;
        private GameObject historyDisclosure;
        private GameObject historyDetails;
        private GameObject savedDisclosure;
        private GameObject savedDetails;
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
        private TextMeshProUGUI advancedLabel;
        private TextMeshProUGUI historyDisclosureLabel;
        private TextMeshProUGUI savedDisclosureLabel;
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
            layout.Validate();
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
                    if (action == NativePanelAttachmentAction.Detach) DestroyAttachedView(contracts);
                    EndOwnerIfSessionEnded();
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
                        DestroyAttachedView(contracts);
                    }
                    EndOwnerIfSessionEnded();
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
            DestroyAttachedView(contracts);
            panelState.EndOwner();
        }

        private void EndOwnerIfSessionEnded()
        {
            if (commands.ActiveSession == null) panelState.EndOwner();
        }

        private void DestroyAttachedView(KingmakerContracts contracts)
        {
            nativeControls.RestoreOwnedStates(contracts);
            attachedAllocator = null;
            accessTabAnchoredToRaceBonus = null;
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
            advancedLabel = null;
            historyDisclosureLabel = null;
            savedDisclosureLabel = null;
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
            expandedSurface = null;
            accessTab = null;
            advancedDisclosure = null;
            advancedContent = null;
            minimumSection = null;
            customSection = null;
            assignmentSection = null;
            summarySection = null;
            historyDisclosure = null;
            historyDetails = null;
            savedDisclosure = null;
            savedDetails = null;
            if (root != null) Object.Destroy(root);
            root = null;
            panelState.DetachView();
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
            RollSession session = commands.ActiveSession;
            if (session == null)
            {
                throw new InvalidOperationException("A native panel cannot attach without an active roll session.");
            }

            bool ownerChanged = panelState.ObserveOwner(session.Controller, session.StableOwner);
            if (!ownerChanged && root != null && ReferenceEquals(attachedAllocator, allocator)) return;

            DestroyAttachedView(contracts);
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
                throw new InvalidOperationException("Native text, material, or button styling could not be resolved.");
            }

            root = NewUiObject(OwnedPanelName, behaviour.gameObject.layer);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.SetParent(behaviour.transform.parent, false);
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            root.transform.SetAsLastSibling();

            CreateExpandedSurface(nativeText, nativeFrame, nativeButton);
            CreateAccessTab(nativeText, nativeButton);
            attachedAllocator = allocator;
            panelState.AttachView();
            ApplySurfaceState();
            PositionAccessTab(allocator, contracts);
            AttachmentCount++;
        }

        private void CreateExpandedSurface(
            TextMeshProUGUI nativeText,
            Image nativeFrame,
            Button nativeButton)
        {
            expandedSurface = NewUiObject("ExpandedSurface", root.layer);
            RectTransform surfaceRect = expandedSurface.GetComponent<RectTransform>();
            surfaceRect.SetParent(root.transform, false);
            surfaceRect.anchorMin = new Vector2(1f, 1f);
            surfaceRect.anchorMax = new Vector2(1f, 1f);
            surfaceRect.pivot = new Vector2(1f, 1f);
            surfaceRect.anchoredPosition = new Vector2(-18f, -18f);
            surfaceRect.sizeDelta = new Vector2(layout.ExpandedWidth, layout.ExpandedHeight);

            Image surfaceImage = expandedSurface.AddComponent<Image>();
            surfaceImage.sprite = null;
            surfaceImage.material = nativeFrame.material;
            surfaceImage.type = Image.Type.Simple;
            surfaceImage.preserveAspect = false;
            surfaceImage.color = new Color(Parchment.r, Parchment.g, Parchment.b, layout.BackgroundOpacity);
            surfaceImage.raycastTarget = true;
            var outline = expandedSurface.AddComponent<Outline>();
            outline.effectColor = new Color(0.19f, 0.08f, 0.035f, 0.9f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            expandedSurface.AddComponent<RectMask2D>();

            var surfaceLayout = expandedSurface.AddComponent<VerticalLayoutGroup>();
            surfaceLayout.padding = new RectOffset(
                layout.InternalPadding,
                layout.InternalPadding,
                12,
                12);
            surfaceLayout.spacing = 7f;
            surfaceLayout.childControlWidth = true;
            surfaceLayout.childForceExpandWidth = true;
            surfaceLayout.childControlHeight = true;
            surfaceLayout.childForceExpandHeight = false;

            GameObject header = CreateHorizontal(expandedSurface.transform, 34f);
            CreateLabel(
                header.transform,
                "Rolled Ability Scores",
                nativeText,
                layout.TitleFontSize,
                TextAlignmentOptions.Left,
                34f,
                -1f,
                HeadingText,
                true);
            CreateButton(
                header.transform,
                "Close",
                nativeText,
                nativeButton,
                76f,
                () =>
                {
                    panelState.Close();
                    ApplySurfaceState();
                });

            Transform content = CreateScrollContent(expandedSurface.transform);
            CreatePanelContent(content, nativeText, nativeButton);
        }

        private Transform CreateScrollContent(Transform parent)
        {
            GameObject scrollObject = NewUiObject("ContentScroll", parent.gameObject.layer);
            scrollObject.transform.SetParent(parent, false);
            var scrollLayout = scrollObject.AddComponent<LayoutElement>();
            scrollLayout.flexibleHeight = 1f;
            scrollLayout.minHeight = 0f;

            GameObject viewportObject = NewUiObject("Viewport", scrollObject.layer);
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            viewport.SetParent(scrollObject.transform, false);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = Vector2.zero;
            Image viewportRaycast = viewportObject.AddComponent<Image>();
            viewportRaycast.sprite = null;
            viewportRaycast.color = new Color(1f, 1f, 1f, 0f);
            viewportRaycast.raycastTarget = true;
            viewportObject.AddComponent<RectMask2D>();

            GameObject contentObject = NewUiObject("Content", scrollObject.layer);
            RectTransform content = contentObject.GetComponent<RectTransform>();
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            var vertical = contentObject.AddComponent<VerticalLayoutGroup>();
            vertical.spacing = 6f;
            vertical.childControlWidth = true;
            vertical.childForceExpandWidth = true;
            vertical.childControlHeight = true;
            vertical.childForceExpandHeight = false;
            var fitter = contentObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 18f;
            scroll.inertia = true;
            scroll.decelerationRate = 0.135f;
            return content;
        }

        private void CreatePanelContent(
            Transform content,
            TextMeshProUGUI nativeText,
            Button nativeButton)
        {
            modeLabel = CreateLabel(
                content,
                string.Empty,
                nativeText,
                layout.SectionFontSize,
                TextAlignmentOptions.Left,
                24f,
                -1f,
                HeadingText,
                true);

            CreateCaptionedSelector(
                content,
                "Roll method",
                nativeText,
                nativeButton,
                RollUiCommand.PreviousPreset,
                RollUiCommand.NextPreset,
                out presetLabel);

            GameObject pointActions = CreateHorizontal(content, 34f);
            rollButton = CreateButton(pointActions.transform, "Roll", nativeText, nativeButton, 120f,
                () => Execute(RollUiCommand.Roll));
            rerollButton = CreateButton(pointActions.transform, "Reroll", nativeText, nativeButton, 120f,
                () => Execute(RollUiCommand.Reroll));
            returnButton = CreateButton(pointActions.transform, "Return to Point Buy", nativeText, nativeButton, 210f,
                () => Execute(RollUiCommand.ReturnToPointBuy));

            Button advancedButton = CreateButton(
                content,
                "Roll Options +",
                nativeText,
                nativeButton,
                -1f,
                () =>
                {
                    panelState.ToggleAdvanced();
                    Render(contractsProvider());
                });
            advancedDisclosure = advancedButton.gameObject;
            advancedDisclosure.name = "AdvancedDisclosure";
            advancedDisclosure.GetComponent<LayoutElement>().preferredHeight = 30f;
            advancedDisclosure.GetComponent<LayoutElement>().minHeight = 30f;
            advancedLabel = advancedButton.GetComponentInChildren<TextMeshProUGUI>();

            advancedContent = CreateVertical("AdvancedContent", content);
            CreateCaptionedSelector(
                advancedContent.transform,
                "Low-score rule",
                nativeText,
                nativeButton,
                RollUiCommand.PreviousPolicy,
                RollUiCommand.NextPolicy,
                out policyLabel);

            minimumSection = CreateVertical("MinimumSection", advancedContent.transform);
            CreateLabel(
                minimumSection.transform,
                "Minimum",
                nativeText,
                layout.BodyFontSize,
                TextAlignmentOptions.Left,
                19f,
                -1f,
                BodyText,
                true);
            GameObject minimumRow = CreateHorizontal(minimumSection.transform, 30f);
            minimumDown = CreateButton(minimumRow.transform, "-", nativeText, nativeButton, 44f,
                () => Execute(RollUiCommand.DecreaseMinimum));
            minimumLabel = CreateLabel(
                minimumRow.transform,
                string.Empty,
                nativeText,
                layout.BodyFontSize,
                TextAlignmentOptions.Center,
                30f,
                -1f,
                BodyText,
                true);
            minimumUp = CreateButton(minimumRow.transform, "+", nativeText, nativeButton, 44f,
                () => Execute(RollUiCommand.IncreaseMinimum));

            customSection = CreateVertical("CustomExpressionSection", advancedContent.transform);
            CreateLabel(
                customSection.transform,
                "Custom expression",
                nativeText,
                layout.BodyFontSize,
                TextAlignmentOptions.Left,
                19f,
                -1f,
                BodyText,
                true);
            customInput = CreateInput(customSection.transform, nativeText, nativeButton);
            customInput.onValueChanged.AddListener(value =>
            {
                if (!rendering) commands.SetCustomExpression(value);
            });
            CreateLabel(
                customSection.transform,
                "Example: 4d[6]kh3",
                nativeText,
                layout.StatusFontSize,
                TextAlignmentOptions.Left,
                19f,
                -1f,
                BodyText,
                true);

            assignmentSection = CreateVertical("AssignmentSection", content);
            CreateLabel(
                assignmentSection.transform,
                "Assign base scores",
                nativeText,
                layout.SectionFontSize,
                TextAlignmentOptions.Left,
                23f,
                -1f,
                HeadingText,
                true);
            CreateAssignmentRows(assignmentSection.transform, nativeText, nativeButton);

            summarySection = CreateVertical("SummarySection", content);
            summaryLabel = CreateLabel(
                summarySection.transform,
                string.Empty,
                nativeText,
                layout.BodyFontSize,
                TextAlignmentOptions.Left,
                63f,
                -1f,
                BodyText,
                false);

            CreateHistorySection(content, nativeText, nativeButton);
            CreateSavedSection(content, nativeText, nativeButton);

            errorLabel = CreateLabel(
                content,
                string.Empty,
                nativeText,
                layout.BodyFontSize,
                TextAlignmentOptions.Left,
                38f,
                -1f,
                ErrorText,
                false);
            statusLabel = CreateLabel(
                content,
                string.Empty,
                nativeText,
                layout.StatusFontSize,
                TextAlignmentOptions.Left,
                24f,
                -1f,
                BodyText,
                false);
        }

        private void CreateCaptionedSelector(
            Transform parent,
            string caption,
            TextMeshProUGUI nativeText,
            Button nativeButton,
            RollUiCommand previous,
            RollUiCommand next,
            out TextMeshProUGUI valueLabel)
        {
            GameObject section = CreateVertical(caption.Replace(" ", string.Empty), parent);
            CreateLabel(
                section.transform,
                caption,
                nativeText,
                layout.BodyFontSize,
                TextAlignmentOptions.Left,
                19f,
                -1f,
                BodyText,
                true);
            GameObject row = CreateHorizontal(section.transform, 32f);
            CreateButton(row.transform, "<", nativeText, nativeButton, 42f, () => Execute(previous));
            valueLabel = CreateLabel(
                row.transform,
                string.Empty,
                nativeText,
                layout.BodyFontSize,
                TextAlignmentOptions.Center,
                32f,
                -1f,
                BodyText,
                true);
            valueLabel.enableAutoSizing = true;
            valueLabel.fontSizeMin = layout.BodyFontSize;
            valueLabel.fontSizeMax = layout.SectionFontSize;
            valueLabel.overflowMode = TextOverflowModes.Ellipsis;
            CreateButton(row.transform, ">", nativeText, nativeButton, 42f, () => Execute(next));
        }

        private void CreateAssignmentRows(
            Transform parent,
            TextMeshProUGUI nativeText,
            Button nativeButton)
        {
            for (int index = 0; index < Abilities.Length; index++)
            {
                AbilityScore ability = Abilities[index];
                GameObject row = CreateHorizontal(parent, 30f);
                TextMeshProUGUI value = CreateLabel(
                    row.transform,
                    string.Empty,
                    nativeText,
                    layout.BodyFontSize,
                    TextAlignmentOptions.Left,
                    30f,
                    layout.AssignmentLabelWidth,
                    BodyText,
                    true);
                Button up = CreateButton(
                    row.transform,
                    "Up",
                    nativeText,
                    nativeButton,
                    layout.AssignmentButtonWidth,
                    () => Execute(RollUiCommand.MoveUp, ability));
                Button down = CreateButton(
                    row.transform,
                    "Down",
                    nativeText,
                    nativeButton,
                    layout.AssignmentButtonWidth,
                    () => Execute(RollUiCommand.MoveDown, ability));
                assignmentRows.Add(new AssignmentWidgets(row, value, up, down));
            }
        }

        private void CreateHistorySection(
            Transform parent,
            TextMeshProUGUI nativeText,
            Button nativeButton)
        {
            Button disclosure = CreateButton(
                parent,
                "History (0) +",
                nativeText,
                nativeButton,
                -1f,
                () =>
                {
                    panelState.ToggleHistory();
                    Render(contractsProvider());
                });
            historyDisclosure = disclosure.gameObject;
            historyDisclosure.name = "HistoryDisclosure";
            historyDisclosure.GetComponent<LayoutElement>().preferredHeight = 30f;
            historyDisclosure.GetComponent<LayoutElement>().minHeight = 30f;
            historyDisclosureLabel = disclosure.GetComponentInChildren<TextMeshProUGUI>();

            historyDetails = CreateVertical("HistoryDetails", parent);
            historyLabel = CreateLabel(
                historyDetails.transform,
                string.Empty,
                nativeText,
                layout.StatusFontSize,
                TextAlignmentOptions.Left,
                25f,
                -1f,
                BodyText,
                true);
            GameObject row = CreateHorizontal(historyDetails.transform, 30f);
            CreateButton(row.transform, "Previous", nativeText, nativeButton, 94f,
                () => Execute(RollUiCommand.PreviousHistory));
            CreateButton(row.transform, "Next", nativeText, nativeButton, 74f,
                () => Execute(RollUiCommand.NextHistory));
            useHistoryButton = CreateButton(row.transform, "Use", nativeText, nativeButton, 68f,
                () => Execute(RollUiCommand.UseHistory));
        }

        private void CreateSavedSection(
            Transform parent,
            TextMeshProUGUI nativeText,
            Button nativeButton)
        {
            Button disclosure = CreateButton(
                parent,
                "Saved (0) +",
                nativeText,
                nativeButton,
                -1f,
                () =>
                {
                    panelState.ToggleSaved();
                    Render(contractsProvider());
                });
            savedDisclosure = disclosure.gameObject;
            savedDisclosure.name = "SavedDisclosure";
            savedDisclosure.GetComponent<LayoutElement>().preferredHeight = 30f;
            savedDisclosure.GetComponent<LayoutElement>().minHeight = 30f;
            savedDisclosureLabel = disclosure.GetComponentInChildren<TextMeshProUGUI>();

            savedDetails = CreateVertical("SavedDetails", parent);
            savedLabel = CreateLabel(
                savedDetails.transform,
                string.Empty,
                nativeText,
                layout.StatusFontSize,
                TextAlignmentOptions.Left,
                25f,
                -1f,
                BodyText,
                true);
            GameObject row = CreateHorizontal(savedDetails.transform, 30f);
            storeButton = CreateButton(row.transform, "Store", nativeText, nativeButton, 68f,
                () => Execute(RollUiCommand.StoreCurrent));
            CreateButton(row.transform, "<", nativeText, nativeButton, 38f,
                () => Execute(RollUiCommand.PreviousSaved));
            CreateButton(row.transform, ">", nativeText, nativeButton, 38f,
                () => Execute(RollUiCommand.NextSaved));
            recallButton = CreateButton(row.transform, "Recall", nativeText, nativeButton, 72f,
                () => Execute(RollUiCommand.RecallSaved));
            deleteButton = CreateButton(row.transform, "Delete", nativeText, nativeButton, 72f,
                () => Execute(RollUiCommand.DeleteSaved));
        }

        private void CreateAccessTab(TextMeshProUGUI nativeText, Button nativeButton)
        {
            Button button = CreateButton(
                root.transform,
                "Roll Stats",
                nativeText,
                nativeButton,
                layout.AccessTabWidth,
                () =>
                {
                    panelState.Open();
                    Render(contractsProvider());
                });
            accessTab = button.gameObject;
            accessTab.name = "CollapsedAccessTab";
            RectTransform rect = accessTab.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(layout.AccessTabWidth, layout.AccessTabHeight);
            LayoutElement element = accessTab.GetComponent<LayoutElement>();
            element.preferredHeight = layout.AccessTabHeight;
            element.minHeight = layout.AccessTabHeight;
        }

        private void PositionAccessTab(object allocator, KingmakerContracts contracts)
        {
            RectTransform tabRect = accessTab == null ? null : accessTab.GetComponent<RectTransform>();
            RectTransform rootRect = root == null ? null : root.GetComponent<RectTransform>();
            GameObject raceBonus = contracts.AbilityAllocatorRaceBonusContainerField.GetValue(allocator) as GameObject;
            RectTransform raceRect = raceBonus == null ? null : raceBonus.GetComponent<RectTransform>();

            if (tabRect != null && rootRect != null && raceRect != null && raceBonus.activeInHierarchy)
            {
                var corners = new Vector3[4];
                raceRect.GetWorldCorners(corners);
                Vector3 worldBottomCenter = (corners[0] + corners[3]) * 0.5f;
                Vector3 local = rootRect.InverseTransformPoint(worldBottomCenter);
                Rect bounds = rootRect.rect;
                float halfWidth = layout.AccessTabWidth * 0.5f;
                float x = Mathf.Clamp(local.x, bounds.xMin + halfWidth + 8f, bounds.xMax - halfWidth - 8f);
                float y = Mathf.Clamp(local.y - 8f, bounds.yMin + layout.AccessTabHeight + 8f, bounds.yMax - 8f);
                tabRect.anchorMin = new Vector2(0.5f, 0.5f);
                tabRect.anchorMax = new Vector2(0.5f, 0.5f);
                tabRect.pivot = new Vector2(0.5f, 1f);
                tabRect.anchoredPosition = new Vector2(x, y);
                ReportAccessAnchor(true);
                return;
            }

            if (tabRect != null)
            {
                tabRect.anchorMin = new Vector2(1f, 1f);
                tabRect.anchorMax = new Vector2(1f, 1f);
                tabRect.pivot = new Vector2(1f, 1f);
                tabRect.anchoredPosition = new Vector2(-18f, -18f);
            }
            ReportAccessAnchor(false);
        }

        private void ReportAccessAnchor(bool raceBonusAnchor)
        {
            if (accessTabAnchoredToRaceBonus.HasValue &&
                accessTabAnchoredToRaceBonus.Value == raceBonusAnchor)
            {
                return;
            }
            accessTabAnchoredToRaceBonus = raceBonusAnchor;
            logger.Info(raceBonusAnchor
                ? "Native Roll Stats access tab anchored beneath the exact Racial Bonus container."
                : "Native Roll Stats access tab used the bounded upper-right fallback anchor.");
        }

        private void Render(KingmakerContracts contracts)
        {
            if (root == null) return;
            RollPanelModel model = presenter.Present(commands.Snapshot, panelState.Disclosure);
            rendering = true;
            try
            {
                modeLabel.text = model.Mode;
                presetLabel.text = model.Preset;
                policyLabel.text = model.Policy;
                minimumLabel.text = model.Minimum;
                minimumDown.interactable = model.MinimumEnabled;
                minimumUp.interactable = model.MinimumEnabled;
                advancedDisclosure.SetActive(model.AdvancedVisible);
                advancedContent.SetActive(model.AdvancedExpanded);
                advancedLabel.text = model.AdvancedLabel;
                minimumSection.SetActive(model.MinimumVisible);
                customSection.SetActive(model.CustomVisible);
                if (customInput.text != model.CustomExpression) customInput.text = model.CustomExpression;

                rollButton.gameObject.SetActive(model.RollVisible);
                rerollButton.gameObject.SetActive(model.RerollVisible);
                returnButton.gameObject.SetActive(model.ReturnToPointBuyVisible);
                rollButton.interactable = model.CanRoll;
                rerollButton.interactable = model.CanReroll;
                returnButton.interactable = model.CanReturnToPointBuy;

                assignmentSection.SetActive(model.AssignmentVisible);
                for (int index = 0; index < assignmentRows.Count; index++)
                {
                    AssignmentWidgets widgets = assignmentRows[index];
                    bool available = model.AssignmentVisible && index < model.AssignmentRows.Count;
                    widgets.Root.SetActive(available);
                    if (!available) continue;
                    RollPanelAssignmentRow row = model.AssignmentRows[index];
                    widgets.Value.text = row.Label + "   " + row.Value;
                    widgets.Up.interactable = row.CanMoveUp;
                    widgets.Down.interactable = row.CanMoveDown;
                }

                summarySection.SetActive(model.SummaryVisible);
                summaryLabel.text = model.Summary;
                historyDisclosure.SetActive(model.HistoryDisclosureVisible);
                historyDisclosureLabel.text = model.HistoryDisclosureLabel;
                historyDetails.SetActive(model.HistoryDetailsVisible);
                historyLabel.text = model.History;
                useHistoryButton.interactable = model.CanUseHistory;

                savedDisclosure.SetActive(model.SavedDisclosureVisible);
                savedDisclosureLabel.text = model.SavedDisclosureLabel;
                savedDetails.SetActive(model.SavedDetailsVisible);
                savedLabel.text = model.Saved;
                storeButton.interactable = model.CanStore;
                recallButton.interactable = model.CanRecall;
                deleteButton.interactable = model.CanDeleteSaved;
                errorLabel.gameObject.SetActive(!string.IsNullOrWhiteSpace(model.Error));
                errorLabel.text = model.Error;
                statusLabel.text = model.Status;
                ApplySurfaceState();
                PositionAccessTab(attachedAllocator, contracts);
            }
            finally
            {
                rendering = false;
            }

            if (commands.Snapshot.Mode == RollSessionMode.Roll)
            {
                string error;
                if (!nativeControls.TrySuppressForRoll(commands.ActiveSession, contracts, out error))
                {
                    logger.Warning("Native Roll Mode control suppression failed: " + error);
                }
            }
        }

        private void ApplySurfaceState()
        {
            if (expandedSurface != null) expandedSurface.SetActive(panelState.ExpandedSurfaceActive);
            if (accessTab != null) accessTab.SetActive(panelState.AccessTabActive);
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

        private static GameObject CreateVertical(string name, Transform parent)
        {
            GameObject section = NewUiObject(name, parent.gameObject.layer);
            section.transform.SetParent(parent, false);
            var layout = section.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4f;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            var fitter = section.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return section;
        }

        private static GameObject CreateHorizontal(Transform parent, float height)
        {
            GameObject row = NewUiObject("Row", parent.gameObject.layer);
            row.transform.SetParent(parent, false);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = NativeRollPanelLayoutSpec.Default.HorizontalSpacing;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = false;
            var element = row.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = height;
            return row;
        }

        private static TextMeshProUGUI CreateLabel(
            Transform parent,
            string text,
            TextMeshProUGUI source,
            float fontSize,
            TextAlignmentOptions alignment,
            float preferredHeight,
            float preferredWidth,
            Color color,
            bool singleLine)
        {
            GameObject gameObject = NewUiObject("Label", parent.gameObject.layer);
            gameObject.transform.SetParent(parent, false);
            var label = gameObject.AddComponent<TextMeshProUGUI>();
            label.font = source.font;
            label.fontSharedMaterial = source.fontSharedMaterial;
            label.color = color;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.enableWordWrapping = !singleLine;
            label.overflowMode = singleLine ? TextOverflowModes.Ellipsis : TextOverflowModes.Truncate;
            label.raycastTarget = false;
            label.text = text;
            var layout = gameObject.AddComponent<LayoutElement>();
            if (preferredWidth > 0f)
            {
                layout.preferredWidth = preferredWidth;
                layout.minWidth = preferredWidth;
            }
            else
            {
                layout.flexibleWidth = 1f;
            }
            if (preferredHeight > 0f)
            {
                layout.preferredHeight = preferredHeight;
                layout.minHeight = preferredHeight;
            }
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
            Image nativeImage = nativeButton.targetGraphic as Image;
            image.sprite = null;
            image.material = nativeImage == null ? null : nativeImage.material;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = ButtonSurface;
            image.raycastTarget = true;

            var button = gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            var colors = new ColorBlock
            {
                normalColor = ButtonSurface,
                highlightedColor = new Color(0.44f, 0.17f, 0.1f, 1f),
                pressedColor = new Color(0.24f, 0.07f, 0.04f, 1f),
                disabledColor = new Color(0.25f, 0.20f, 0.16f, 0.55f),
                colorMultiplier = 1f,
                fadeDuration = 0.1f
            };
            button.colors = colors;
            button.onClick.AddListener(() => action());

            var layout = gameObject.AddComponent<LayoutElement>();
            if (width > 0f)
            {
                layout.preferredWidth = width;
                layout.minWidth = width;
            }
            else
            {
                layout.flexibleWidth = 1f;
            }

            TextMeshProUGUI label = CreateLabel(
                gameObject.transform,
                text,
                nativeText,
                NativeRollPanelLayoutSpec.Default.BodyFontSize,
                TextAlignmentOptions.Center,
                -1f,
                -1f,
                ButtonText,
                true);
            label.enableAutoSizing = true;
            label.fontSizeMin = 12f;
            label.fontSizeMax = NativeRollPanelLayoutSpec.Default.BodyFontSize;
            label.outlineColor = new Color32(18, 10, 6, 255);
            label.outlineWidth = 0.15f;
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(4f, 2f);
            labelRect.offsetMax = new Vector2(-4f, -2f);
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
            Image nativeImage = nativeButton.targetGraphic as Image;
            image.sprite = null;
            image.material = nativeImage == null ? null : nativeImage.material;
            image.type = Image.Type.Simple;
            image.color = new Color(0.97f, 0.93f, 0.82f, 1f);
            image.raycastTarget = true;
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
                NativeRollPanelLayoutSpec.Default.BodyFontSize,
                TextAlignmentOptions.Left,
                -1f,
                -1f,
                BodyText,
                true);
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            TextMeshProUGUI placeholder = CreateLabel(
                viewport,
                "4d[6]kh3",
                nativeText,
                NativeRollPanelLayoutSpec.Default.BodyFontSize,
                TextAlignmentOptions.Left,
                -1f,
                -1f,
                new Color(BodyText.r, BodyText.g, BodyText.b, 0.5f),
                true);
            RectTransform placeholderRect = placeholder.rectTransform;
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = Vector2.zero;
            placeholderRect.offsetMax = Vector2.zero;

            input.textViewport = viewport;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.lineType = TMP_InputField.LineType.SingleLine;
            var layout = gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 32f;
            layout.minHeight = 32f;
            layout.flexibleWidth = 1f;
            return input;
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
