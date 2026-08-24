using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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
        private readonly ResponsiveRollPanelLayoutCalculator layoutCalculator;
        private readonly CollapsedAccessTabLayoutCalculator accessTabLayoutCalculator;
        private readonly List<AssignmentWidgets> assignmentRows = new List<AssignmentWidgets>();

        private object attachedAllocator;
        private CollapsedAccessTabAnchorSource? lastAccessTabAnchorSource;
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
        private GameObject bodyScrollbarObject;
        private bool rendering;
        private RectTransform expandedSurfaceRect;
        private RectTransform bodyViewport;
        private RectTransform bodyContent;
        private ScrollRect bodyScroll;
        private LayoutElement headerLayout;
        private LayoutElement footerLayout;
        private RollPanelPresentationProfile? lastProfile;
        private bool? lastScrolling;
        private ResponsiveRollPanelLayoutResult lastLayoutResult;
        private float lastAvailableWidth = -1f;
        private float lastAvailableHeight = -1f;
        private float lastPreferredBodyHeight = -1f;
        private string lastLayoutModelKey;
        private string lastLayoutDiagnostic;

        private TextMeshProUGUI modeLabel;
        private TextMeshProUGUI presetLabel;
        private TextMeshProUGUI policyLabel;
        private TextMeshProUGUI minimumLabel;
        private TextMeshProUGUI summaryLabel;
        private TextMeshProUGUI historyLabel;
        private TextMeshProUGUI savedLabel;
        private TextMeshProUGUI footerLabel;
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
            layoutCalculator = new ResponsiveRollPanelLayoutCalculator(layout);
            accessTabLayoutCalculator = new CollapsedAccessTabLayoutCalculator();
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
            lastAccessTabAnchorSource = null;
            assignmentRows.Clear();
            modeLabel = null;
            presetLabel = null;
            policyLabel = null;
            minimumLabel = null;
            summaryLabel = null;
            historyLabel = null;
            savedLabel = null;
            footerLabel = null;
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
            bodyScrollbarObject = null;
            expandedSurfaceRect = null;
            bodyViewport = null;
            bodyContent = null;
            bodyScroll = null;
            headerLayout = null;
            footerLayout = null;
            lastProfile = null;
            lastScrolling = null;
            lastLayoutResult = null;
            lastAvailableWidth = -1f;
            lastAvailableHeight = -1f;
            lastPreferredBodyHeight = -1f;
            lastLayoutModelKey = null;
            lastLayoutDiagnostic = null;
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
            expandedSurfaceRect = surfaceRect;
            surfaceRect.SetParent(root.transform, false);
            surfaceRect.anchorMin = new Vector2(1f, 1f);
            surfaceRect.anchorMax = new Vector2(1f, 1f);
            surfaceRect.pivot = new Vector2(1f, 1f);
            surfaceRect.anchoredPosition = new Vector2(-layout.SafeRightInset, -layout.SafeTopInset);
            surfaceRect.sizeDelta = new Vector2(
                layout.PreferredExpandedWidth,
                layout.PreferredExpandedHeight);

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
                (int)layout.SurfaceVerticalPadding,
                (int)layout.SurfaceVerticalPadding);
            surfaceLayout.spacing = layout.MajorVerticalSpacing;
            surfaceLayout.childControlWidth = true;
            surfaceLayout.childForceExpandWidth = true;
            surfaceLayout.childControlHeight = true;
            surfaceLayout.childForceExpandHeight = false;

            GameObject header = CreateHorizontal(expandedSurface.transform, layout.HeaderHeight);
            header.name = "FixedHeader";
            headerLayout = header.GetComponent<LayoutElement>();
            CreateLabel(
                header.transform,
                "Rolled Ability Scores",
                nativeText,
                layout.TitleFontSize,
                TextAlignmentOptions.Left,
                layout.HeaderHeight,
                -1f,
                HeadingText,
                true);
            modeLabel = CreateLabel(
                header.transform,
                string.Empty,
                nativeText,
                layout.SectionFontSize,
                TextAlignmentOptions.Right,
                layout.HeaderHeight,
                132f,
                HeadingText,
                true);
            CreateButton(
                header.transform,
                "Close",
                nativeText,
                nativeButton,
                layout.CloseButtonWidth,
                () =>
                {
                    panelState.Close();
                    ApplySurfaceState();
                },
                layout.CloseButtonHeight);

            Transform content = CreateScrollContent(expandedSurface.transform);
            CreatePanelContent(content, nativeText, nativeButton);

            GameObject footer = NewUiObject("FixedFooter", expandedSurface.layer);
            footer.transform.SetParent(expandedSurface.transform, false);
            footerLayout = footer.AddComponent<LayoutElement>();
            footerLayout.preferredHeight = layout.FooterHeight;
            footerLayout.minHeight = layout.FooterHeight;
            var footerGroup = footer.AddComponent<HorizontalLayoutGroup>();
            footerGroup.childControlWidth = true;
            footerGroup.childForceExpandWidth = true;
            footerGroup.childControlHeight = true;
            footerGroup.childForceExpandHeight = false;
            footerLabel = CreateLabel(
                footer.transform,
                string.Empty,
                nativeText,
                layout.StatusFontSize,
                TextAlignmentOptions.Left,
                layout.FooterHeight,
                -1f,
                BodyText,
                false);
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
            bodyViewport = viewport;
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
            bodyContent = content;
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

            bodyScrollbarObject = CreateVerticalScrollbar(scrollObject.transform);
            var scrollbar = bodyScrollbarObject.GetComponent<Scrollbar>();

            var scroll = scrollObject.AddComponent<ScrollRect>();
            bodyScroll = scroll;
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 18f;
            scroll.inertia = true;
            scroll.decelerationRate = 0.135f;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarSpacing = 2f;
            bodyScrollbarObject.SetActive(false);
            return content;
        }

        private void CreatePanelContent(
            Transform content,
            TextMeshProUGUI nativeText,
            Button nativeButton)
        {
            CreateCaptionedSelector(
                content,
                "Roll method",
                nativeText,
                nativeButton,
                RollUiCommand.PreviousPreset,
                RollUiCommand.NextPreset,
                out presetLabel);

            GameObject pointActions = CreateHorizontal(content, layout.OrdinaryControlHeight);
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

            minimumSection = CreateHorizontal(
                advancedContent.transform,
                layout.OrdinaryControlHeight);
            minimumSection.name = "MinimumSection";
            CreateLabel(
                minimumSection.transform,
                "Minimum",
                nativeText,
                layout.BodyFontSize,
                TextAlignmentOptions.Left,
                layout.OrdinaryControlHeight,
                124f,
                BodyText,
                true);
            minimumDown = CreateButton(minimumSection.transform, "-", nativeText, nativeButton, 44f,
                () => Execute(RollUiCommand.DecreaseMinimum));
            minimumLabel = CreateLabel(
                minimumSection.transform,
                string.Empty,
                nativeText,
                layout.BodyFontSize,
                TextAlignmentOptions.Center,
                layout.OrdinaryControlHeight,
                -1f,
                BodyText,
                true);
            minimumUp = CreateButton(minimumSection.transform, "+", nativeText, nativeButton, 44f,
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
            GameObject section = CreateHorizontal(parent, layout.OrdinaryControlHeight);
            section.name = caption.Replace(" ", string.Empty);
            CreateLabel(
                section.transform,
                caption,
                nativeText,
                layout.BodyFontSize,
                TextAlignmentOptions.Left,
                layout.OrdinaryControlHeight,
                124f,
                BodyText,
                true);
            CreateButton(section.transform, "<", nativeText, nativeButton, 42f, () => Execute(previous));
            valueLabel = CreateLabel(
                section.transform,
                string.Empty,
                nativeText,
                layout.BodyFontSize,
                TextAlignmentOptions.Center,
                layout.OrdinaryControlHeight,
                -1f,
                BodyText,
                true);
            valueLabel.enableAutoSizing = true;
            valueLabel.fontSizeMin = layout.BodyFontSize;
            valueLabel.fontSizeMax = layout.SectionFontSize;
            valueLabel.overflowMode = TextOverflowModes.Ellipsis;
            CreateButton(section.transform, ">", nativeText, nativeButton, 42f, () => Execute(next));
        }

        private void CreateAssignmentRows(
            Transform parent,
            TextMeshProUGUI nativeText,
            Button nativeButton)
        {
            for (int index = 0; index < Abilities.Length; index++)
            {
                AbilityScore ability = Abilities[index];
                GameObject row = CreateHorizontal(parent, layout.AssignmentRowHeight);
                TextMeshProUGUI value = CreateLabel(
                    row.transform,
                    string.Empty,
                    nativeText,
                    layout.BodyFontSize,
                    TextAlignmentOptions.Left,
                    layout.AssignmentRowHeight,
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
                layout.SectionFontSize,
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
                layout.SectionFontSize,
                TextAlignmentOptions.Left,
                25f,
                -1f,
                BodyText,
                true);
            GameObject row = CreateHorizontal(savedDetails.transform, 30f);
            storeButton = CreateButton(row.transform, "Store", nativeText, nativeButton, 68f,
                () => Execute(RollUiCommand.StoreCurrent));
            CreateButton(row.transform, "Previous", nativeText, nativeButton, 94f,
                () => Execute(RollUiCommand.PreviousSaved));
            CreateButton(row.transform, "Next", nativeText, nativeButton, 74f,
                () => Execute(RollUiCommand.NextSaved));
            recallButton = CreateButton(row.transform, "Recall", nativeText, nativeButton, 72f,
                () => Execute(RollUiCommand.RecallSaved));
            deleteButton = CreateButton(row.transform, "Delete", nativeText, nativeButton, 72f,
                () => Execute(RollUiCommand.DeleteSaved));
        }

        private GameObject CreateVerticalScrollbar(Transform parent)
        {
            GameObject scrollbarObject = NewUiObject("VerticalScrollbar", parent.gameObject.layer);
            RectTransform scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
            scrollbarRect.SetParent(parent, false);
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = new Vector2(1f, 1f);
            scrollbarRect.pivot = new Vector2(1f, 0.5f);
            scrollbarRect.anchoredPosition = Vector2.zero;
            scrollbarRect.sizeDelta = new Vector2(layout.ScrollbarWidth, 0f);
            Image track = scrollbarObject.AddComponent<Image>();
            track.sprite = null;
            track.color = new Color(BodyText.r, BodyText.g, BodyText.b, 0.18f);
            track.raycastTarget = true;

            GameObject slidingAreaObject = NewUiObject("SlidingArea", scrollbarObject.layer);
            RectTransform slidingArea = slidingAreaObject.GetComponent<RectTransform>();
            slidingArea.SetParent(scrollbarObject.transform, false);
            slidingArea.anchorMin = Vector2.zero;
            slidingArea.anchorMax = Vector2.one;
            slidingArea.offsetMin = new Vector2(1f, 1f);
            slidingArea.offsetMax = new Vector2(-1f, -1f);

            GameObject handleObject = NewUiObject("Handle", scrollbarObject.layer);
            RectTransform handleRect = handleObject.GetComponent<RectTransform>();
            handleRect.SetParent(slidingArea, false);
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = Vector2.zero;
            handleRect.offsetMax = Vector2.zero;
            Image handle = handleObject.AddComponent<Image>();
            handle.sprite = null;
            handle.color = new Color(ButtonSurface.r, ButtonSurface.g, ButtonSurface.b, 0.9f);
            handle.raycastTarget = true;

            var scrollbar = scrollbarObject.AddComponent<Scrollbar>();
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handle;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.numberOfSteps = 0;
            scrollbar.value = 1f;
            return scrollbarObject;
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
            if (tabRect == null || rootRect == null || allocator == null || contracts == null) return;

            GameObject raceBonus = contracts.AbilityAllocatorRaceBonusContainerField.GetValue(allocator) as GameObject;
            RectTransform raceRect = raceBonus == null ? null : raceBonus.GetComponent<RectTransform>();
            Image allocatorFrame = contracts.AbilityAllocatorFrameField.GetValue(allocator) as Image;
            RectTransform frameRect = allocatorFrame == null ? null : allocatorFrame.rectTransform;
            MonoBehaviour allocatorBehaviour = allocator as MonoBehaviour;
            RectTransform allocatorRect = allocatorBehaviour == null
                ? null
                : allocatorBehaviour.transform as RectTransform;

            LocalLayoutRect? raceBounds = TryGetLocalBounds(raceRect, rootRect);
            LocalLayoutRect? frameBounds = TryGetLocalBounds(frameRect, rootRect);
            LocalLayoutRect? allocatorBounds = TryGetLocalBounds(allocatorRect, rootRect);
            Rect bounds = rootRect.rect;
            var input = new CollapsedAccessTabLayoutInput(
                new LocalLayoutRect(bounds.xMin, bounds.yMin, bounds.xMax, bounds.yMax),
                raceBounds,
                raceBonus != null && raceBonus.activeInHierarchy,
                frameBounds,
                allocatorBounds,
                layout.AccessTabWidth,
                layout.AccessTabHeight,
                layout.SafeLeftInset,
                layout.SafeRightInset,
                layout.SafeTopInset,
                layout.SafeBottomInset,
                layout.AccessTabSafeGap);
            CollapsedAccessTabLayoutResult result = accessTabLayoutCalculator.Calculate(input);

            tabRect.anchorMin = new Vector2(0.5f, 0.5f);
            tabRect.anchorMax = new Vector2(0.5f, 0.5f);
            tabRect.pivot = new Vector2(0.5f, 0.5f);
            tabRect.anchoredPosition = new Vector2(result.CenterX, result.CenterY);
            ReportAccessAnchor(result.Source);
        }

        private static LocalLayoutRect? TryGetLocalBounds(
            RectTransform candidate,
            RectTransform rootRect)
        {
            if (candidate == null || rootRect == null) return null;
            var corners = new Vector3[4];
            candidate.GetWorldCorners(corners);
            Vector3 first = rootRect.InverseTransformPoint(corners[0]);
            float xMin = first.x;
            float xMax = first.x;
            float yMin = first.y;
            float yMax = first.y;
            for (int index = 1; index < corners.Length; index++)
            {
                Vector3 local = rootRect.InverseTransformPoint(corners[index]);
                xMin = Mathf.Min(xMin, local.x);
                xMax = Mathf.Max(xMax, local.x);
                yMin = Mathf.Min(yMin, local.y);
                yMax = Mathf.Max(yMax, local.y);
            }
            var result = new LocalLayoutRect(xMin, yMin, xMax, yMax);
            return result.IsFinitePositive ? result : (LocalLayoutRect?)null;
        }

        private void ReportAccessAnchor(CollapsedAccessTabAnchorSource source)
        {
            if (lastAccessTabAnchorSource.HasValue &&
                lastAccessTabAnchorSource.Value == source) return;
            lastAccessTabAnchorSource = source;
            logger.Info(
                "Native Roll Stats access tab is bottom-centered from verified ability geometry; " +
                "anchorSource=" + source + ".");
        }

        private void Render(KingmakerContracts contracts)
        {
            if (root == null) return;
            RollUiSnapshot snapshot = commands.Snapshot;
            ResponsiveRollPanelLayoutResult preliminaryLayout = CalculateResponsiveLayout(
                snapshot.Mode == RollSessionMode.Roll
                    ? layout.OrdinaryWideRollContentHeight
                    : layout.OrdinaryWidePointBuyContentHeight);
            RollPanelModel model = presenter.Present(
                snapshot,
                panelState.Disclosure,
                preliminaryLayout.Profile);
            rendering = true;
            try
            {
                ApplyModel(model);
                ApplySurfaceState();
                ApplyResponsiveGeometry(preliminaryLayout);
                if (panelState.ExpandedSurfaceActive)
                {
                    RefreshResponsiveLayout(model, preliminaryLayout);
                }
                else
                {
                    lastProfile = preliminaryLayout.Profile;
                    lastLayoutResult = preliminaryLayout;
                }
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

        private void ApplyModel(RollPanelModel model)
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
            historyLabel.text = "History   " + model.History;
            useHistoryButton.interactable = model.CanUseHistory;

            savedDisclosure.SetActive(model.SavedDisclosureVisible);
            savedDisclosureLabel.text = model.SavedDisclosureLabel;
            savedDetails.SetActive(model.SavedDetailsVisible);
            savedLabel.text = "Saved   " + model.Saved;
            storeButton.interactable = model.CanStore;
            recallButton.interactable = model.CanRecall;
            deleteButton.interactable = model.CanDeleteSaved;
            bool hasError = !string.IsNullOrWhiteSpace(model.Error);
            footerLabel.text = hasError ? model.Error : model.Status;
            footerLabel.color = hasError ? ErrorText : BodyText;
        }

        private ResponsiveRollPanelLayoutResult CalculateResponsiveLayout(float preferredBodyHeight)
        {
            RectTransform rootRect = root == null ? null : root.GetComponent<RectTransform>();
            float availableWidth = rootRect == null ? 0f : Mathf.Max(0f, rootRect.rect.width);
            float availableHeight = rootRect == null ? 0f : Mathf.Max(0f, rootRect.rect.height);
            return layoutCalculator.Calculate(new ResponsiveRollPanelLayoutInput(
                availableWidth,
                availableHeight,
                layout.SafeLeftInset,
                layout.SafeTopInset,
                layout.SafeRightInset,
                layout.SafeBottomInset,
                Mathf.Max(0f, preferredBodyHeight),
                lastProfile,
                lastScrolling));
        }

        private void RefreshResponsiveLayout(
            RollPanelModel model,
            ResponsiveRollPanelLayoutResult preliminary)
        {
            RectTransform rootRect = root.GetComponent<RectTransform>();
            float availableWidth = Mathf.Max(0f, rootRect.rect.width);
            float availableHeight = Mathf.Max(0f, rootRect.rect.height);
            string modelKey = BuildLayoutModelKey(model);
            bool meaningfulChange = lastLayoutResult == null ||
                Mathf.Abs(availableWidth - lastAvailableWidth) > 0.5f ||
                Mathf.Abs(availableHeight - lastAvailableHeight) > 0.5f ||
                !string.Equals(modelKey, lastLayoutModelKey, StringComparison.Ordinal) ||
                lastProfile != model.Profile;

            ResponsiveRollPanelLayoutResult resolved = preliminary;
            float preferredBodyHeight = lastPreferredBodyHeight < 0f
                ? (model.AssignmentVisible
                    ? layout.OrdinaryWideRollContentHeight
                    : layout.OrdinaryWidePointBuyContentHeight)
                : lastPreferredBodyHeight;
            if (meaningfulChange)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(expandedSurfaceRect);
                LayoutRebuilder.ForceRebuildLayoutImmediate(bodyContent);
                preferredBodyHeight = Mathf.Max(0f, LayoutUtility.GetPreferredHeight(bodyContent));
                resolved = CalculateResponsiveLayout(preferredBodyHeight);
                bool scrollStateChanged = resolved.ScrollingRequired != preliminary.ScrollingRequired;
                ApplyResponsiveGeometry(resolved);
                if (scrollStateChanged)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(expandedSurfaceRect);
                    LayoutRebuilder.ForceRebuildLayoutImmediate(bodyContent);
                    preferredBodyHeight = Mathf.Max(0f, LayoutUtility.GetPreferredHeight(bodyContent));
                    resolved = CalculateResponsiveLayout(preferredBodyHeight);
                    ApplyResponsiveGeometry(resolved);
                }
                LayoutRebuilder.ForceRebuildLayoutImmediate(expandedSurfaceRect);
            }
            else if (lastLayoutResult != null)
            {
                resolved = lastLayoutResult;
                ApplyResponsiveGeometry(resolved);
            }

            lastAvailableWidth = availableWidth;
            lastAvailableHeight = availableHeight;
            lastPreferredBodyHeight = preferredBodyHeight;
            lastLayoutModelKey = modelKey;
            lastProfile = resolved.Profile;
            lastScrolling = resolved.ScrollingRequired;
            lastLayoutResult = resolved;
            ReportResponsiveLayout(resolved, preferredBodyHeight, availableWidth, availableHeight);
        }

        private void ApplyResponsiveGeometry(ResponsiveRollPanelLayoutResult result)
        {
            if (expandedSurfaceRect != null)
            {
                expandedSurfaceRect.anchoredPosition = new Vector2(
                    result.AnchoredPositionX,
                    result.AnchoredPositionY);
                expandedSurfaceRect.sizeDelta = new Vector2(result.PanelWidth, result.PanelHeight);
            }
            if (headerLayout != null)
            {
                headerLayout.preferredHeight = result.HeaderHeight;
                headerLayout.minHeight = result.HeaderHeight;
            }
            if (footerLayout != null)
            {
                footerLayout.preferredHeight = result.FooterHeight;
                footerLayout.minHeight = result.FooterHeight;
            }
            if (bodyScroll != null)
            {
                bodyScroll.horizontal = false;
                bodyScroll.vertical = result.ScrollingRequired;
                if (!result.ScrollingRequired) bodyScroll.verticalNormalizedPosition = 1f;
            }
            if (bodyScrollbarObject != null)
            {
                bodyScrollbarObject.SetActive(result.ScrollingRequired);
            }
            if (bodyViewport != null)
            {
                bodyViewport.offsetMin = Vector2.zero;
                bodyViewport.offsetMax = new Vector2(
                    result.ScrollingRequired ? -(layout.ScrollbarWidth + 2f) : 0f,
                    0f);
            }
        }

        private static string BuildLayoutModelKey(RollPanelModel model)
        {
            return string.Join("|", new[]
            {
                model.Profile.ToString(),
                model.Mode,
                model.AdvancedVisible.ToString(),
                model.AdvancedExpanded.ToString(),
                model.MinimumVisible.ToString(),
                model.CustomVisible.ToString(),
                model.AssignmentVisible.ToString(),
                model.AssignmentRows.Count.ToString(CultureInfo.InvariantCulture),
                model.SummaryVisible.ToString(),
                model.HistoryDisclosureVisible.ToString(),
                model.HistoryDetailsVisible.ToString(),
                model.SavedDisclosureVisible.ToString(),
                model.SavedDetailsVisible.ToString(),
                (model.CustomExpression ?? string.Empty).Length.ToString(CultureInfo.InvariantCulture),
                (model.History ?? string.Empty).Length.ToString(CultureInfo.InvariantCulture),
                (model.Saved ?? string.Empty).Length.ToString(CultureInfo.InvariantCulture)
            });
        }

        private void ReportResponsiveLayout(
            ResponsiveRollPanelLayoutResult result,
            float preferredBodyHeight,
            float availableWidth,
            float availableHeight)
        {
            Canvas canvas = root == null ? null : root.GetComponentInParent<Canvas>();
            float canvasScale = canvas == null ? 1f : canvas.scaleFactor;
            RectTransform tabRect = accessTab == null ? null : accessTab.GetComponent<RectTransform>();
            Vector2 tabAnchor = tabRect == null ? Vector2.zero : tabRect.anchoredPosition;
            RollSession session = commands.ActiveSession;
            string creationKind = session == null ? "None" : session.CreationKind.ToString();
            string diagnostic = string.Format(
                CultureInfo.InvariantCulture,
                "Native Roll Stats layout: creationKind={0}; profile={1}; available={2:0.0}x{3:0.0}; canvasScale={4:0.###}; panel={5:0.0}x{6:0.0}; bodyViewport={7:0.0}; preferredBody={8:0.0}; scroll={9}; expandedAnchor=({10:0.0},{11:0.0}); accessAnchor=({12:0.0},{13:0.0}).",
                creationKind,
                result.Profile,
                availableWidth,
                availableHeight,
                canvasScale,
                result.PanelWidth,
                result.PanelHeight,
                result.BodyViewportHeight,
                preferredBodyHeight,
                result.ScrollingRequired,
                result.AnchoredPositionX,
                result.AnchoredPositionY,
                tabAnchor.x,
                tabAnchor.y);
            if (string.Equals(diagnostic, lastLayoutDiagnostic, StringComparison.Ordinal)) return;
            lastLayoutDiagnostic = diagnostic;
            logger.Info(diagnostic);
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
            return section;
        }

        private static GameObject CreateHorizontal(Transform parent, float height)
        {
            GameObject row = NewUiObject("Row", parent.gameObject.layer);
            row.transform.SetParent(parent, false);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = NativeRollPanelLayoutSpec.Default.HorizontalSpacing;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
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
            Action action,
            float height = -1f)
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
            float resolvedHeight = height > 0f
                ? height
                : NativeRollPanelLayoutSpec.Default.OrdinaryControlHeight;
            layout.preferredHeight = resolvedHeight;
            layout.minHeight = resolvedHeight;

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
