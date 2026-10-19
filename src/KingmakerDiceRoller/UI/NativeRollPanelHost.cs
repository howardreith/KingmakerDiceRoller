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
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace KingmakerDiceRoller.UI
{
    public sealed class NativeRollPanelHost
    {
        public const string OwnedPanelName = "KingmakerDiceRoller.NativeRollPanel";

        // Label RectTransform insets inside a button; see CreateButton.
        private const float CaptionHorizontalInset = 4f;
        private const float CaptionVerticalInset = 2f;

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
        private readonly List<Button> inlineDownArrows = new List<Button>();
        private readonly List<Button> inlinePresetChoices = new List<Button>();
        private readonly List<GameObject> inlineOwnedArrows = new List<GameObject>();
        private readonly List<NativeButtonVisualState> nativeRowVisuals = new List<NativeButtonVisualState>();
        private NativeViewCommandBinding viewBinding;
        private GameObject inlineToolbar;
        private GameObject inlinePointBuyEntry;
        private GameObject inlineWorkspace;
        private GameObject inlinePolicyNote;
        private GameObject presetPopup;
        private GameObject presetBlocker;
        private TextMeshProUGUI policyNoteLabel;
        private TMP_InputField formulaInput;
        private RollFormulaDraft formulaDraft;
        private Button inlineEntryButton;
        private Button inlinePreviousButton;
        private Button inlineNextButton;
        private TextMeshProUGUI badgeNativeCaption;
        private TextMeshProUGUI badgeNativeValue;
        private TextMeshProUGUI badgeOwnedCaption;
        private TextMeshProUGUI badgeOwnedValue;
        private GameObject badgeActiveBackground;
        private bool badgeCaptionOriginallyEnabled;
        private bool badgeValueOriginallyEnabled;
        private bool workspaceOpen;
        private bool nativeVisualsHidden;
        private bool readyControlsSuppressed;
        private RollSessionMode? lastInlineMode;
        private GameObject inlineStatus;
        private GameObject presetChoices;
        private GameObject presetSelectorSection;
        private GameObject policySelectorSection;
        private TextMeshProUGUI inlineStatusLabel;
        private Button inlinePresetButton;
        private Button inlineRollButton;
        private Button inlineRerollButton;
        private Button inlineSaveButton;
        private Button inlineLoadButton;
        private Button inlineReturnButton;
        private bool inlineAvailable;
        private int inlineAttachAttempts;
        private InlineMenuKind inlineMenu;

        private enum InlineMenuKind { None, Presets, Load }

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
        private GameObject pointActionsRow;
        private GameObject returnRow;
        private LayoutElement accessTabLayout;
        private float accessTabFittedWidth;
        private bool rendering;
        private RectTransform expandedSurfaceRect;
        private RectTransform bodyViewport;
        private RectTransform bodyContent;
        private ScrollRect bodyScroll;
        private LayoutElement headerLayout;
        private RollPanelPresentationProfile? lastProfile;
        private bool? lastScrolling;
        private ResponsiveRollPanelLayoutResult lastLayoutResult;
        private LayoutElement advancedDisclosureLayout;
        private LayoutElement historyDisclosureLayout;
        private LayoutElement savedDisclosureLayout;
        private float lastAvailableWidth = -1f;
        private float lastAvailableHeight = -1f;
        private float lastPreferredBodyHeight = -1f;
        private string lastLayoutModelKey;
        private string lastLayoutDiagnostic;

        private string commandError;
        private Button closeButton;
        private TextMeshProUGUI modeLabel;
        private TextMeshProUGUI menuHeadingLabel;
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
            if (inlineAvailable && Input.GetKeyDown(KeyCode.Escape) &&
                inlineMenu == InlineMenuKind.Presets)
            {
                ClosePresetPopup();
            }
            else if (inlineAvailable && formulaInput != null && formulaInput.isFocused &&
                Input.GetKeyDown(KeyCode.Escape) && formulaDraft != null)
            {
                formulaDraft.Revert();
                rendering = true;
                try { formulaInput.text = formulaDraft.Text; }
                finally { rendering = false; }
                formulaInput.DeactivateInputField();
                commandError = null;
            }
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
            workspaceOpen = false;
            formulaDraft = null;
            lastInlineMode = null;
        }

        private void EndOwnerIfSessionEnded()
        {
            if (commands.ActiveSession == null)
            {
                panelState.EndOwner();
                workspaceOpen = false;
                formulaDraft = null;
                lastInlineMode = null;
            }
        }

        private void DestroyAttachedView(KingmakerContracts contracts)
        {
            nativeControls.RestoreOwnedStates(contracts);
            DestroyInlineView();
            presetChoices = null;
            presetSelectorSection = null;
            policySelectorSection = null;
            inlineAttachAttempts = 0;
            viewBinding = null;
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
            menuHeadingLabel = null;
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
            pointActionsRow = null;
            returnRow = null;
            accessTabLayout = null;
            accessTabFittedWidth = 0f;
            advancedDisclosureLayout = null;
            historyDisclosureLayout = null;
            savedDisclosureLayout = null;
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
            if (ownerChanged || formulaDraft == null)
            {
                workspaceOpen = false;
                formulaDraft = new RollFormulaDraft(commands.Snapshot.Configuration);
                lastInlineMode = null;
                inlineMenu = InlineMenuKind.None;
            }
            Transform themeOwner = NativeBookTheme.FindOwner(behaviour);
            if (!ownerChanged && root != null && ReferenceEquals(attachedAllocator, allocator) &&
                attachedThemeOwner == themeOwner && root.transform.parent == behaviour.transform.parent)
            {
                RecoverTheme(behaviour, allocatorFilled);
                if (!inlineAvailable && allocatorFilled && inlineAttachAttempts < 2)
                {
                    AttemptInlineAttachment(behaviour, contracts);
                    if (inlineAvailable)
                    {
                        panelState.Close();
                        if (theme != null)
                        {
                            themeBindings.InvalidateApplied();
                            themeBindings.Apply(theme.Resources, ReportAttachment);
                        }
                    }
                }
                return;
            }

            InlineMenuKind previousMenu = ownerChanged ? InlineMenuKind.None : inlineMenu;
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
                viewBinding = new NativeViewCommandBinding(
                    session.Controller, session.StableOwner, allocator);
                AttemptInlineAttachment(behaviour, contracts, nativeText, nativeButton);
                if (inlineAvailable)
                {
                    inlineMenu = previousMenu;
                    if (inlineMenu == InlineMenuKind.None && panelState.IsExpanded)
                        panelState.Close();
                }
                attachedThemeOwner = themeOwner;
                themeRecovery.Bind(allocator, themeOwner);
                RecoverTheme(behaviour, allocatorFilled);
                panelState.AttachView();
                ApplySurfaceState();
                if (!inlineAvailable) PositionAccessTab(allocator, contracts);
                constructionBudget.Clear();
                AttachmentCount++;
            }
            catch (Exception)
            {
                constructionBudget.RecordFailure();
                throw;
            }
        }

        private void AttemptInlineAttachment(MonoBehaviour allocator, KingmakerContracts contracts,
            TextMeshProUGUI nativeText = null, Button nativeButton = null)
        {
            inlineAttachAttempts++;
            int originalThemeBindings = themeBindings.Count;
            try
            {
                if (nativeText == null)
                    nativeText = contracts.AbilityAllocatorMainLabelField.GetValue(allocator) as TextMeshProUGUI;
                if (nativeButton == null) nativeButton = ResolveNativeButton(allocator, contracts);
                if (nativeText == null || nativeButton == null)
                    throw new InvalidOperationException("Native row styling is unavailable.");
                inlineAvailable = TryCreateInlineView(allocator, contracts, nativeText, nativeButton);
            }
            catch (Exception exception)
            {
                ReportAttachment("Native inline stats attachment unavailable; using Roll Stats drawer: " + exception.Message);
                DestroyInlineView();
                themeBindings.TrimTo(originalThemeBindings);
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

        private bool TryCreateInlineView(
            MonoBehaviour allocator, KingmakerContracts contracts,
            TextMeshProUGUI nativeText, Button nativeButton)
        {
            IList entries = contracts.AbilityAllocatorStatEntriesField.GetValue(allocator) as IList;
            if (entries == null || entries.Count != Abilities.Length)
                throw new InvalidOperationException("The live allocator does not have six native score rows.");
            var mapped = new object[Abilities.Length];
            var rowKeys = new object[Abilities.Length];
            Transform statsContainer = null;
            for (int rowIndex = 0; rowIndex < entries.Count; rowIndex++)
            {
                object entry = entries[rowIndex];
                var row = entry as MonoBehaviour;
                if (row == null || !row.isActiveAndEnabled)
                    throw new InvalidOperationException("A native score row is unavailable.");
                rowKeys[rowIndex] = contracts.ScoreEntryTypeField.GetValue(entry);
                Button up = contracts.ScoreEntryUpButtonField.GetValue(entry) as Button;
                Button down = contracts.ScoreEntryDownButtonField.GetValue(entry) as Button;
                if (up == null || down == null || !up.transform.IsChildOf(row.transform) ||
                    !down.transform.IsChildOf(row.transform))
                    throw new InvalidOperationException("Native row buttons do not belong to their score row.");
                if (statsContainer == null) statsContainer = row.transform.parent;
                if (statsContainer != row.transform.parent || statsContainer == null ||
                    statsContainer.parent != allocator.transform)
                    throw new InvalidOperationException("Native score rows do not share the allocator's stat container.");
            }
            int[] rowIndexByAbility = NativeScoreRowIdentity.Map(contracts.AbilityStatKeys, rowKeys);
            for (int ability = 0; ability < mapped.Length; ability++)
                mapped[ability] = entries[rowIndexByAbility[ability]];

            inlineToolbar = CreateVertical("InlineRollToolbar", allocator.transform);
            inlineToolbar.transform.SetSiblingIndex(statsContainer.GetSiblingIndex() + 1);
            var toolbarLayout = inlineToolbar.AddComponent<LayoutElement>();
            toolbarLayout.minHeight = 42f;
            toolbarLayout.preferredHeight = 42f;
            toolbarLayout.flexibleWidth = 1f;
            var toolbarGroup = inlineToolbar.GetComponent<VerticalLayoutGroup>();
            toolbarGroup.padding = new RectOffset(8, 8, 1, 1);
            toolbarGroup.spacing = 0f;

            inlinePointBuyEntry = CreateHorizontal(inlineToolbar.transform, 40f);
            inlinePointBuyEntry.name = "PointBuyEntry";
            inlinePointBuyEntry.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
            inlineEntryButton = CreateButton(inlinePointBuyEntry.transform, "Roll for Stats", nativeText,
                nativeButton, 245f, OpenRollWorkspace, 38f);

            inlineWorkspace = CreateVertical("RollWorkspace", inlineToolbar.transform);
            var workspaceGroup = inlineWorkspace.GetComponent<VerticalLayoutGroup>();
            workspaceGroup.spacing = 6f;
            workspaceGroup.padding = new RectOffset(0, 0, 1, 1);
            GameObject formulaRow = CreateHorizontal(inlineWorkspace.transform, 40f);
            formulaRow.name = "FormulaRow";
            formulaInput = CreateInput(formulaRow.transform, nativeText, nativeButton);
            formulaInput.gameObject.name = "RollFormula";
            var formulaLayout = formulaInput.GetComponent<LayoutElement>();
            formulaLayout.minHeight = formulaLayout.preferredHeight = 40f;
            formulaInput.text = formulaDraft.Text;
            formulaInput.onValueChanged.AddListener(value =>
            {
                if (!rendering && CanDispatchCurrentView())
                {
                    formulaDraft.Edit(value, commands.Snapshot.Configuration);
                    commandError = null;
                    Render(contractsProvider());
                }
            });
            formulaInput.onSubmit.AddListener(value => CommitFormulaDraft());
            formulaInput.onEndEdit.AddListener(value =>
            {
                if (!rendering && !Input.GetKeyDown(KeyCode.Escape) &&
                    formulaDraft.HasUncommittedEdit) CommitFormulaDraft();
            });
            inlinePresetButton = CreateButton(formulaRow.transform, "▼", nativeText,
                nativeButton, 42f, () => OpenInlineMenu(InlineMenuKind.Presets), 40f);
            AddInlineTooltip(inlinePresetButton.gameObject, nativeText, "Choose a roll formula");

            inlinePolicyNote = NewUiObject("RollPolicyNote", inlineToolbar.layer);
            inlinePolicyNote.transform.SetParent(inlineWorkspace.transform, false);
            policyNoteLabel = CreateLabel(inlinePolicyNote.transform, string.Empty, nativeText,
                layout.StatusFontSize, TextAlignmentOptions.Left, 26f, -1f, BodyText, false);
            inlinePolicyNote.AddComponent<LayoutElement>().preferredHeight = 26f;
            SetVisible(inlinePolicyNote, false);

            GameObject primary = CreateHorizontal(inlineWorkspace.transform, 48f);
            primary.name = "PrimaryActions";
            primary.GetComponent<HorizontalLayoutGroup>().spacing = 9f;
            inlineLoadButton = CreateButton(primary.transform, "Load", nativeText,
                nativeButton, 160f, () => OpenInlineMenu(InlineMenuKind.Load), 48f);
            inlineSaveButton = CreateButton(primary.transform, "Save", nativeText,
                nativeButton, 160f, () => Execute(RollUiCommand.StoreCurrent), 48f);
            inlineRollButton = CreateButton(primary.transform, "Roll", nativeText,
                nativeButton, 160f, ExecuteGeneration, 48f);
            inlineRerollButton = CreateButton(primary.transform, "Reroll", nativeText,
                nativeButton, 160f, ExecuteGeneration, 48f);
            foreach (Button button in new[] { inlineLoadButton, inlineSaveButton, inlineRollButton, inlineRerollButton })
                button.GetComponent<LayoutElement>().flexibleWidth = 1f;

            GameObject history = CreateHorizontal(inlineWorkspace.transform, 34f);
            history.name = "RecentRollNavigation";
            history.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
            inlinePreviousButton = CreateButton(history.transform, "Previous", nativeText,
                nativeButton, 145f, () => Execute(RollUiCommand.PreviousHistory), 34f);
            inlineNextButton = CreateButton(history.transform, "Next", nativeText,
                nativeButton, 145f, () => Execute(RollUiCommand.NextHistory), 34f);
            AddInlineTooltip(inlinePreviousButton.gameObject, nativeText, "Restore previous roll");
            AddInlineTooltip(inlineNextButton.gameObject, nativeText, "Restore next roll");

            GameObject returnRow = CreateHorizontal(inlineWorkspace.transform, 46f);
            returnRow.name = "ReturnRow";
            returnRow.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
            inlineReturnButton = CreateButton(returnRow.transform, "Return to Point Buy", nativeText,
                nativeButton, 265f, ReturnFromWorkspace, 34f);
            AddInlineTooltip(inlineReturnButton.gameObject, nativeText,
                "Restore your exact Point Buy allocation and remaining points.", 390f, 42f);

            inlineStatus = NewUiObject("InlineStatus", inlineToolbar.layer);
            inlineStatus.transform.SetParent(inlineToolbar.transform, false);
            inlineStatusLabel = CreateLabel(inlineStatus.transform, string.Empty, nativeText,
                layout.StatusFontSize, TextAlignmentOptions.Left, 24f, -1f, ErrorText, false);
            RectTransform inlineStatusRect = inlineStatusLabel.rectTransform;
            inlineStatusRect.anchorMin = Vector2.zero;
            inlineStatusRect.anchorMax = Vector2.one;
            inlineStatusRect.offsetMin = new Vector2(4f, 0f);
            inlineStatusRect.offsetMax = new Vector2(-4f, 0f);
            var statusLayout = inlineStatus.AddComponent<LayoutElement>();
            statusLayout.minHeight = 26f;
            statusLayout.preferredHeight = 26f;
            SetVisible(inlineStatus, false);
            SetVisible(inlineWorkspace, false);

            for (int index = 0; index < mapped.Length; index++)
            {
                object entry = mapped[index];
                Button nativeUp = contracts.ScoreEntryUpButtonField.GetValue(entry) as Button;
                Button nativeDown = contracts.ScoreEntryDownButtonField.GetValue(entry) as Button;
                AbilityScore ability = Abilities[index];
                Button down = CreateInlineArrow(nativeUp, nativeText,
                    index == Abilities.Length - 1 ? "No score below" : "Swap with " + Abilities[index + 1],
                    () => Execute(RollUiCommand.MoveDown, ability));
                inlineDownArrows.Add(down);
                nativeRowVisuals.Add(new NativeButtonVisualState(nativeUp));
                nativeRowVisuals.Add(new NativeButtonVisualState(nativeDown));
            }
            CreateBadgeOverlay(allocator, contracts);
            CreatePresetPopup(nativeText, nativeButton);
            ValidateInlineGeometry(allocator, statsContainer, contracts);
            logger.Info("Native roll workspace attached to six exact score keys and the allocator's own Points badge.");
            return true;
        }

        private void ValidateInlineGeometry(
            MonoBehaviour allocator, Transform statsContainer, KingmakerContracts contracts)
        {
            RectTransform allocatorRect = allocator.transform as RectTransform;
            RectTransform statsRect = statsContainer as RectTransform;
            RectTransform toolbarRect = inlineToolbar.transform as RectTransform;
            if (allocatorRect == null || statsRect == null || toolbarRect == null)
                throw new InvalidOperationException("Native inline stat geometry is unavailable.");
            LayoutRebuilder.ForceRebuildLayoutImmediate(allocatorRect);
            LocalLayoutRect? stats = TryGetLocalBounds(statsRect, allocatorRect);
            LocalLayoutRect? toolbar = TryGetLocalBounds(toolbarRect, allocatorRect);
            Rect area = allocatorRect.rect;
            if (!stats.HasValue || !toolbar.HasValue ||
                toolbar.Value.Width < 520f ||
                toolbar.Value.XMin < area.xMin - 1f || toolbar.Value.XMax > area.xMax + 1f ||
                toolbar.Value.YMax > stats.Value.YMin + 2f ||
                toolbar.Value.YMin < area.yMin - 1f)
                throw new InvalidOperationException("The compact toolbar does not fit below the native score rows.");
            GameObject racial = contracts.AbilityAllocatorRaceBonusContainerField.GetValue(allocator) as GameObject;
            if (racial != null && racial.activeInHierarchy)
            {
                LocalLayoutRect? race = TryGetLocalBounds(racial.transform as RectTransform, allocatorRect);
                if (!race.HasValue || toolbar.Value.YMin < race.Value.YMax - 2f ||
                    race.Value.YMin < area.yMin - 1f)
                    throw new InvalidOperationException("The compact toolbar would overlap the native racial selector.");
            }
        }

        private Button CreateInlineArrow(Button source, TextMeshProUGUI nativeText,
            string tooltip, Action action)
        {
            GameObject owned = NewUiObject("Assignment." + tooltip, source.gameObject.layer);
            owned.transform.SetParent(source.transform.parent, false);
            var rect = owned.GetComponent<RectTransform>();
            RectTransform donorRect = source.GetComponent<RectTransform>();
            rect.anchorMin = donorRect.anchorMin;
            rect.anchorMax = donorRect.anchorMax;
            rect.pivot = donorRect.pivot;
            rect.anchoredPosition = donorRect.anchoredPosition;
            rect.sizeDelta = donorRect.sizeDelta;
            rect.localRotation = donorRect.localRotation;
            rect.SetSiblingIndex(source.transform.GetSiblingIndex() + 1);
            owned.AddComponent<LayoutElement>().ignoreLayout = true;
            owned.AddComponent<InlineArrowAnchor>().Bind(donorRect);
            Image image = owned.AddComponent<Image>();
            NativeBookTheme.CopyImage(source.targetGraphic as Image, image);
            image.raycastTarget = true;
            Button button = owned.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = source.colors;
            button.spriteState = source.spriteState;
            button.transition = source.transition;
            button.navigation = new Navigation { mode = Navigation.Mode.Automatic };
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() =>
            {
                if (!CanDispatchCurrentView()) return;
                NativeUiPresentation.Activate(action, NativeBookTheme.PlayClick, ReportAttachment);
            });
            Transform sourceIcon = source.transform.Find("Button");
            Image sourceIconImage = sourceIcon == null ? null : sourceIcon.GetComponent<Image>();
            if (sourceIconImage == null)
                throw new InvalidOperationException("Native score-arrow icon is unavailable.");
            GameObject iconObject = NewUiObject("ArrowIcon", owned.layer);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            RectTransform sourceRect = sourceIcon as RectTransform;
            iconRect.SetParent(owned.transform, false);
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            float iconExtent = Mathf.Min(26f, Mathf.Max(18f,
                Mathf.Min(sourceRect.rect.width, sourceRect.rect.height)));
            iconRect.sizeDelta = new Vector2(iconExtent, iconExtent);
            // The right-hand native donor points upward; reverse it for MoveDown.
            iconRect.localRotation = sourceRect.localRotation * Quaternion.Euler(0f, 0f, 180f);
            Image icon = iconObject.AddComponent<Image>();
            NativeBookTheme.CopyImage(sourceIconImage, icon);
            icon.raycastTarget = false;
            AddInlineTooltip(owned, nativeText, tooltip);
            owned.SetActive(false);
            inlineOwnedArrows.Add(owned);
            return button;
        }

        private void AddInlineTooltip(GameObject arrow, TextMeshProUGUI nativeText, string text,
            float width = 190f, float height = 30f)
        {
            GameObject tooltip = NewUiObject("Tooltip", arrow.layer);
            RectTransform rect = tooltip.GetComponent<RectTransform>();
            rect.SetParent(arrow.transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 3f);
            rect.sizeDelta = new Vector2(width, height);
            Image background = tooltip.AddComponent<Image>();
            background.color = Parchment;
            background.raycastTarget = false;
            TextMeshProUGUI label = CreateLabel(tooltip.transform, text, nativeText,
                layout.StatusFontSize, TextAlignmentOptions.Center, -1f, -1f, BodyText, true);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(3f, 0f);
            labelRect.offsetMax = new Vector2(-3f, 0f);
            var hover = arrow.AddComponent<InlineArrowTooltip>();
            hover.Configure(tooltip);
            tooltip.SetActive(false);
        }

        private void CreateBadgeOverlay(MonoBehaviour allocator, KingmakerContracts contracts)
        {
            badgeNativeCaption = contracts.AbilityAllocatorPointsCaptionField.GetValue(allocator) as TextMeshProUGUI;
            badgeNativeValue = contracts.AbilityAllocatorPointsValueField.GetValue(allocator) as TextMeshProUGUI;
            Image frame = contracts.AbilityAllocatorFrameField.GetValue(allocator) as Image;
            if (badgeNativeCaption == null || badgeNativeValue == null || frame == null ||
                badgeNativeCaption.transform.parent != badgeNativeValue.transform.parent.parent ||
                !badgeNativeCaption.transform.IsChildOf(allocator.transform) ||
                !badgeNativeValue.transform.IsChildOf(allocator.transform) ||
                !frame.transform.IsChildOf(allocator.transform))
                throw new InvalidOperationException("The ability allocator's exact Points badge is unavailable.");
            badgeCaptionOriginallyEnabled = badgeNativeCaption.enabled;
            badgeValueOriginallyEnabled = badgeNativeValue.enabled;
            badgeOwnedCaption = CreateBadgeText(badgeNativeCaption, "Value", BodyText);
            badgeOwnedValue = CreateBadgeText(badgeNativeValue, "—", BodyText);
            badgeActiveBackground = NewUiObject("RolledValueBackground", frame.gameObject.layer);
            RectTransform tintRect = badgeActiveBackground.GetComponent<RectTransform>();
            tintRect.SetParent(frame.transform, false);
            tintRect.anchorMin = Vector2.zero;
            tintRect.anchorMax = Vector2.one;
            tintRect.offsetMin = tintRect.offsetMax = Vector2.zero;
            tintRect.SetAsFirstSibling();
            Image tint = badgeActiveBackground.AddComponent<Image>();
            NativeBookTheme.CopyImage(frame, tint);
            tint.color = new Color(0.725f, 0.408f, 0.408f, 1f);
            tint.raycastTarget = false;
            badgeActiveBackground.SetActive(false);
        }

        private static TextMeshProUGUI CreateBadgeText(TextMeshProUGUI source, string value, Color color)
        {
            GameObject owned = NewUiObject("Owned" + value + "BadgeText", source.gameObject.layer);
            RectTransform rect = owned.GetComponent<RectTransform>();
            rect.SetParent(source.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            TextMeshProUGUI text = owned.AddComponent<TextMeshProUGUI>();
            NativeBookTheme.CopyText(source, text);
            text.text = value;
            text.color = color;
            text.raycastTarget = false;
            owned.SetActive(false);
            return text;
        }

        private void ShowValueBadge(bool show, RollUiSnapshot snapshot)
        {
            if (badgeNativeCaption == null || badgeNativeValue == null) return;
            if (show)
            {
                badgeNativeCaption.enabled = false;
                badgeNativeValue.enabled = false;
                badgeOwnedCaption.gameObject.SetActive(true);
                badgeOwnedValue.gameObject.SetActive(true);
                badgeActiveBackground.SetActive(true);
                badgeOwnedValue.text = snapshot.Mode == RollSessionMode.Roll && snapshot.AssignedValues != null
                    ? snapshot.Total.ToString(CultureInfo.InvariantCulture)
                    : "—";
            }
            else
            {
                badgeOwnedCaption.gameObject.SetActive(false);
                badgeOwnedValue.gameObject.SetActive(false);
                badgeActiveBackground.SetActive(false);
                badgeNativeCaption.enabled = badgeCaptionOriginallyEnabled;
                badgeNativeValue.enabled = badgeValueOriginallyEnabled;
            }
        }

        private void CreatePresetPopup(TextMeshProUGUI nativeText, Button nativeButton)
        {
            presetBlocker = NewUiObject("PresetDismissBlocker", root.layer);
            RectTransform blockerRect = presetBlocker.GetComponent<RectTransform>();
            blockerRect.SetParent(root.transform, false);
            blockerRect.anchorMin = Vector2.zero;
            blockerRect.anchorMax = Vector2.one;
            blockerRect.offsetMin = blockerRect.offsetMax = Vector2.zero;
            Image blockerImage = presetBlocker.AddComponent<Image>();
            blockerImage.color = Color.clear;
            blockerImage.raycastTarget = true;
            Button blockerButton = presetBlocker.AddComponent<Button>();
            blockerButton.transition = Selectable.Transition.None;
            blockerButton.onClick = new Button.ButtonClickedEvent();
            blockerButton.onClick.AddListener(ClosePresetPopup);

            presetPopup = CreateVertical("FormulaPresets", root.transform);
            var popupRect = presetPopup.GetComponent<RectTransform>();
            popupRect.anchorMin = popupRect.anchorMax = new Vector2(0.5f, 0.5f);
            popupRect.pivot = new Vector2(0f, 1f);
            popupRect.sizeDelta = new Vector2(450f, 236f);
            Image popupImage = presetPopup.AddComponent<Image>();
            popupImage.color = Parchment;
            popupImage.raycastTarget = true;
            themeBindings.Add(NativeThemeCapability.Paper,
                donors => NativeBookTheme.CopyImage((Image)donors[0], popupImage),
                () => { popupImage.sprite = null; popupImage.color = Parchment; });
            VerticalLayoutGroup group = presetPopup.GetComponent<VerticalLayoutGroup>();
            group.padding = new RectOffset(10, 10, 10, 10);
            group.spacing = 2f;
            foreach (DiceRollPreset preset in Enum.GetValues(typeof(DiceRollPreset)))
            {
                DiceRollPreset choice = preset;
                string caption = choice == DiceRollPreset.CustomExpression
                    ? "Custom formula… — Edit the field"
                    : DiceRollRule.ForPreset(choice, LowScorePolicy.Tabletop).Expression + " — " +
                        PresetShortDescription(choice);
                inlinePresetChoices.Add(CreateButton(presetPopup.transform, caption, nativeText,
                    nativeButton, -1f, () => SelectInlinePreset(choice), 34f));
            }
            presetBlocker.SetActive(false);
            presetPopup.SetActive(false);
        }

        private static string PresetShortDescription(DiceRollPreset choice)
        {
            switch (choice)
            {
                case DiceRollPreset.FourD6DropLowest: return "Drop lowest";
                case DiceRollPreset.FourD6RerollOnesDropLowest: return "Reroll ones";
                case DiceRollPreset.ThreeD6: return "Three dice";
                case DiceRollPreset.TwoD6PlusSix: return "Two dice + six";
                case DiceRollPreset.OneD20: return "One d20";
                default: return string.Empty;
            }
        }

        private void OpenRollWorkspace()
        {
            if (commands.Snapshot.Mode != RollSessionMode.PointBuy) return;
            workspaceOpen = true;
            commandError = null;
            Render(contractsProvider());
        }

        private void ReturnFromWorkspace()
        {
            if (commands.Snapshot.Mode == RollSessionMode.PointBuy)
            {
                workspaceOpen = false;
                inlineMenu = InlineMenuKind.None;
                Render(contractsProvider());
                // Re-observe native point-buy availability after the view-only
                // Roll-ready state, including race-dependent button admission.
                if (attachedAllocator != null)
                {
                    try { contractsProvider().AbilityAllocatorFillDataMethod.Invoke(attachedAllocator, null); }
                    catch (Exception exception)
                    {
                        logger.Warning("Native Point Buy refresh failed: " + exception.Message);
                    }
                }
                return;
            }
            Execute(RollUiCommand.ReturnToPointBuy);
        }

        private void ExecuteGeneration()
        {
            if (!CommitFormulaDraft()) return;
            RollSessionMode mode = commands.Snapshot.Mode;
            Execute(mode == RollSessionMode.PointBuy ? RollUiCommand.Roll : RollUiCommand.Reroll);
        }

        private bool CommitFormulaDraft()
        {
            if (!CanDispatchCurrentView() || formulaDraft == null) return false;
            string error;
            if (!formulaDraft.Validate(commands.Snapshot.Configuration, out error))
            {
                commandError = error;
                Render(contractsProvider());
                return false;
            }
            if (formulaDraft.HasUncommittedEdit)
            {
                if (!commands.SelectCustomExpression(formulaDraft.Text, out error))
                {
                    commandError = error;
                    Render(contractsProvider());
                    return false;
                }
                formulaDraft.CommitCustom();
            }
            commandError = null;
            Render(contractsProvider());
            return true;
        }

        private void ClosePresetPopup()
        {
            if (inlineMenu != InlineMenuKind.Presets) return;
            inlineMenu = InlineMenuKind.None;
            Render(contractsProvider());
            if (inlinePresetButton != null) inlinePresetButton.Select();
        }

        private void SetNativeButtonPresentation(bool hide, bool rollReady)
        {
            if (hide)
            {
                foreach (NativeButtonVisualState state in nativeRowVisuals)
                {
                    if (!nativeVisualsHidden) state.Conceal(rollReady);
                    else state.KeepConcealed(rollReady);
                }
                nativeVisualsHidden = true;
                if (rollReady) readyControlsSuppressed = true;
            }
            else
            {
                RestoreNativeButtonPresentation();
            }
        }

        private void RestoreNativeButtonPresentation()
        {
            if (!nativeVisualsHidden) return;
            foreach (NativeButtonVisualState state in nativeRowVisuals)
                state.Restore(readyControlsSuppressed);
            nativeVisualsHidden = false;
            readyControlsSuppressed = false;
        }

        private void ReleaseRollReadyBeforeApplication(RollUiCommand command)
        {
            if (commands.Snapshot.Mode != RollSessionMode.PointBuy || !workspaceOpen) return;
            if (command != RollUiCommand.Roll && command != RollUiCommand.PreviousHistory &&
                command != RollUiCommand.NextHistory && command != RollUiCommand.RecallSaved &&
                command != RollUiCommand.UseHistory) return;
            RestoreNativeButtonPresentation();
            ShowValueBadge(false, commands.Snapshot);
        }

        private void DestroyInlineView()
        {
            RestoreNativeButtonPresentation();
            if (badgeNativeCaption != null) badgeNativeCaption.enabled = badgeCaptionOriginallyEnabled;
            if (badgeNativeValue != null) badgeNativeValue.enabled = badgeValueOriginallyEnabled;
            if (badgeOwnedCaption != null) badgeOwnedCaption.gameObject.SetActive(false);
            if (badgeOwnedValue != null) badgeOwnedValue.gameObject.SetActive(false);
            if (badgeActiveBackground != null) badgeActiveBackground.SetActive(false);
            if (badgeOwnedCaption != null) Object.Destroy(badgeOwnedCaption.gameObject);
            if (badgeOwnedValue != null) Object.Destroy(badgeOwnedValue.gameObject);
            if (badgeActiveBackground != null) Object.Destroy(badgeActiveBackground);
            badgeNativeCaption = badgeNativeValue = badgeOwnedCaption = badgeOwnedValue = null;
            badgeActiveBackground = null;
            nativeRowVisuals.Clear();
            foreach (GameObject arrow in inlineOwnedArrows)
                if (arrow != null) { arrow.SetActive(false); Object.Destroy(arrow); }
            inlineOwnedArrows.Clear();
            inlineDownArrows.Clear();
            inlinePresetChoices.Clear();
            if (presetPopup != null) { presetPopup.SetActive(false); Object.Destroy(presetPopup); }
            if (presetBlocker != null) { presetBlocker.SetActive(false); Object.Destroy(presetBlocker); }
            presetPopup = null;
            presetBlocker = null;
            if (inlineToolbar != null)
            {
                RectTransform allocatorRect = inlineToolbar.transform.parent as RectTransform;
                inlineToolbar.SetActive(false);
                Object.Destroy(inlineToolbar);
                if (allocatorRect != null) LayoutRebuilder.ForceRebuildLayoutImmediate(allocatorRect);
            }
            inlineToolbar = null;
            inlinePointBuyEntry = null;
            inlineWorkspace = null;
            inlinePolicyNote = null;
            policyNoteLabel = null;
            formulaInput = null;
            inlineEntryButton = null;
            inlinePreviousButton = null;
            inlineNextButton = null;
            inlineStatus = null;
            inlineStatusLabel = null;
            inlinePresetButton = null;
            inlineRollButton = null;
            inlineRerollButton = null;
            inlineSaveButton = null;
            inlineLoadButton = null;
            inlineReturnButton = null;
            inlineAvailable = false;
            inlineMenu = InlineMenuKind.None;
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
            menuHeadingLabel = CreateLabel(
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
                    inlineMenu = InlineMenuKind.None;
                    if (inlineAvailable && inlinePresetButton != null) inlinePresetButton.Select();
                    else if (accessTab != null) accessTab.GetComponent<Button>().Select();
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
            presetChoices = CreateVertical("PresetChoices", content);
            foreach (DiceRollPreset preset in Enum.GetValues(typeof(DiceRollPreset)))
            {
                DiceRollPreset choice = preset;
                CreateButton(presetChoices.transform, RollPanelPresenter.FormatPreset(choice),
                    nativeText, nativeButton, -1f, () => SelectInlinePreset(choice));
            }
            SetVisible(presetChoices, false);
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
            presetSelectorSection = CreateCaptionedSelector(
                content,
                "Roll method",
                nativeText,
                nativeButton,
                RollUiCommand.PreviousPreset,
                RollUiCommand.NextPreset,
                out presetLabel);

            pointActionsRow = CreateHorizontal(content, layout.OrdinaryControlHeight);
            rollButton = CreateButton(pointActionsRow.transform, "Roll", nativeText, nativeButton, 120f,
                () => Execute(RollUiCommand.Roll));
            rerollButton = CreateButton(pointActionsRow.transform, "Reroll", nativeText, nativeButton, 120f,
                () => Execute(RollUiCommand.Reroll));
            // Compact profiles cannot hold the whole point-actions row next to a
            // fitted caption; the Return control reflows onto this spare row.
            returnRow = CreateHorizontal(content, layout.OrdinaryControlHeight);
            returnRow.name = "ReturnRow";
            SetVisible(returnRow, false);
            returnButton = CreateButton(pointActionsRow.transform, "Return to Point Buy", nativeText, nativeButton, 210f,
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
            advancedDisclosureLayout = advancedDisclosure.GetComponent<LayoutElement>();
            advancedLabel = advancedButton.GetComponentInChildren<TextMeshProUGUI>();

            advancedContent = CreateVertical("AdvancedContent", content);
            policySelectorSection = CreateCaptionedSelector(
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
                if (!rendering && CanDispatchCurrentView())
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

        private GameObject CreateCaptionedSelector(
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
            TextMeshProUGUI captionLabel = CreateLabel(
                section.transform,
                caption,
                nativeText,
                layout.BodyFontSize,
                TextAlignmentOptions.Left,
                layout.OrdinaryControlHeight,
                108f,
                BodyText,
                true);
            // Captions such as "Low-score rule" reserve their full styled width
            // instead of ellipsizing inside the design column.
            BindLabelCaptionFit(captionLabel, 108f);
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
            return section;
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
                // Both controls of a pair share one fitted width so the six rows stay aligned.
                UnifyButtonWidths(up, down);
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
            historyDisclosureLayout = historyDisclosure.GetComponent<LayoutElement>();
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
            savedDisclosureLayout = savedDisclosure.GetComponent<LayoutElement>();
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
            LayoutElement element = accessTab.GetComponent<LayoutElement>();
            accessTabLayout = element;
            // The tab is positioned by rect, not by a layout group; keep the rect
            // and the placement input aligned with the caption-fitted width.
            accessTabFittedWidth = element.minWidth;
            RectTransform rect = accessTab.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(accessTabFittedWidth, layout.AccessTabHeight);
            element.preferredHeight = layout.AccessTabHeight;
            element.minHeight = layout.AccessTabHeight;
        }

        private void PositionAccessTab(object allocator, KingmakerContracts contracts)
        {
            RectTransform tabRect = accessTab == null ? null : accessTab.GetComponent<RectTransform>();
            RectTransform rootRect = root == null ? null : root.GetComponent<RectTransform>();
            if (tabRect == null || rootRect == null || allocator == null || contracts == null) return;
            if (accessTabLayout != null && accessTabFittedWidth > 0f &&
                Mathf.Abs(accessTabLayout.minWidth - accessTabFittedWidth) > 0.01f)
            {
                // A later theme refit changed the fitted caption width.
                accessTabFittedWidth = accessTabLayout.minWidth;
                tabRect.sizeDelta = new Vector2(accessTabFittedWidth, layout.AccessTabHeight);
            }
            float tabWidth = accessTabFittedWidth > 0f ? accessTabFittedWidth : layout.AccessTabWidth;

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
                tabWidth,
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
            if (inlineAvailable)
            {
                if (lastInlineMode.HasValue && lastInlineMode.Value != RollSessionMode.PointBuy &&
                    snapshot.Mode == RollSessionMode.PointBuy) workspaceOpen = false;
                if (snapshot.Mode != RollSessionMode.PointBuy) workspaceOpen = true;
                lastInlineMode = snapshot.Mode;
                RollPanelModel inlineModel = presenter.Present(
                    snapshot, panelState.Disclosure, RollPanelPresentationProfile.Compact);
                rendering = true;
                try
                {
                    ApplyModel(inlineModel);
                    ApplyInlineModel(inlineModel, snapshot);
                    ApplySurfaceState();
                    if (panelState.ExpandedSurfaceActive) ApplyInlineMenuGeometry();
                    if (inlineMenu == InlineMenuKind.Presets) ApplyPresetPopupGeometry();
                }
                finally { rendering = false; }
                if (snapshot.Mode == RollSessionMode.Roll ||
                    snapshot.Mode == RollSessionMode.EnteringRollMode)
                {
                    string inlineError;
                    if (!nativeControls.TrySuppressForRoll(commands.ActiveSession, contracts, out inlineError))
                        logger.Warning("Native Roll Mode control suppression failed: " + inlineError);
                }
                return;
            }
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
            SetDisclosureCaption(advancedLabel, advancedDisclosureLayout, model.AdvancedLabel);
            SetVisible(minimumSection, model.MinimumVisible);
            SetVisible(customSection, model.CustomVisible);
            if (customInput.text != model.CustomExpression) customInput.text = model.CustomExpression;

            SetVisible(rollButton.gameObject, model.RollVisible);
            SetVisible(rerollButton.gameObject, model.RerollVisible);
            SetVisible(returnButton.gameObject, model.ReturnToPointBuyVisible);
            if (returnRow != null && returnButton.transform.parent == returnRow.transform)
            {
                // In the reflowed compact layout the spare row tracks its control.
                SetVisible(returnRow, model.ReturnToPointBuyVisible);
            }
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
            SetDisclosureCaption(historyDisclosureLabel, historyDisclosureLayout, model.HistoryDisclosureLabel);
            SetVisible(historyDetails, model.HistoryDetailsVisible);
            historyLabel.text = "History   " + model.History;
            SetInteractable(useHistoryButton, model.CanUseHistory);

            SetVisible(savedDisclosure, model.SavedDisclosureVisible);
            SetDisclosureCaption(savedDisclosureLabel, savedDisclosureLayout, model.SavedDisclosureLabel);
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

        // Compact content width cannot hold Roll, Reroll and a fitted
        // "Return to Point Buy" caption in one row; the Return control moves
        // to its spare row there while Wide keeps the single original row.
        private void ApplyPointActionsProfile(RollPanelPresentationProfile profile)
        {
            if (returnButton == null || pointActionsRow == null || returnRow == null) return;
            Transform target = profile == RollPanelPresentationProfile.Compact
                ? returnRow.transform
                : pointActionsRow.transform;
            if (returnButton.transform.parent != target)
            {
                returnButton.transform.SetParent(target, false);
                // Row structure changed; force a body-height re-measure.
                lastLayoutModelKey = null;
            }
            SetVisible(returnRow,
                profile == RollPanelPresentationProfile.Compact && returnButton.gameObject.activeSelf);
        }

        private void ApplyResponsiveGeometry(ResponsiveRollPanelLayoutResult result)
        {
            ApplyPointActionsProfile(result.Profile);
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
            if (accessTab != null) SetVisible(accessTab, !inlineAvailable && panelState.AccessTabActive);
        }

        private void ApplyInlineModel(RollPanelModel model, RollUiSnapshot snapshot)
        {
            bool transitional = snapshot.Mode == RollSessionMode.EnteringRollMode ||
                snapshot.Mode == RollSessionMode.RestoringPointBuy;
            bool showWorkspace = workspaceOpen;
            SetVisible(inlinePointBuyEntry, !showWorkspace);
            SetVisible(inlineWorkspace, showWorkspace);
            SetInteractable(inlineEntryButton, snapshot.Mode == RollSessionMode.PointBuy);
            SetInteractable(inlinePresetButton, !transitional);
            bool rolled = snapshot.Mode == RollSessionMode.Roll;
            SetVisible(inlineRollButton.gameObject, !rolled);
            SetVisible(inlineRerollButton.gameObject, rolled);
            SetInteractable(inlineRollButton, snapshot.CanRoll && formulaDraft.Error == null);
            SetInteractable(inlineRerollButton, snapshot.CanReroll && formulaDraft.Error == null);
            SetInteractable(inlineSaveButton, snapshot.CanStore);
            SetInteractable(inlineLoadButton, snapshot.CanRecall);
            RollSession activeSession = commands.ActiveSession;
            int adjacent;
            SetInteractable(inlinePreviousButton, activeSession != null && !transitional &&
                activeSession.History.AdjacentActive(true, !rolled, out adjacent) != null);
            SetInteractable(inlineNextButton, activeSession != null && rolled && !transitional &&
                activeSession.History.AdjacentActive(false, false, out adjacent) != null);
            SetInteractable(inlineReturnButton, !transitional &&
                (snapshot.Mode == RollSessionMode.PointBuy || snapshot.CanReturnToPointBuy));
            for (int index = 0; index < inlineDownArrows.Count; index++)
            {
                bool visible = model.AssignmentVisible;
                SetVisible(inlineDownArrows[index].gameObject, visible);
                SetInteractable(inlineDownArrows[index], visible &&
                    model.AssignmentRows[index].CanMoveDown);
            }
            SetNativeButtonPresentation(showWorkspace, snapshot.Mode == RollSessionMode.PointBuy);
            ShowValueBadge(showWorkspace, snapshot);
            SetVisible(presetBlocker, showWorkspace && inlineMenu == InlineMenuKind.Presets);
            SetVisible(presetPopup, showWorkspace && inlineMenu == InlineMenuKind.Presets);
            DiceRollPreset selectedPreset = formulaDraft.CustomIntent
                ? DiceRollPreset.CustomExpression : snapshot.Configuration.Preset;
            for (int index = 0; index < inlinePresetChoices.Count; index++)
            {
                Button choice = inlinePresetChoices[index];
                ColorBlock colors = choice.colors;
                colors.normalColor = index == (int)selectedPreset
                    ? new Color(1f, 0.83f, 0.68f, 1f) : Color.white;
                choice.colors = colors;
            }
            string policy = RollPanelPresenter.FormatPolicy(snapshot.Configuration.LowScorePolicy);
            bool policyActive = snapshot.Configuration.MinimumApplies;
            policyNoteLabel.text = policyActive
                ? policy + " below " + snapshot.Configuration.MinimumScore + " (change in Mod Manager settings)"
                : "";
            SetVisible(inlinePolicyNote, showWorkspace && policyActive);

            string status = !string.IsNullOrWhiteSpace(formulaDraft.Error) ? formulaDraft.Error :
                !string.IsNullOrWhiteSpace(model.Error) ? model.Error : commandError;
            switch (commands.LastPointBuyReturn)
            {
                case PointBuyReturnOutcome.DerivedValuesNotRefreshed:
                    status = "Point Buy restored; derived values need a refresh. Check Troubleshooting.";
                    break;
                case PointBuyReturnOutcome.PageNotRefreshed:
                    status = "Point Buy restored; reopen this page if scores look stale.";
                    break;
                case PointBuyReturnOutcome.Failed:
                    status = "Return to Point Buy failed. Try again or check Troubleshooting.";
                    break;
            }
            if (string.IsNullOrWhiteSpace(status) && model.Status != null &&
                model.Status.StartsWith("Stored ", StringComparison.Ordinal)) status = model.Status;
            inlineStatusLabel.text = status ?? string.Empty;
            bool returnNotice = commands.LastPointBuyReturn == PointBuyReturnOutcome.DerivedValuesNotRefreshed ||
                commands.LastPointBuyReturn == PointBuyReturnOutcome.PageNotRefreshed ||
                commands.LastPointBuyReturn == PointBuyReturnOutcome.Failed;
            SetVisible(inlineStatus, (showWorkspace || returnNotice) &&
                !string.IsNullOrWhiteSpace(status));
            float height = showWorkspace ? 190f + (policyActive ? 32f : 0f) +
                (inlineStatus.activeSelf ? 32f : 0f) :
                42f + (inlineStatus.activeSelf ? 32f : 0f);
            inlineToolbar.GetComponent<LayoutElement>().preferredHeight = height;
            inlineToolbar.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical, height);

            // The old score list and ordinary actions are retained only for
            // degraded attachment. A healthy inline view uses this surface as
            // one bounded secondary menu at a time.
            SetVisible(presetSelectorSection, false);
            SetVisible(pointActionsRow, false);
            SetVisible(returnRow, false);
            SetVisible(assignmentSection, false);
            SetVisible(advancedDisclosure, false);
            SetVisible(historyDisclosure, false);
            SetVisible(savedDisclosure, false);
            SetVisible(presetChoices, false);
            SetVisible(advancedContent, false);
            SetVisible(summarySection, false);
            SetVisible(historyDetails, false);
            SetVisible(savedDetails, inlineMenu == InlineMenuKind.Load);
            SetVisible(storeButton.gameObject, false);
            if (inlineMenu == InlineMenuKind.Load)
            {
                savedLabel.text = "Saved arrays   " + model.Saved;
            }
            SetVisible(messageLabel.gameObject, inlineMenu == InlineMenuKind.Load &&
                !string.IsNullOrWhiteSpace(messageLabel.text));
            menuHeadingLabel.text = "Load Scores";
        }

        private void ApplyInlineMenuGeometry()
        {
            RectTransform rootRect = root.GetComponent<RectTransform>();
            RectTransform toolbarRect = inlineToolbar.GetComponent<RectTransform>();
            LocalLayoutRect? toolbarBounds = TryGetLocalBounds(toolbarRect, rootRect);
            if (!toolbarBounds.HasValue) return;
            float width = Mathf.Min(460f, rootRect.rect.width - 36f);
            float height = 360f;
            height = Mathf.Min(height, rootRect.rect.height - 110f);
            if (width <= 0f || height <= 0f) return;
            expandedSurfaceRect.anchorMin = expandedSurfaceRect.anchorMax = new Vector2(0.5f, 0.5f);
            expandedSurfaceRect.pivot = new Vector2(0f, 0f);
            expandedSurfaceRect.sizeDelta = new Vector2(width, height);
            expandedSurfaceRect.anchoredPosition = new Vector2(
                Mathf.Clamp(toolbarBounds.Value.XMin, rootRect.rect.xMin + 18f,
                    rootRect.rect.xMax - width - 18f),
                Mathf.Clamp(toolbarBounds.Value.YMax + 4f, rootRect.rect.yMin + 92f,
                    rootRect.rect.yMax - height - 18f));
            foreach (RectTransform paper in paperLayers)
                paper.sizeDelta = new Vector2(width * 2f, height * 2f);
            LayoutRebuilder.ForceRebuildLayoutImmediate(expandedSurfaceRect);
            float preferred = LayoutUtility.GetPreferredHeight(bodyContent);
            bodyScroll.vertical = preferred > bodyViewport.rect.height + 2f;
            SetVisible(bodyScrollbarObject, bodyScroll.vertical);
            bodyViewport.offsetMax = new Vector2(bodyScroll.vertical ? -22f : 0f, 0f);
        }

        private void ApplyPresetPopupGeometry()
        {
            RectTransform rootRect = root.GetComponent<RectTransform>();
            RectTransform fieldRect = formulaInput.GetComponent<RectTransform>();
            RectTransform popupRect = presetPopup.GetComponent<RectTransform>();
            LocalLayoutRect? field = TryGetLocalBounds(fieldRect, rootRect);
            if (!field.HasValue) return;
            float width = Mathf.Min(450f, rootRect.rect.width - 36f);
            LayoutRebuilder.ForceRebuildLayoutImmediate(popupRect);
            float height = Mathf.Max(234f, LayoutUtility.GetPreferredHeight(popupRect));
            popupRect.sizeDelta = new Vector2(width, height);
            float left = Mathf.Clamp(field.Value.XMin, rootRect.rect.xMin + 18f,
                rootRect.rect.xMax - width - 18f);
            float below = field.Value.YMin - 4f;
            float top = below - height >= rootRect.rect.yMin + 18f
                ? below : field.Value.YMax + height + 4f <= rootRect.rect.yMax - 18f
                    ? field.Value.YMax + height + 4f
                    : Mathf.Clamp(below, rootRect.rect.yMin + height + 18f,
                        rootRect.rect.yMax - 18f);
            popupRect.anchoredPosition = new Vector2(left, top);
            presetBlocker.transform.SetAsLastSibling();
            presetPopup.transform.SetAsLastSibling();
        }

        private bool CanDispatchCurrentView()
        {
            RollSession session = commands.ActiveSession;
            if (session == null || viewBinding == null ||
                !commands.CanAttachNativePanel || attachedAllocator == null || root == null)
                return false;
            object owner;
            bool active;
            object phase;
            object allocator;
            try
            {
                KingmakerContracts contracts = contractsProvider();
                return contracts != null && contracts.TryGetAbilityPhasePresentationContext(
                    out owner, out active, out phase, out allocator) &&
                    viewBinding.Matches(session.Controller, session.StableOwner, allocator, active);
            }
            catch (Exception exception)
            {
                ReportAttachment("Native UI command binding unavailable: " + exception.Message);
                return false;
            }
        }

        private void OpenInlineMenu(InlineMenuKind menu)
        {
            if (!inlineAvailable) return;
            if (inlineMenu == menu)
            {
                if (menu == InlineMenuKind.Load)
                    NativeUiPresentation.CloseDrawer(panelState, commands.NotifyDrawerClosed);
                inlineMenu = InlineMenuKind.None;
            }
            else
            {
                if (inlineMenu == InlineMenuKind.Load)
                    NativeUiPresentation.CloseDrawer(panelState, commands.NotifyDrawerClosed);
                inlineMenu = menu;
                if (menu == InlineMenuKind.Load) panelState.Open();
            }
            Render(contractsProvider());
            if (menu == InlineMenuKind.Load && panelState.IsExpanded && closeButton != null)
                closeButton.Select();
        }

        private void SelectInlinePreset(DiceRollPreset preset)
        {
            string error = null;
            bool succeeded = preset == DiceRollPreset.CustomExpression ||
                commands.SelectPreset(preset, out error);
            if (preset == DiceRollPreset.CustomExpression) error = null;
            commandError = succeeded ? null : error;
            if (succeeded && preset != DiceRollPreset.CustomExpression)
                formulaDraft.SelectPreset(preset, commands.Snapshot.Configuration);
            inlineMenu = InlineMenuKind.None;
            Render(contractsProvider());
            if (succeeded && formulaInput != null)
            {
                if (preset != DiceRollPreset.CustomExpression)
                {
                    rendering = true;
                    try { formulaInput.text = formulaDraft.Text; }
                    finally { rendering = false; }
                }
                formulaInput.Select();
                if (preset == DiceRollPreset.CustomExpression) formulaInput.ActivateInputField();
            }
        }

        private void Execute(RollUiCommand command, AbilityScore ability = AbilityScore.Strength)
        {
            ReleaseRollReadyBeforeApplication(command);
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
            button.onClick.AddListener(() =>
            {
                if (!CanDispatchCurrentView()) return;
                NativeUiPresentation.Activate(action, NativeBookTheme.PlayClick, ReportAttachment);
            });

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
            BindButtonCaptionFit(button, label, width, resolvedHeight);
            return button;
        }

        // preferredWidth/preferredHeight are unconstrained and computed from the
        // label's live font, style and spacings, so the fit adapts to whichever
        // styling is active without measuring truncated rendered text.
        private static float MeasuredCaptionExtent(float extent)
        {
            return float.IsNaN(extent) || float.IsInfinity(extent) ? 0f : Mathf.Max(0f, extent);
        }

        private static void FitButtonCaption(Button button, TextMeshProUGUI label, float designWidth, float designHeight)
        {
            if (button == null || label == null || label.font == null) return;
            LayoutElement element = button.GetComponent<LayoutElement>();
            if (element == null) return;
            float width = ButtonCaptionFit.ResolveExtent(
                designWidth,
                ButtonCaptionFit.RequiredWidth(MeasuredCaptionExtent(label.preferredWidth), CaptionHorizontalInset));
            float height = ButtonCaptionFit.ResolveExtent(
                designHeight,
                ButtonCaptionFit.RequiredHeight(MeasuredCaptionExtent(label.preferredHeight), CaptionVerticalInset));
            if (designWidth > 0f) element.preferredWidth = width;
            element.minWidth = width;
            element.preferredHeight = height;
            element.minHeight = height;
        }

        private static float RequiredButtonWidth(TextMeshProUGUI label)
        {
            if (label == null || label.font == null) return 0f;
            return ButtonCaptionFit.RequiredWidth(MeasuredCaptionExtent(label.preferredWidth), CaptionHorizontalInset);
        }

        // Keeps a paired control set (assignment Up/Down) at one shared width so
        // rows stay aligned while both captions fit.
        private static void UnifyButtonWidths(params Button[] buttons)
        {
            if (buttons == null || buttons.Length == 0) return;
            float unified = 0f;
            foreach (Button button in buttons)
                unified = ButtonCaptionFit.UnifyPair(unified, RequiredButtonWidth(button.GetComponentInChildren<TextMeshProUGUI>()));
            foreach (Button button in buttons)
            {
                LayoutElement element = button == null ? null : button.GetComponent<LayoutElement>();
                if (element == null) continue;
                float width = ButtonCaptionFit.ResolveExtent(element.preferredWidth, unified);
                element.preferredWidth = width;
                element.minWidth = width;
            }
        }

        private void BindButtonCaptionFit(Button button, TextMeshProUGUI label, float designWidth, float designHeight)
        {
            // Styled text metrics change with theme application and fallback
            // restoration; re-fit on those capability changes and once now.
            themeBindings.Add(NativeThemeCapability.ButtonText,
                donors => FitButtonCaption(button, label, designWidth, designHeight),
                () => FitButtonCaption(button, label, designWidth, designHeight));
            FitButtonCaption(button, label, designWidth, designHeight);
        }

        private static void FitLabelCaptionWidth(TextMeshProUGUI label, float designMinimum)
        {
            if (label == null || label.font == null) return;
            LayoutElement element = label.GetComponent<LayoutElement>();
            if (element == null) return;
            float width = ButtonCaptionFit.ResolveExtent(
                designMinimum,
                ButtonCaptionFit.RequiredWidth(MeasuredCaptionExtent(label.preferredWidth), 0f));
            element.preferredWidth = width;
            element.minWidth = width;
        }

        private void BindLabelCaptionFit(TextMeshProUGUI label, float designMinimum)
        {
            themeBindings.Add(NativeThemeCapability.Body,
                donors => FitLabelCaptionWidth(label, designMinimum),
                () => FitLabelCaptionWidth(label, designMinimum));
            FitLabelCaptionWidth(label, designMinimum);
        }

        private void SetDisclosureCaption(TextMeshProUGUI label, LayoutElement element, string text)
        {
            string value = text ?? string.Empty;
            if (label.text == value) return;
            label.text = value;
            // Measure only when the caption changed; counts grow, so keeping the
            // widest seen minimum avoids per-frame measurement and layout jitter.
            if (element != null && label.font != null)
                element.minWidth = ButtonCaptionFit.UnifyPair(
                    element.minWidth,
                    ButtonCaptionFit.RequiredWidth(MeasuredCaptionExtent(label.preferredWidth), 0f));
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

        private sealed class NativeButtonVisualState
        {
            private readonly Button button;
            private readonly Graphic[] graphics;
            private readonly bool[] enabled;
            private readonly bool[] raycast;
            private readonly Navigation navigation;
            private bool readyInteractable;

            internal NativeButtonVisualState(Button button)
            {
                this.button = button ?? throw new ArgumentNullException(nameof(button));
                graphics = button.GetComponentsInChildren<Graphic>(true);
                enabled = new bool[graphics.Length];
                raycast = new bool[graphics.Length];
                navigation = button.navigation;
                for (int index = 0; index < graphics.Length; index++)
                {
                    enabled[index] = graphics[index].enabled;
                    raycast[index] = graphics[index].raycastTarget;
                }
            }

            internal void Conceal(bool rollReady)
            {
                if (rollReady) readyInteractable = button.interactable;
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                if (rollReady) button.interactable = false;
                foreach (Graphic graphic in graphics)
                {
                    if (graphic == null) continue;
                    graphic.enabled = false;
                    graphic.raycastTarget = false;
                }
            }

            internal void KeepConcealed(bool rollReady)
            {
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                if (rollReady) button.interactable = false;
                foreach (Graphic graphic in graphics)
                {
                    if (graphic == null) continue;
                    graphic.enabled = false;
                    graphic.raycastTarget = false;
                }
            }

            internal void Restore(bool restoreReadyInteractable)
            {
                if (button == null) return;
                button.navigation = navigation;
                if (restoreReadyInteractable) button.interactable = readyInteractable;
                for (int index = 0; index < graphics.Length; index++)
                {
                    if (graphics[index] == null) continue;
                    graphics[index].enabled = enabled[index];
                    graphics[index].raycastTarget = raycast[index];
                }
            }
        }
    }
}
