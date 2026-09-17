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
        private static readonly Color BodyText = new Color(0.157f, 0.067f, 0.035f, 1f);
        private static readonly Color HeadingText = new Color(0.588f, 0.243f, 0.106f, 1f);
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

        private NativeBookTheme theme;
        private Transform attachedThemeOwner;
        private readonly NativeThemeRecovery themeRecovery = new NativeThemeRecovery();
        private readonly NativePanelConstructionBudget constructionBudget = new NativePanelConstructionBudget();
        private readonly NativeThemeBindings themeBindings = new NativeThemeBindings();
        private readonly Dictionary<NativeThemeCapability, string> themeDiagnostics = new Dictionary<NativeThemeCapability, string>();
        private readonly List<RectTransform> paperLayers = new List<RectTransform>();
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
        private RollPanelPresentationProfile? lastProfile;
        private bool? lastScrolling;
        private ResponsiveRollPanelLayoutResult lastLayoutResult;
        private float lastAvailableWidth = -1f;
        private float lastAvailableHeight = -1f;
        private float lastPreferredBodyHeight = -1f;
        private string lastLayoutModelKey;
        private string lastLayoutDiagnostic;

        private string commandError;
        private Button closeButton;
        private TextMeshProUGUI modeLabel;
        private TextMeshProUGUI presetLabel;
        private TextMeshProUGUI policyLabel;
        private TextMeshProUGUI minimumLabel;
        private TextMeshProUGUI summaryLabel;
        private TextMeshProUGUI historyLabel;
        private TextMeshProUGUI savedLabel;
        private TextMeshProUGUI messageLabel;
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

        private readonly System.Collections.Generic.HashSet<string> attachmentDiagnostics =
            new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);

        private void ReportAttachment(string message)
        {
            if (attachmentDiagnostics.Count < 16 && attachmentDiagnostics.Add(message)) logger.Warning(message);
        }

        public bool IsAttached => root != null;
        public int AttachmentCount { get; private set; }

        public void OnAbilityAllocatorFilled(object allocator)
        {
            KingmakerContracts contracts = null;
            try
            {
                contracts = contractsProvider();
                bool eligible = contracts != null && IsEligibleAllocator(allocator, contracts);
                NativePanelAttachmentAction action = lifecycle.Observe(eligible, allocator);
                if (!eligible)
                {
                    if (action == NativePanelAttachmentAction.Detach) DestroyAttachedView(contracts);
                    EndOwnerIfSessionEnded();
                    return;
                }
                EnsureAttached(allocator, contracts, true);
            }
            catch (Exception exception)
            {
                ReportAttachment("Dice Roller panel attachment failure (construction): " + exception);
                Detach(contracts);
                return;
            }
            try
            {
                Render(contracts);
            }
            catch (Exception exception)
            {
                ReportAttachment("Dice Roller panel attachment failure (rendering): " + exception);
                Detach(contracts);
            }
        }

        public void Update()
        {
            KingmakerContracts contracts = contractsProvider();
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
                !active || allocator == null || !IsEligibleAllocator(allocator, contracts))
            {
                if (lifecycle.Observe(false, null) == NativePanelAttachmentAction.Detach)
                {
                    DestroyAttachedView(contracts);
                }
                EndOwnerIfSessionEnded();
                return;
            }

            lifecycle.Observe(true, allocator);
            try
            {
                EnsureAttached(allocator, contracts);
            }
            catch (Exception exception)
            {
                ReportAttachment("Dice Roller panel lifecycle failure (construction): " + exception);
                Detach(contracts);
                return;
            }
            try
            {
                Render(contracts);
            }
            catch (Exception exception)
            {
                ReportAttachment("Dice Roller panel lifecycle failure (rendering): " + exception);
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
            theme = null;
            attachedThemeOwner = null;
            themeRecovery.Reset();
            themeBindings.Clear();
            themeDiagnostics.Clear();
            paperLayers.Clear();
            lastAccessTabAnchorSource = null;
            assignmentRows.Clear();
            modeLabel = null;
            closeButton = null;
            commandError = null;
            presetLabel = null;
            policyLabel = null;
            minimumLabel = null;
            summaryLabel = null;
            historyLabel = null;
            savedLabel = null;
            messageLabel = null;
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
            lastProfile = null;
            lastScrolling = null;
            lastLayoutResult = null;
            lastAvailableWidth = -1f;
            lastAvailableHeight = -1f;
            lastPreferredBodyHeight = -1f;
            lastLayoutModelKey = null;
            lastLayoutDiagnostic = null;
            if (root != null)
            {
                root.SetActive(false);
                Object.Destroy(root);
            }
            root = null;
            panelState.DetachView();
        }

        private bool IsEligibleAllocator(object allocator, KingmakerContracts contracts)
        {
            var behaviour = allocator as MonoBehaviour;
            if (commands.ActiveSession != null && (behaviour == null || !behaviour.isActiveAndEnabled))
                ReportAttachment("Roll Stats unavailable: the owned ability allocator is hidden or inactive.");
            if (commands.ActiveSession != null && !commands.CanAttachNativePanel)
                ReportAttachment("Roll Stats unavailable: the current controller/preview binding is unresolved, unowned, or starting scores became locked.");
            if (behaviour == null || !behaviour.isActiveAndEnabled || !commands.CanAttachNativePanel) return false;
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

        private void EnsureAttached(object allocator, KingmakerContracts contracts, bool allocatorFilled = false)
        {
            RollSession session = commands.ActiveSession;
            if (session == null)
                throw new InvalidOperationException("A native panel cannot attach without an active roll session.");
            var behaviour = allocator as MonoBehaviour;
            if (behaviour == null)
                throw new InvalidOperationException("The exact native ability allocator is not a MonoBehaviour.");

            constructionBudget.Observe(allocator, session.Controller);
            if (constructionBudget.Exhausted) return;

            bool ownerChanged = panelState.ObserveOwner(session.Controller, session.StableOwner);
            Transform themeOwner = NativeBookTheme.FindOwner(behaviour);
            if (!ownerChanged && root != null && ReferenceEquals(attachedAllocator, allocator) &&
                attachedThemeOwner == themeOwner && root.transform.parent == behaviour.transform.parent)
            {
                RecoverTheme(behaviour, allocatorFilled);
                return;
            }

            DestroyAttachedView(contracts);
            TextMeshProUGUI nativeText = contracts.AbilityAllocatorMainLabelField.GetValue(allocator) as TextMeshProUGUI;
            Image nativeFrame = contracts.AbilityAllocatorFrameField.GetValue(allocator) as Image;
            Button nativeButton = ResolveNativeButton(allocator, contracts);
            if (nativeText == null || nativeFrame == null || nativeButton == null)
                throw new InvalidOperationException("Native text, material, or button styling could not be resolved.");

            try
            {
                // Construct the working fallback and all owned widgets exactly once.
                // Styling can subsequently change without touching their listeners/text/state.
                CreateOwnedView(behaviour, nativeText, nativeFrame, nativeButton);
                attachedAllocator = allocator;
                attachedThemeOwner = themeOwner;
                themeRecovery.Bind(allocator, themeOwner);
                RecoverTheme(behaviour, allocatorFilled);
                panelState.AttachView();
                ApplySurfaceState();
                PositionAccessTab(allocator, contracts);
                constructionBudget.Clear();
                AttachmentCount++;
            }
            catch (Exception)
            {
                constructionBudget.RecordFailure();
                throw;
            }
        }

        private void RecoverTheme(MonoBehaviour allocator, bool allocatorFilled)
        {
            // Update only checks cached object liveness. Donor lookup is attempted
            // initially and at most twice at the existing FillData postfix boundary.
            // Own skill-counter synchronization can reach that same hook; attempts
            // are consumed before resolution and never reset by a notification.
            try
            {
                if (theme != null && theme.Resources.DiscardStale())
                {
                    themeBindings.Apply(theme.Resources, ReportAttachment);
                    lastLayoutModelKey = null;
                    ReportTheme();
                }
                bool incomplete = theme == null || theme.Resources.Status != NativeThemeStatus.FullyThemed;
                if (!themeRecovery.TryBegin(incomplete, allocatorFilled)) return;
                try
                {
                    NativeBookTheme resolved = NativeUiPresentation.ResolveTheme(() => NativeBookTheme.Resolve(allocator), ReportAttachment);
                    if (resolved != null) theme = resolved;
                    themeBindings.Apply(theme == null ? null : theme.Resources, ReportAttachment);
                    lastLayoutModelKey = null;
                    ReportTheme();
                }
                finally { themeRecovery.Complete(); }
            }
            catch (Exception exception)
            {
                // A cosmetic retry never enters panel/session teardown or mechanic recovery.
                ReportAttachment("Native Dice Roller theme recovery failed; retaining owned controls: " + exception);
            }
        }

        private void ReportTheme()
        {
            logger.Info("Native Dice Roller theme: " + (theme == null ? "status=Fallback; capabilities=0/9" : theme.Resources.Summary) +
                "; attempt=" + themeRecovery.Attempts + "/" + NativeThemeRecovery.MaximumAttempts +
                "; owner=" + (theme == null ? "<unresolved>" : theme.OwnerLocation));
            if (theme == null) return;
            foreach (NativeThemeCapability capability in NativeThemeResolution.Capabilities)
            {
                NativeThemeResource resource = theme.Resources.Get(capability);
                string detail = resource == null ? "unavailable; " + theme.Resources.Failure(capability) : "available; " + resource.Identity;
                string previous;
                if (themeDiagnostics.TryGetValue(capability, out previous) && string.Equals(previous, detail, StringComparison.Ordinal)) continue;
                themeDiagnostics[capability] = detail;
                string message = "Native Dice Roller theme " + capability + ": " + detail;
                if (resource == null) logger.Warning(message); else logger.Info(message);
            }
        }

        private void CreateOwnedView(
            MonoBehaviour behaviour, TextMeshProUGUI nativeText, Image nativeFrame, Button nativeButton)
        {
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
            surfaceImage.color = Parchment;
            surfaceImage.raycastTarget = true;
            themeBindings.Add(NativeThemeCapability.Paper,
                donors => surfaceImage.color = Color.clear, () => surfaceImage.color = Parchment);
            CreatePaperLayer("PaperShadow", new Vector2(2f, -3f), new Color(0.16f, 0.10f, 0.06f, 0.24f));
            CreatePaperLayer("Paper", Vector2.zero, Color.white);
            // Only the inner body is masked. The paper silhouette and shadow
            // remain outside that viewport, with no full-screen hit surface.

            var surfaceLayout = expandedSurface.AddComponent<VerticalLayoutGroup>();
            surfaceLayout.padding = new RectOffset(
                layout.InternalPadding,
                layout.InternalPadding,
                (int)layout.SurfaceVerticalPadding + 8,
                (int)layout.SurfaceVerticalPadding - 8);
            surfaceLayout.spacing = layout.MajorVerticalSpacing;
            surfaceLayout.childControlWidth = true;
            surfaceLayout.childForceExpandWidth = true;
            surfaceLayout.childControlHeight = true;
            surfaceLayout.childForceExpandHeight = false;

            GameObject header = CreateHorizontal(expandedSurface.transform, layout.HeaderHeight);
            header.name = "FixedHeader";
            headerLayout = header.GetComponent<LayoutElement>();
            GameObject heading = CreateVertical("Heading", header.transform);
            heading.AddComponent<LayoutElement>().flexibleWidth = 1f;
            CreateLabel(
                heading.transform, "Rolled Ability Scores", nativeText,
                layout.TitleFontSize, TextAlignmentOptions.Left, 28f, -1f, HeadingText, true);
            modeLabel = CreateLabel(
                heading.transform, string.Empty, nativeText,
                layout.StatusFontSize, TextAlignmentOptions.Left, 20f, -1f, BodyText, true);
            closeButton = CreateButton(
                header.transform,
                "Close",
                nativeText,
                nativeButton,
                layout.CloseButtonWidth,
                () =>
                {
                    NativeUiPresentation.CloseDrawer(panelState, commands.NotifyDrawerClosed);
                    ApplySurfaceState();
                    if (accessTab != null) accessTab.GetComponent<Button>().Select();
                },
                layout.CloseButtonHeight);

            {
                GameObject ornament = NewUiObject("HeaderRule", root.layer);
                RectTransform rect = ornament.GetComponent<RectTransform>();
                rect.SetParent(expandedSurface.transform, false);
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.sizeDelta = new Vector2(-2f * layout.InternalPadding, 5f);
                rect.anchoredPosition = new Vector2(0f, -layout.SurfaceVerticalPadding - 8f - layout.HeaderHeight - 1f);
                ornament.AddComponent<LayoutElement>().ignoreLayout = true;
                Image rule = ornament.AddComponent<Image>();
                rule.raycastTarget = false;
                rule.enabled = false;
                themeBindings.Add(NativeThemeCapability.Ornament,
                    donors => { NativeBookTheme.CopyImage((Image)donors[0], rule); rule.enabled = true; },
                    () => { rule.sprite = null; rule.enabled = false; });
            }
            Transform content = CreateScrollContent(expandedSurface.transform);
            messageLabel = CreateLabel(
                content, string.Empty, nativeText, layout.StatusFontSize,
                TextAlignmentOptions.Left, -1f, -1f, ErrorText, false);
            messageLabel.gameObject.name = "InlineMessage";
            SetVisible(messageLabel.gameObject, false);
            CreatePanelContent(content, nativeText, nativeButton);
        }

        private void CreatePaperLayer(string name, Vector2 offset, Color tint)
        {
            GameObject layer = NewUiObject(name, root.layer);
            RectTransform rect = layer.GetComponent<RectTransform>();
            rect.SetParent(expandedSurface.transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(layout.PreferredExpandedWidth * 2f, layout.PreferredExpandedHeight * 2f);
            rect.anchoredPosition = offset;
            // Unity 2018 has no Image.pixelsPerUnitMultiplier. Uniform half
            // scale retains the verified sliced border without editing sprites.
            rect.localScale = new Vector3(0.5f, 0.5f, 1f);
            layer.AddComponent<LayoutElement>().ignoreLayout = true;
            Image image = layer.AddComponent<Image>();
            image.raycastTarget = false;
            image.enabled = false;
            themeBindings.Add(NativeThemeCapability.Paper,
                donors => { NativeBookTheme.CopyImage((Image)donors[0], image); image.color = tint; image.enabled = true; },
                () => { image.sprite = null; image.enabled = false; });
            paperLayers.Add(rect);
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
            SetVisible(bodyScrollbarObject, false);
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
            advancedDisclosure.GetComponent<LayoutElement>().preferredHeight = layout.OrdinaryControlHeight;
            advancedDisclosure.GetComponent<LayoutElement>().minHeight = layout.OrdinaryControlHeight;
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
                108f,
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
                if (!rendering)
                {
                    commandError = null;
                    commands.SetCustomExpression(value);
                }
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
                -1f,
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
                108f,
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
                true,
                NativeThemeCapability.Selector);
            valueLabel.fontSize = layout.BodyFontSize;
            valueLabel.enableWordWrapping = true;
            valueLabel.overflowMode = TextOverflowModes.Overflow;
            // The value sets the row's measured height on narrow pages. Keep
            // the native font readable instead of hiding the selected rule.
            valueLabel.GetComponent<LayoutElement>().preferredHeight = -1f;
            section.GetComponent<LayoutElement>().preferredHeight = -1f;
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
                    -1f,
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
            historyDisclosure.GetComponent<LayoutElement>().preferredHeight = layout.OrdinaryControlHeight;
            historyDisclosure.GetComponent<LayoutElement>().minHeight = layout.OrdinaryControlHeight;
            historyDisclosureLabel = disclosure.GetComponentInChildren<TextMeshProUGUI>();

            historyDetails = CreateVertical("HistoryDetails", parent);
            historyLabel = CreateLabel(
                historyDetails.transform,
                string.Empty,
                nativeText,
                layout.BodyFontSize,
                TextAlignmentOptions.Left,
                -1f,
                -1f,
                BodyText,
                false);
            GameObject row = CreateHorizontal(historyDetails.transform, layout.OrdinaryControlHeight);
            CreateButton(row.transform, "Previous", nativeText, nativeButton, 88f,
                () => Execute(RollUiCommand.PreviousHistory));
            CreateButton(row.transform, "Next", nativeText, nativeButton, 60f,
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
            savedDisclosure.GetComponent<LayoutElement>().preferredHeight = layout.OrdinaryControlHeight;
            savedDisclosure.GetComponent<LayoutElement>().minHeight = layout.OrdinaryControlHeight;
            savedDisclosureLabel = disclosure.GetComponentInChildren<TextMeshProUGUI>();

            savedDetails = CreateVertical("SavedDetails", parent);
            savedLabel = CreateLabel(
                savedDetails.transform,
                string.Empty,
                nativeText,
                layout.BodyFontSize,
                TextAlignmentOptions.Left,
                -1f,
                -1f,
                BodyText,
                false);
            GameObject row = CreateHorizontal(savedDetails.transform, layout.OrdinaryControlHeight);
            storeButton = CreateButton(row.transform, "Store", nativeText, nativeButton, 62f,
                () => Execute(RollUiCommand.StoreCurrent));
            CreateButton(row.transform, "Previous", nativeText, nativeButton, 88f,
                () => Execute(RollUiCommand.PreviousSaved));
            CreateButton(row.transform, "Next", nativeText, nativeButton, 60f,
                () => Execute(RollUiCommand.NextSaved));
            GameObject savedActions = CreateHorizontal(savedDetails.transform, layout.OrdinaryControlHeight);
            recallButton = CreateButton(savedActions.transform, "Recall", nativeText, nativeButton, 70f,
                () => Execute(RollUiCommand.RecallSaved));
            deleteButton = CreateButton(savedActions.transform, "Delete", nativeText, nativeButton, 70f,
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
            Color trackColor = track.color;
            Color handleColor = handle.color;
            ColorBlock scrollColors = scrollbar.colors;
            Selectable.Transition scrollTransition = scrollbar.transition;
            themeBindings.Add(NativeThemeCapability.Scrollbar,
                donors =>
                {
                    NativeBookTheme.CopyImage((Image)donors[0], track);
                    NativeBookTheme.CopyImage((Image)donors[1], handle);
                    track.raycastTarget = handle.raycastTarget = true;
                    scrollbar.colors = ((Scrollbar)donors[2]).colors;
                    scrollbar.transition = ((Scrollbar)donors[2]).transition;
                },
                () =>
                {
                    ResetFallbackImage(track, null, trackColor);
                    ResetFallbackImage(handle, null, handleColor);
                    scrollbar.colors = scrollColors;
                    scrollbar.transition = scrollTransition;
                });
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
                    if (closeButton != null) closeButton.Select();
                },
                layout.AccessTabHeight);
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
                lastPreferredBodyHeight >= 0f ? lastPreferredBodyHeight :
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
            SetInteractable(minimumDown, model.MinimumEnabled);
            SetInteractable(minimumUp, model.MinimumEnabled);
            SetVisible(advancedDisclosure, model.AdvancedVisible);
            SetVisible(advancedContent, model.AdvancedExpanded);
            advancedLabel.text = model.AdvancedLabel;
            SetVisible(minimumSection, model.MinimumVisible);
            SetVisible(customSection, model.CustomVisible);
            if (customInput.text != model.CustomExpression) customInput.text = model.CustomExpression;

            SetVisible(rollButton.gameObject, model.RollVisible);
            SetVisible(rerollButton.gameObject, model.RerollVisible);
            SetVisible(returnButton.gameObject, model.ReturnToPointBuyVisible);
            SetInteractable(rollButton, model.CanRoll);
            SetInteractable(rerollButton, model.CanReroll);
            SetInteractable(returnButton, model.CanReturnToPointBuy);

            SetVisible(assignmentSection, model.AssignmentVisible);
            for (int index = 0; index < assignmentRows.Count; index++)
            {
                AssignmentWidgets widgets = assignmentRows[index];
                bool available = model.AssignmentVisible && index < model.AssignmentRows.Count;
                SetVisible(widgets.Root, available);
                if (!available) continue;
                RollPanelAssignmentRow row = model.AssignmentRows[index];
                widgets.Value.text = row.Label + "   " + row.Value;
                SetInteractable(widgets.Up, row.CanMoveUp);
                SetInteractable(widgets.Down, row.CanMoveDown);
            }

            SetVisible(summarySection, model.SummaryVisible);
            summaryLabel.text = model.Summary;
            SetVisible(historyDisclosure, model.HistoryDisclosureVisible);
            historyDisclosureLabel.text = model.HistoryDisclosureLabel;
            SetVisible(historyDetails, model.HistoryDetailsVisible);
            historyLabel.text = "History   " + model.History;
            SetInteractable(useHistoryButton, model.CanUseHistory);

            SetVisible(savedDisclosure, model.SavedDisclosureVisible);
            savedDisclosureLabel.text = model.SavedDisclosureLabel;
            SetVisible(savedDetails, model.SavedDetailsVisible);
            savedLabel.text = "Saved   " + model.Saved;
            SetInteractable(storeButton, model.CanStore);
            SetInteractable(recallButton, model.CanRecall);
            SetInteractable(deleteButton, model.CanDeleteSaved);
            bool hasError = !string.IsNullOrWhiteSpace(model.Error) || !string.IsNullOrWhiteSpace(commandError);
            string message = !string.IsNullOrWhiteSpace(model.Error) ? model.Error : commandError ?? model.Status;
            if (messageLabel.text != message && hasError) bodyScroll.verticalNormalizedPosition = 1f;
            messageLabel.text = message ?? string.Empty;
            messageLabel.color = hasError ? ErrorText : BodyText;
            SetVisible(messageLabel.gameObject, !string.IsNullOrWhiteSpace(message));
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
                foreach (RectTransform paper in paperLayers)
                    paper.sizeDelta = new Vector2(result.PanelWidth * 2f, result.PanelHeight * 2f);
            }
            if (headerLayout != null)
            {
                headerLayout.preferredHeight = result.HeaderHeight;
                headerLayout.minHeight = result.HeaderHeight;
            }
            if (bodyScroll != null)
            {
                bodyScroll.horizontal = false;
                bodyScroll.vertical = result.ScrollingRequired;
                if (!result.ScrollingRequired) bodyScroll.verticalNormalizedPosition = 1f;
            }
            if (bodyScrollbarObject != null)
            {
                SetVisible(bodyScrollbarObject, result.ScrollingRequired);
            }
            if (bodyViewport != null)
            {
                bodyViewport.offsetMin = Vector2.zero;
                bodyViewport.offsetMax = new Vector2(
                    result.ScrollingRequired ? -(layout.ScrollbarWidth + 2f) : 0f,
                    0f);
            }
        }

        private string BuildLayoutModelKey(RollPanelModel model)
        {
            return string.Join("|", new[]
            {
                model.Profile.ToString(),
                model.Mode,
                model.Preset,
                model.Policy,
                model.Message ?? string.Empty,
                commandError ?? string.Empty,
                model.Summary ?? string.Empty,
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
                model.History ?? string.Empty,
                model.Saved ?? string.Empty
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
            if (expandedSurface != null) SetVisible(expandedSurface, panelState.ExpandedSurfaceActive);
            if (accessTab != null) SetVisible(accessTab, panelState.AccessTabActive);
        }

        private void Execute(RollUiCommand command, AbilityScore ability = AbilityScore.Strength)
        {
            string error;
            bool succeeded = commands.Execute(command, ability, out error);
            commandError = succeeded ? null : error;
            if (!succeeded && !string.IsNullOrWhiteSpace(error))
                logger.Warning("Native Dice Roller command failed: " + error);
            Render(contractsProvider());
        }

        private static Button ResolveNativeButton(object allocator, KingmakerContracts contracts)
        {
            IList entries = contracts.AbilityAllocatorStatEntriesField.GetValue(allocator) as IList;
            if (entries == null || entries.Count == 0 || entries[0] == null) return null;
            return contracts.ScoreEntryUpButtonField.GetValue(entries[0]) as Button;
        }

        private static void SetVisible(GameObject target, bool visible)
        {
            if (target.activeSelf != visible) target.SetActive(visible);
        }

        private static void SetInteractable(Button target, bool interactable)
        {
            if (target.interactable != interactable) target.interactable = interactable;
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
            layout.childAlignment = TextAnchor.MiddleLeft;
            var element = row.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = height;
            return row;
        }

        private TextMeshProUGUI CreateLabel(
            Transform parent,
            string text,
            TextMeshProUGUI source,
            float fontSize,
            TextAlignmentOptions alignment,
            float preferredHeight,
            float preferredWidth,
            Color color,
            bool singleLine,
            NativeThemeCapability? role = null)
        {
            GameObject gameObject = NewUiObject("Label", parent.gameObject.layer);
            gameObject.transform.SetParent(parent, false);
            var label = gameObject.AddComponent<TextMeshProUGUI>();
            label.font = source.font;
            label.fontSharedMaterial = source.fontSharedMaterial;
            label.color = color;
            label.richText = false;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.enableWordWrapping = !singleLine;
            label.overflowMode = singleLine ? TextOverflowModes.Ellipsis : TextOverflowModes.Overflow;
            label.raycastTarget = false;
            label.text = text;
            BindTextStyle(label, role ?? (color == HeadingText ? NativeThemeCapability.Heading : NativeThemeCapability.Body), color);
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

        private Button CreateButton(
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
            Material fallbackMaterial = image.material;
            themeBindings.Add(NativeThemeCapability.Buttons,
                donors => NativeBookTheme.ApplyButton((Button)donors[0], button, image),
                () =>
                {
                    ResetFallbackImage(image, fallbackMaterial, ButtonSurface);
                    button.spriteState = new SpriteState();
                    button.colors = colors;
                    button.transition = Selectable.Transition.ColorTint;
                });
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() => NativeUiPresentation.Activate(
                action, NativeBookTheme.PlayClick, ReportAttachment));

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
                true,
                NativeThemeCapability.ButtonText);
            label.enableAutoSizing = true;
            label.fontSizeMin = NativeRollPanelLayoutSpec.Default.BodyFontSize;
            label.fontSizeMax = NativeRollPanelLayoutSpec.Default.BodyFontSize;
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(4f, 2f);
            labelRect.offsetMax = new Vector2(-4f, -2f);
            return button;
        }

        private TMP_InputField CreateInput(
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

            // Bind the dependencies first: the installed TMP_InputField.caretColor
            // getter returns textComponent.color while customCaretColor is false,
            // so reading it on a freshly added, unbound input throws.
            input.textViewport = viewport;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.richText = false;

            GameObject frame = NewUiObject("InputFrame", parent.gameObject.layer);
            RectTransform frameRect = frame.GetComponent<RectTransform>();
            frameRect.SetParent(gameObject.transform, false);
            frameRect.anchorMin = Vector2.zero;
            frameRect.anchorMax = Vector2.one;
            frameRect.offsetMin = frameRect.offsetMax = Vector2.zero;
            Image frameImage = frame.AddComponent<Image>();
            frameImage.raycastTarget = false;
            frameImage.enabled = false;
            Material fallbackMaterial = image.material;
            Color fallbackColor = image.color;
            Color selectionColor = input.selectionColor;
            Color caretColor = input.caretColor;
            float blinkRate = input.caretBlinkRate;
            bool customCaret = input.customCaretColor;
            themeBindings.Add(NativeThemeCapability.Input,
                donors =>
                {
                    NativeBookTheme.CopyImage((Image)donors[0], image);
                    image.raycastTarget = true;
                    NativeBookTheme.CopyImage((Image)donors[1], frameImage);
                    frameImage.enabled = true;
                    var nativeInput = (TMP_InputField)donors[2];
                    input.selectionColor = nativeInput.selectionColor;
                    input.caretBlinkRate = nativeInput.caretBlinkRate;
                    input.caretColor = nativeInput.caretColor;
                    input.customCaretColor = true;
                },
                () =>
                {
                    ResetFallbackImage(image, fallbackMaterial, fallbackColor);
                    frameImage.sprite = null;
                    frameImage.enabled = false;
                    input.selectionColor = selectionColor;
                    input.caretBlinkRate = blinkRate;
                    input.caretColor = caretColor;
                    input.customCaretColor = customCaret;
                });

            var layout = gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 32f;
            layout.minHeight = 32f;
            layout.flexibleWidth = 1f;
            return input;
        }

        private static void ResetFallbackImage(Image image, Material material, Color color)
        {
            image.overrideSprite = null;
            image.sprite = null;
            image.material = material;
            image.type = Image.Type.Simple;
            image.color = color;
            image.preserveAspect = false;
            image.fillCenter = true;
            image.raycastTarget = true;
        }

        private void BindTextStyle(TextMeshProUGUI label, NativeThemeCapability role, Color color)
        {
            // Capture fallback data, not a native behaviour whose owner may disappear.
            TMP_FontAsset font = label.font;
            Material material = label.fontSharedMaterial;
            FontStyles style = label.fontStyle;
            float characters = label.characterSpacing;
            float words = label.wordSpacing;
            float lines = label.lineSpacing;
            themeBindings.Add(role,
                donors =>
                {
                    NativeBookTheme.CopyText((TextMeshProUGUI)donors[0], label);
                    if (role != NativeThemeCapability.ButtonText) label.color = color;
                },
                () =>
                {
                    label.font = font;
                    label.fontSharedMaterial = material;
                    label.fontStyle = style;
                    label.characterSpacing = characters;
                    label.wordSpacing = words;
                    label.lineSpacing = lines;
                    label.color = color;
                });
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
