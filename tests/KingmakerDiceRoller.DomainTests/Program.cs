using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerDiceRoller.Domain;

namespace KingmakerDiceRoller.DomainTests
{
    internal static class Program
    {
        private sealed class TestCase
        {
            internal TestCase(string name, Action body)
            {
                Name = name;
                Body = body;
            }

            internal string Name { get; }
            internal Action Body { get; }
        }

        private static int Main()
        {
            var tests = new List<TestCase>
            {
                new TestCase("constant expression", ConstantExpression),
                new TestCase("whitespace and case normalization", WhitespaceAndCaseNormalization),
                new TestCase("4d6 keep highest three", FourD6KeepHighestThree),
                new TestCase("4d6 keep lowest three", FourD6KeepLowestThree),
                new TestCase("reroll ones", RerollOnes),
                new TestCase("custom minimum and maximum", CustomMinimumMaximum),
                new TestCase("nested dice count", NestedDiceCount),
                new TestCase("nested keep count", NestedKeepCount),
                new TestCase("arithmetic precedence", ArithmeticPrecedence),
                new TestCase("parenthesized arithmetic", ParenthesizedArithmetic),
                new TestCase("invalid expression", InvalidExpression),
                new TestCase("division rejected", DivisionRejected),
                new TestCase("all faces rerolled rejected", AllFacesRerolledRejected),
                new TestCase("unreasonable dice count rejected", UnreasonableDiceCountRejected),
                new TestCase("literal overflow rejected", LiteralOverflowRejected),
                new TestCase("deterministic seeded source", DeterministicSeededSource),
                new TestCase("six-score generation", SixScoreGeneration),
                new TestCase("3d6 preset", ThreeD6Preset),
                new TestCase("2d6 plus six preset", TwoD6PlusSixPreset),
                new TestCase("1d20 preset", OneD20Preset),
                new TestCase("individual low-score reroll", IndividualLowScoreReroll),
                new TestCase("whole-array low-score reroll", WholeArrayLowScoreReroll),
                new TestCase("tabletop mode preserves low score", TabletopModePreservesLowScore),
                new TestCase("no silent score clamp", NoSilentScoreClamp),
                new TestCase("array requires six values", ArrayRequiresSixValues),
                new TestCase("array enforces range", ArrayEnforcesRange),
                new TestCase("array is immutable", ArrayIsImmutable),
                new TestCase("duplicate values are valid", DuplicateValuesAreValid),
                new TestCase("swap uses positions", SwapUsesPositions),
                new TestCase("repeated swap restores assignment", RepeatedSwapRestoresAssignment),
                new TestCase("move boundaries are stable", MoveBoundariesAreStable),
                new TestCase("point buy standard values", PointBuyStandardValues),
                new TestCase("point buy fixed array", PointBuyFixedArray),
                new TestCase("point buy extended low values", PointBuyExtendedLowValues),
                new TestCase("point buy extended high values", PointBuyExtendedHighValues),
                new TestCase("saved array validates", SavedArrayValidates),
                new TestCase("saved array rejects malformed values", SavedArrayRejectsMalformedValues),
                new TestCase("fixed diagnostic array", FixedDiagnosticArray),
                new TestCase("default product configuration", ProductWorkflowTests.DefaultConfigurationIsTabletopFourD6),
                new TestCase("every product preset expression", ProductWorkflowTests.EveryPresetBuildsExpectedExpression),
                new TestCase("custom expression accepts explicit score boundary", ProductWorkflowTests.CustomExpressionUsesExplicitBoundary),
                new TestCase("custom expression rejects above boundary without clamping", ProductWorkflowTests.CustomExpressionRejectsAboveBoundaryWithoutClamping),
                new TestCase("invalid custom expression consumes no random", ProductWorkflowTests.InvalidCustomExpressionFailsBeforeRandomConsumption),
                new TestCase("workflow construction and render consume no random", ProductWorkflowTests.WorkflowConstructionAndSnapshotConsumeNoRandom),
                new TestCase("explicit generation creates six scores", ProductWorkflowTests.ExplicitGenerateProducesExactlySixScores),
                new TestCase("invalid product preset and policy fail closed", ProductWorkflowTests.InvalidPresetAndPolicyFailClosed),
                new TestCase("minimum outside explicit boundary fails closed", ProductWorkflowTests.MinimumOutsideBoundaryFailsClosed),
                new TestCase("individual minimum exhaustion", ProductWorkflowTests.IndividualPolicyExhaustionPreservesFailure),
                new TestCase("whole-array minimum exhaustion", ProductWorkflowTests.WholeArrayPolicyExhaustionPreservesFailure),
                new TestCase("source-position duplicate round trip", ProductWorkflowTests.SourcePositionRoundTripPreservesDuplicates),
                new TestCase("invalid assignment permutation fails closed", ProductWorkflowTests.InvalidPermutationFailsClosed),
                new TestCase("assignment preserves roll summary", ProductWorkflowTests.AssignmentDoesNotChangeSummary),
                new TestCase("point-buy equivalent supports score boundary", ProductWorkflowTests.PointBuyEquivalentSupportsScoreBoundary),
                new TestCase("history adds and selects newest", ProductWorkflowTests.HistoryAddsAndSelectsNewest),
                new TestCase("history evicts oldest at twenty", ProductWorkflowTests.HistoryEvictsOldestAtTwenty),
                new TestCase("history navigation wraps", ProductWorkflowTests.HistoryNavigationWraps),
                new TestCase("reassignment updates history without adding", ProductWorkflowTests.ReassignmentUpdatesSelectedHistoryWithoutAdding),
                new TestCase("different array cannot contaminate history", ProductWorkflowTests.DifferentArrayCannotContaminateSelectedHistory),
                new TestCase("browsing history preserves active entry", ProductWorkflowTests.BrowsingHistoryDoesNotChangeActiveEntry),
                new TestCase("used history entry becomes active", ProductWorkflowTests.SelectingHistoryMarksOnlyUsedEntryActive),
                new TestCase("saved schema one migrates identity", ProductWorkflowTests.SavedVersionOneMigratesIdentity),
                new TestCase("saved schema two round trips assignment", ProductWorkflowTests.SavedVersionTwoRoundTripsAssignment),
                new TestCase("saved schema two XML round trip", ProductWorkflowTests.SavedVersionTwoRoundTripsXmlSerialization),
                new TestCase("unsupported saved schema rejected", ProductWorkflowTests.SavedUnsupportedSchemaIsRejected),
                new TestCase("malformed saved permutation rejected", ProductWorkflowTests.SavedMalformedPermutationIsRejected),
                new TestCase("saved catalog isolates corrupt entries", ProductWorkflowTests.SavedCatalogIsolatesCorruptEntries),
                new TestCase("saved catalog evicts at ten", ProductWorkflowTests.SavedCatalogEvictsAtTen),
                new TestCase("saved catalog delete and navigation", ProductWorkflowTests.SavedDeleteAndNavigationAreBounded),
                new TestCase("new session snapshot starts point buy", ProductWorkflowTests.NewSessionSnapshotStartsPointBuyWithoutArray),
                new TestCase("generated commit adds one history entry", ProductWorkflowTests.GeneratedCommitAddsOneHistoryEntry),
                new TestCase("reroll preserves point-buy origin", ProductWorkflowTests.RerollPreservesPointBuyOrigin),
                new TestCase("preview rebind adds no history", ProductWorkflowTests.PreviewRebindAddsNoHistoryAndKeepsAssignment),
                new TestCase("aborted entry returns to point buy", ProductWorkflowTests.AbortedEntryReturnsToPointBuyWithoutOrigin),
                new TestCase("aborted reroll restores verified roll", ProductWorkflowTests.AbortedRerollRestoresPriorVerifiedRollState),
                new TestCase("point-buy panel snapshot", RollUiPresenterTests.PointBuySnapshotOffersRollWithoutAssignment),
                new TestCase("roll panel assignment rows", RollUiPresenterTests.RollSnapshotBuildsSixAssignmentRows),
                new TestCase("roll panel summary", RollUiPresenterTests.RollSummaryShowsTotalEquivalentAndRule),
                new TestCase("extended panel summary", RollUiPresenterTests.ExtendedSummaryIsMarked),
                new TestCase("custom expression panel visibility", RollUiPresenterTests.CustomExpressionVisibilityFollowsPreset),
                new TestCase("tabletop minimum control state", RollUiPresenterTests.TabletopPolicyLeavesMinimumInactive),
                new TestCase("panel history saved and error state", RollUiPresenterTests.HistorySavedAndErrorArePresented),
                new TestCase("point-buy panel uses progressive disclosure", RollUiPresenterTests.PointBuyUsesProgressiveDisclosure),
                new TestCase("roll panel shows relevant primary controls", RollUiPresenterTests.RollModeShowsOnlyRelevantPrimaryControls),
                new TestCase("advanced panel options begin collapsed", RollUiPresenterTests.AdvancedOptionsBeginCollapsed),
                new TestCase("history details require disclosure", RollUiPresenterTests.HistoryDetailsRequireDisclosure),
                new TestCase("saved details require disclosure", RollUiPresenterTests.SavedDetailsRequireDisclosure),
                new TestCase("panel uses readable player labels", RollUiPresenterTests.ReadablePlayerFacingLabelsAreUsed),
                new TestCase("panel policy names hide enums", RollUiPresenterTests.PolicyNamesNeverExposeEnums),
                new TestCase("panel disclosure render has no side effects", RollUiPresenterTests.DisclosureRenderingHasNoCommandSideEffects),
                new TestCase("panel render has no command side effects", RollUiPresenterTests.PresenterHasNoCommandSideEffects),
                new TestCase("UI router primary commands", RollUiPresenterTests.RouterRoutesPrimaryCommands),
                new TestCase("UI router position move", RollUiPresenterTests.RouterRoutesPositionMove),
                new TestCase("UI router preset and policy wrap", RollUiPresenterTests.RouterCyclesPresetAndPolicyWithWrap),
                new TestCase("UI router minimum boundary", RollUiPresenterTests.RouterKeepsMinimumWithinBoundary),
                new TestCase("UI router history and saved commands", RollUiPresenterTests.RouterRoutesHistoryAndSavedCommands),
                new TestCase("UI router custom expression", RollUiPresenterTests.RouterRoutesCustomExpression),
                new TestCase("native panel attaches once", NativePanelLifecycleTests.EligibleAllocatorAttachesExactlyOnce),
                new TestCase("native panel rebinds allocator", NativePanelLifecycleTests.ReplacementAllocatorRebindsWithoutDuplicateOwnership),
                new TestCase("native panel detaches on phase exit", NativePanelLifecycleTests.PhaseExitDetachesOnce),
                new TestCase("native panel rejects missing allocator", NativePanelLifecycleTests.MissingAllocatorFailsClosed),
                new TestCase("native panel reset permits reattach", NativePanelLifecycleTests.DisableResetPermitsFreshAttachment),
                new TestCase("new native panel owner starts collapsed", NativePanelUsabilityTests.NewOwnerStartsCollapsed),
                new TestCase("collapsed panel surface and background inactive", NativePanelUsabilityTests.CollapsedSurfaceAndBackgroundAreInactive),
                new TestCase("collapsed panel only access tab raycasts", NativePanelUsabilityTests.OnlyAccessTabRaycastsWhenCollapsed),
                new TestCase("opening panel changes presentation only", NativePanelUsabilityTests.OpeningChangesOnlyPresentationState),
                new TestCase("closing panel preserves disclosures and owner", NativePanelUsabilityTests.ClosingPreservesDisclosuresAndOwner),
                new TestCase("same owner panel rebind preserves expanded choice", NativePanelUsabilityTests.SameOwnerRebindPreservesExpandedChoice),
                new TestCase("new panel owner resets presentation choice", NativePanelUsabilityTests.NewOwnerResetsPresentationChoice),
                new TestCase("detached panel has no raycast footprint", NativePanelUsabilityTests.DetachedViewHasNoRaycastFootprint),
                new TestCase("ending panel owner clears presentation state", NativePanelUsabilityTests.EndOwnerClearsAllPresentationState),
                new TestCase("expanded panel raycasts only visible surface", NativePanelUsabilityTests.ExpandedStateUsesOnlyVisibleSurfaceRaycast),
                new TestCase("native panel uses code-owned rectangle", NativePanelUsabilityTests.LayoutUsesCodeOwnedRectangle),
                new TestCase("native panel expanded dimensions bounded", NativePanelUsabilityTests.ExpandedDimensionsAreBounded),
                new TestCase("native panel access tab dimensions bounded", NativePanelUsabilityTests.AccessTabDimensionsAreBounded),
                new TestCase("native panel typography and padding readable", NativePanelUsabilityTests.TypographyAndPaddingRemainReadable),
                new TestCase("native panel selector and label raycasts safe", NativePanelUsabilityTests.SelectorAndLabelRaycastPolicyIsSafe),
                new TestCase("native panel content masked and scrollable", NativePanelUsabilityTests.ContentIsMaskedAndScrollable),
                new TestCase("native panel assignment controls fit", NativePanelUsabilityTests.AssignmentControlsFitWithinPanel),
                new TestCase("native panel access anchor uses race bonus fallback", NativePanelUsabilityTests.AccessTabPrefersRacialBonusWithSafeFallback),
                new TestCase("native panel repeated attach detach creates one view", NativePanelUsabilityTests.RepeatedAttachDetachCreatesOneModeledView),
                new TestCase("context permits absent main character", CharacterCreationContextPolicyTests.NoMainCharacterValuePermitsCandidate),
                new TestCase("context permits direct same main character", CharacterCreationContextPolicyTests.DirectSameMainCharacterPermitsCandidate),
                new TestCase("context resolves Value wrapper", CharacterCreationContextPolicyTests.ValueWrapperSameMainCharacterPermitsCandidate),
                new TestCase("context resolves Descriptor wrapper", CharacterCreationContextPolicyTests.DescriptorWrapperSameMainCharacterPermitsCandidate),
                new TestCase("context resolves Unit wrapper", CharacterCreationContextPolicyTests.UnitWrapperSameMainCharacterPermitsCandidate),
                new TestCase("context permits controller source for owned preview", CharacterCreationContextPolicyTests.ControllerSourceMainCharacterPermitsOwnedPreview),
                new TestCase("context rejects different main character", CharacterCreationContextPolicyTests.DifferentMainCharacterIsRejected),
                new TestCase("context accepts exact mercenary", CharacterCreationContextPolicyTests.ExactMercenaryContextIsAccepted),
                new TestCase("mercenary marker requires controller ownership", CharacterCreationContextPolicyTests.MercenaryMarkerRequiresControllerOwnership),
                new TestCase("mercenary marker requires player faction", CharacterCreationContextPolicyTests.MercenaryMarkerRequiresPlayerFaction),
                new TestCase("mercenary marker rejects pet", CharacterCreationContextPolicyTests.MercenaryMarkerRejectsPet),
                new TestCase("mercenary marker rejects enemy", CharacterCreationContextPolicyTests.MercenaryMarkerRejectsEnemy),
                new TestCase("mercenary marker rejects respec", CharacterCreationContextPolicyTests.MercenaryMarkerRejectsRespec),
                new TestCase("mercenary marker rejects pregen", CharacterCreationContextPolicyTests.MercenaryMarkerRejectsPreGen),
                new TestCase("mercenary marker rejects unknown mode", CharacterCreationContextPolicyTests.MercenaryMarkerRejectsUnknownMode),
                new TestCase("mercenary marker rejects level-up mode", CharacterCreationContextPolicyTests.MercenaryMarkerRejectsLevelUpMode),
                new TestCase("mercenary marker rejects non-first-level", CharacterCreationContextPolicyTests.MercenaryMarkerRejectsNonFirstLevel),
                new TestCase("mercenary marker requires resolved main", CharacterCreationContextPolicyTests.MercenaryMarkerRequiresResolvedMainCharacter),
                new TestCase("mercenary evidence cannot authorize another controller", CharacterCreationContextPolicyTests.MercenaryEvidenceCannotAuthorizeAnotherController),
                new TestCase("mercenary evidence cannot authorize later build", CharacterCreationContextPolicyTests.MercenaryEvidenceCannotAuthorizeLaterBuild),
                new TestCase("main and mercenary kinds cannot cross rebind", CharacterCreationContextPolicyTests.MainAndMercenaryKindsCannotCrossRebind),
                new TestCase("context fails closed for unresolvable main character", CharacterCreationContextPolicyTests.UnresolvableMainCharacterFailsClosed),
                new TestCase("context keeps respec excluded", CharacterCreationContextPolicyTests.RespecRemainsRejectedWhenMainMatches),
                new TestCase("context keeps non-first-level excluded", CharacterCreationContextPolicyTests.NonFirstLevelRemainsRejectedWhenMainMatches),
                new TestCase("context keeps pets excluded", CharacterCreationContextPolicyTests.PetCandidateRemainsRejected),
                new TestCase("context keeps enemies excluded", CharacterCreationContextPolicyTests.EnemyCandidateRemainsRejected),
                new TestCase("context requires controller ownership", CharacterCreationContextPolicyTests.ControllerOwnershipRemainsMandatory),
                new TestCase("different main character cannot open session", CharacterCreationContextPolicyTests.DifferentMainCharacterCannotOpenSession),
                new TestCase("different main character cannot rebind session", CharacterCreationContextPolicyTests.DifferentMainCharacterCannotRebindSession),
                new TestCase("rebuilt state reuses fixed assignment", CharacterCreationContextPolicyTests.RebuiltStateReusesFixedAssignment),
                new TestCase("diagnostics distinguish same and different main", CharacterCreationContextPolicyTests.DiagnosticRelationDistinguishesSameAndDifferent),
                new TestCase("preview A opens with stable source", PreviewSessionContinuityTests.PreviewAOpensWithStableSource),
                new TestCase("preview B rebinds with different descriptor", PreviewSessionContinuityTests.PreviewBRebindsWithDifferentDescriptor),
                new TestCase("same owner does not report another unit", PreviewSessionContinuityTests.SameOwnerDoesNotReportAnotherUnit),
                new TestCase("constructor-stage replacement is marked pending", PreviewSessionContinuityTests.ConstructorStageReplacementIsMarkedPending),
                new TestCase("different stable owner is rejected", PreviewSessionContinuityTests.DifferentStableOwnerIsRejected),
                new TestCase("assignment survives three generations", PreviewSessionContinuityTests.AssignmentSurvivesThreeGenerations),
                new TestCase("rebind replaces transient objects and rollback but preserves pristine", PreviewSessionContinuityTests.RebindReplacesTransientObjectsAndRollbackButPreservesPristine),
                new TestCase("first preview captures pristine point-buy origin", PreviewSessionContinuityTests.FirstPreviewCapturesPristinePointBuyOrigin),
                new TestCase("fixed staging does not mutate pristine origin", PreviewSessionContinuityTests.FixedStagingDoesNotMutatePristineOrigin),
                new TestCase("same-owner rebind never recaptures pristine origin", PreviewSessionContinuityTests.SameOwnerRebindNeverRecapturesPristineOrigin),
                new TestCase("generation rollback changes independently from pristine origin", PreviewSessionContinuityTests.GenerationRollbackChangesIndependentlyFromPristineOrigin),
                new TestCase("nested preview refresh is refused", PreviewSessionContinuityTests.NestedPreviewRefreshIsRefused),
                new TestCase("reentrant replacement uses one refresh", PreviewSessionContinuityTests.ReentrantReplacementUsesOneRefresh),
                new TestCase("final live replacement contains fixed array", PreviewSessionContinuityTests.FinalLiveReplacementContainsFixedArray),
                new TestCase("application requests no replacement refresh", PreviewSessionContinuityTests.ApplicationDoesNotRequestAnotherRefresh),
                new TestCase("coordinator counts only verified live application", PreviewSessionContinuityTests.CoordinatorCountsOnlyVerifiedLiveApplication),
                new TestCase("detached matching preview cannot verify", PreviewSessionContinuityTests.DetachedMatchingPreviewCannotVerify),
                new TestCase("same-owner replacement does not release", PreviewSessionContinuityTests.SameOwnerReplacementDoesNotRelease),
                new TestCase("null state with same owner does not release", PreviewSessionContinuityTests.NullStateWithSameOwnerDoesNotRelease),
                new TestCase("missing controller eventually releases", PreviewSessionContinuityTests.MissingControllerEventuallyReleases),
                new TestCase("different controller eventually releases", PreviewSessionContinuityTests.DifferentControllerEventuallyReleases),
                new TestCase("point buy restores newest preview only", PreviewSessionContinuityTests.PointBuyRestoresNewestPreviewOnly),
                new TestCase("point buy restores non-default budget and allocation", PreviewSessionContinuityTests.PointBuyRestoresNonDefaultBudgetAndAllocation),
                new TestCase("hybrid rolled values and full budget cannot verify", PreviewSessionContinuityTests.HybridRolledValuesAndFullBudgetCannotVerify),
                new TestCase("zero-budget pristine assignment is not misclassified as hybrid", PreviewSessionContinuityTests.ZeroBudgetPristineAssignmentIsNotMisclassifiedAsHybrid),
                new TestCase("racial modifiers remain separate from restored base values", PreviewSessionContinuityTests.RacialModifiersRemainSeparateFromRestoredBaseValues),
                new TestCase("point-buy mode survives same-owner rebuild without restaging", PreviewSessionContinuityTests.PointBuyModeSurvivesSameOwnerRebuildWithoutRestaging),
                new TestCase("point-buy mode does not force completion or allocator restart", PreviewSessionContinuityTests.PointBuyModeDoesNotForceCompletionOrAllocatorRestart),
                new TestCase("disable during roll restores before clearing ownership", PreviewSessionContinuityTests.DisableDuringRollRestoresBeforeClearingOwnership),
                new TestCase("failed restoration rolls back to isolated roll mode", PreviewSessionContinuityTests.FailedRestorationRollsBackToIsolatedRollMode),
                new TestCase("failed rollback refuses unsafe disable", PreviewSessionContinuityTests.FailedRollbackRefusesUnsafeDisable),
                new TestCase("point-buy cancellation releases and new owner can open", PreviewSessionContinuityTests.PointBuyModeCancellationReleasesAndNewOwnerCanOpen),
                new TestCase("restoration diagnostics expose pristine transition", PreviewSessionContinuityTests.RestorationDiagnosticsExposePristineTransition),
                new TestCase("semantic restoration without active presentation is not synchronized", PreviewSessionContinuityTests.SemanticRestoreWithoutPresentationIsNotSynchronized),
                new TestCase("native ability refresh runs after pristine writes", PreviewSessionContinuityTests.NativeAbilityRefreshRunsAfterPristineWrites),
                new TestCase("presentation refresh is bounded per generation", PreviewSessionContinuityTests.PresentationRefreshIsBoundedPerGeneration),
                new TestCase("presentation refresh cannot reenter roll mode", PreviewSessionContinuityTests.PresentationRefreshCannotReenterRollMode),
                new TestCase("same-owner replacement during presentation stays suppressed", PreviewSessionContinuityTests.SameOwnerReplacementDuringPresentationStaysSuppressed),
                new TestCase("fixed assignment is not restaged by presentation", PreviewSessionContinuityTests.FixedAssignmentIsNotRestagedByPresentation),
                new TestCase("post-refresh live state remains pristine", PreviewSessionContinuityTests.PostRefreshLiveStateRemainsPristine),
                new TestCase("post-refresh allocator keeps observed budget", PreviewSessionContinuityTests.PostRefreshAllocatorKeepsObservedBudget),
                new TestCase("presentation binds current state and distribution", PreviewSessionContinuityTests.PresentationBindsCurrentStateAndDistribution),
                new TestCase("human presentation immediately shows pristine point buy", PreviewSessionContinuityTests.HumanPresentationImmediatelyShowsPristinePointBuy),
                new TestCase("race modifiers remain separate in immediate presentation", PreviewSessionContinuityTests.RaceModifiersRemainSeparateInImmediatePresentation),
                new TestCase("non-default budget reaches immediate presentation", PreviewSessionContinuityTests.NonDefaultBudgetReachesImmediatePresentation),
                new TestCase("navigation after presentation stays in point buy", PreviewSessionContinuityTests.NavigationAfterPresentationStaysInPointBuy),
                new TestCase("presentation failure preserves safe point buy", PreviewSessionContinuityTests.PresentationFailurePreservesSafePointBuy),
                new TestCase("presentation failure never rolls back to fixed array", PreviewSessionContinuityTests.PresentationFailureNeverRollsBackToFixedArray),
                new TestCase("disable after semantic restoration remains safe", PreviewSessionContinuityTests.DisableAfterSemanticRestorationRemainsSafe),
                new TestCase("disable during roll synchronizes before clear", PreviewSessionContinuityTests.DisableDuringRollSynchronizesBeforeClear),
                new TestCase("presentation refresh does not rebuild preview", PreviewSessionContinuityTests.PresentationRefreshDoesNotRebuildPreview),
                new TestCase("inactive ability phase cannot claim synchronization", PreviewSessionContinuityTests.InactiveAbilityPhaseCannotClaimSynchronization),
                new TestCase("diagnostics separate semantic and presentation failure", PreviewSessionContinuityTests.DiagnosticsSeparateSemanticAndPresentationFailure),
                new TestCase("diagnostics report native presentation verification", PreviewSessionContinuityTests.DiagnosticsReportNativePresentationVerification),
                new TestCase("view binding mismatch cannot claim synchronization", PreviewSessionContinuityTests.ViewBindingMismatchCannotClaimSynchronization),
                new TestCase("completion uses current live distribution", PreviewSessionContinuityTests.CompletionUsesCurrentLiveDistributionOnly),
                new TestCase("existing and special paths remain excluded", PreviewSessionContinuityTests.ExistingAndSpecialCreationPathsRemainExcluded),
                new TestCase("diagnostics distinguish preview lifecycle", PreviewSessionContinuityTests.DiagnosticsDistinguishPreviewLifecycle),
                new TestCase("explicit session consumes no random before Roll", PreviewSessionContinuityTests.ExplicitCoordinatorSessionConsumesNoRandomUntilRoll),
                new TestCase("mercenary preview replacement preserves session", PreviewSessionContinuityTests.MercenaryPreviewReplacementPreservesSession),
                new TestCase("mercenary allocator replacement preserves session", PreviewSessionContinuityTests.MercenaryAllocatorReplacementPreservesSession),
                new TestCase("second mercenary starts fresh session", PreviewSessionContinuityTests.SecondMercenaryStartsFreshSession),
                new TestCase("canceled mercenary clears session", PreviewSessionContinuityTests.CanceledMercenaryClearsSession),
                new TestCase("completed mercenary clears session", PreviewSessionContinuityTests.CompletedMercenaryClearsSession),
                new TestCase("untouched mercenary restores observed twenty-point origin", PreviewSessionContinuityTests.UntouchedMercenaryRestoresObservedTwentyPointOrigin),
                new TestCase("partial mercenary allocation restores exactly", PreviewSessionContinuityTests.PartialMercenaryAllocationRestoresExactly),
                new TestCase("mercenary nonstandard budget is preserved", PreviewSessionContinuityTests.MercenaryNonstandardBudgetIsPreserved),
                new TestCase("mercenary reroll preserves original origin", PreviewSessionContinuityTests.MercenaryRerollPreservesOriginalOrigin),
                new TestCase("mercenary recall captures original origin", PreviewSessionContinuityTests.MercenaryRecallCapturesOriginalOrigin),
                new TestCase("mercenary roll leaves campaign main unchanged", PreviewSessionContinuityTests.MercenaryRollLeavesCampaignMainUnchanged),
                new TestCase("explicit Roll restores modified origin", PreviewSessionContinuityTests.ExplicitRollRestoresModifiedPreRollAllocation),
                new TestCase("second Roll captures new origin", PreviewSessionContinuityTests.SecondRollTransitionCapturesNewPointBuyOrigin),
                new TestCase("invalid explicit Roll preserves point buy", PreviewSessionContinuityTests.InvalidExplicitRollLeavesPointBuyUntouched),
                new TestCase("native panel requires current owner binding", PreviewSessionContinuityTests.NativePanelEligibilityRequiresCurrentLiveOwnerBinding),
                new TestCase("native controls suppressed in roll mode", PreviewSessionContinuityTests.NativePointBuyControlsAreSuppressedInRollMode),
                new TestCase("native control states restored exactly", PreviewSessionContinuityTests.NativePointBuyControlOriginalStatesAreRestored),
                new TestCase("session lifecycle happy path", SessionLifecycleHappyPath),
                new TestCase("session lifecycle rejects invalid transition", SessionLifecycleRejectsInvalidTransition),
                new TestCase("session lifecycle can abort point-buy restore", SessionLifecycleCanAbortPointBuyRestore),
                new TestCase("liveness ignores failed observations", LivenessIgnoresFailedObservations),
                new TestCase("liveness protects unconfirmed session", LivenessProtectsUnconfirmedSession),
                new TestCase("liveness releases confirmed mismatch", LivenessReleasesConfirmedMismatch),
                new TestCase("liveness match resets mismatch", LivenessMatchResetsMismatch),
                new TestCase("liveness rejects invalid delta", LivenessRejectsInvalidDelta),
                new TestCase("session ownership uses identity", SessionOwnershipUsesIdentity),
                new TestCase("session ownership transfers explicitly", SessionOwnershipTransfersExplicitly)
            };

            int passed = 0;
            foreach (TestCase test in tests)
            {
                try
                {
                    test.Body();
                    Console.WriteLine("PASS " + test.Name);
                    passed++;
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine("FAIL " + test.Name + ": " + exception);
                }
            }

            Console.WriteLine("RESULT " + passed + "/" + tests.Count + " passed");
            return passed == tests.Count ? 0 : 1;
        }

        private static DiceExpression Parse(string text) => new DiceExpressionParser().Parse(text);
        private static DiceRollEngine Engine(params int[] values) => new DiceRollEngine(new DiceExpressionParser(), new SequenceRandomSource(values));

        private static void ConstantExpression() => AssertEx.Equal(18, Parse("18").Evaluate(new SequenceRandomSource()));
        private static void WhitespaceAndCaseNormalization()
        {
            DiceExpression expression = Parse(" 4D[6] KH3 ");
            AssertEx.Equal("4d[6]kh3", expression.NormalizedText);
            AssertEx.Equal(15, expression.Evaluate(new SequenceRandomSource(6, 5, 4, 1)));
        }
        private static void FourD6KeepHighestThree() => AssertEx.Equal(15, Parse("4d[6]kh3").Evaluate(new SequenceRandomSource(6, 5, 4, 1)));
        private static void FourD6KeepLowestThree() => AssertEx.Equal(10, Parse("4d[6]kl3").Evaluate(new SequenceRandomSource(6, 5, 4, 1)));
        private static void RerollOnes() => AssertEx.Equal(12, Parse("4d[6]r[1]kh3").Evaluate(new SequenceRandomSource(1, 4, 2, 3, 5)));
        private static void CustomMinimumMaximum() => AssertEx.Equal(15, Parse("3d[2,8]").Evaluate(new SequenceRandomSource(2, 5, 8)));
        private static void NestedDiceCount() => AssertEx.Equal(8, Parse("(1d[4]+1)d[8]").Evaluate(new SequenceRandomSource(1, 3, 5)));
        private static void NestedKeepCount() => AssertEx.Equal(11, Parse("4d[6]kh(1d[2]+1)").Evaluate(new SequenceRandomSource(6, 5, 4, 1, 1)));
        private static void ArithmeticPrecedence() => AssertEx.Equal(14, Parse("2+3*4").Evaluate(new SequenceRandomSource()));
        private static void ParenthesizedArithmetic() => AssertEx.Equal(20, Parse("(2+3)*4").Evaluate(new SequenceRandomSource()));
        private static void InvalidExpression() => AssertEx.Throws<DiceExpressionException>(() => Parse("garbage"));
        private static void DivisionRejected() => AssertEx.Throws<DiceExpressionException>(() => Parse("4/2"));
        private static void AllFacesRerolledRejected() => AssertEx.Throws<DiceExpressionException>(() => Parse("1d[2]r[1,2]").Evaluate(new SequenceRandomSource()));
        private static void UnreasonableDiceCountRejected() => AssertEx.Throws<DiceExpressionException>(() => Parse("1001d[6]").Evaluate(new SequenceRandomSource()));
        private static void LiteralOverflowRejected() => AssertEx.Throws<DiceExpressionException>(() => Parse("999999999999999999"));
        private static void DeterministicSeededSource()
        {
            var first = new SystemRandomSource(42);
            var second = new SystemRandomSource(42);
            for (int index = 0; index < 20; index++) AssertEx.Equal(first.NextInclusive(1, 20), second.NextInclusive(1, 20));
        }
        private static void SixScoreGeneration()
        {
            DiceRollRule rule = DiceRollRule.ForPreset(DiceRollPreset.FourD6DropLowest, LowScorePolicy.Tabletop);
            RolledStatArray array = Engine(6,5,4,1, 6,5,4,1, 6,5,4,1, 6,5,4,1, 6,5,4,1, 6,5,4,1).Generate(rule);
            AssertEx.SequenceEqual(new[] { 15,15,15,15,15,15 }, array);
        }
        private static void ThreeD6Preset()
        {
            RolledStatArray array = Engine(1,2,3, 2,3,4, 3,4,5, 4,5,6, 1,1,1, 6,6,6).Generate(DiceRollRule.ForPreset(DiceRollPreset.ThreeD6, LowScorePolicy.Tabletop));
            AssertEx.SequenceEqual(new[] { 6,9,12,15,3,18 }, array);
        }
        private static void TwoD6PlusSixPreset()
        {
            RolledStatArray array = Engine(1,1, 2,2, 3,3, 4,4, 5,5, 6,6).Generate(DiceRollRule.ForPreset(DiceRollPreset.TwoD6PlusSix, LowScorePolicy.Tabletop));
            AssertEx.SequenceEqual(new[] { 8,10,12,14,16,18 }, array);
        }
        private static void OneD20Preset()
        {
            RolledStatArray array = Engine(1,4,8,12,16,20).Generate(DiceRollRule.ForPreset(DiceRollPreset.OneD20, LowScorePolicy.Tabletop, 1));
            AssertEx.SequenceEqual(new[] { 1,4,8,12,16,20 }, array);
        }
        private static void IndividualLowScoreReroll()
        {
            DiceRollRule rule = new DiceRollRule("safe", "safe", "1d[20]", LowScorePolicy.RerollIndividualBelowMinimum, 7, 20, 10, 10);
            RolledStatArray array = Engine(3, 7, 6, 8, 9, 10, 11, 12).Generate(rule);
            AssertEx.SequenceEqual(new[] { 7,8,9,10,11,12 }, array);
        }
        private static void WholeArrayLowScoreReroll()
        {
            DiceRollRule rule = new DiceRollRule("safe", "safe", "1d[20]", LowScorePolicy.RerollEntireArrayBelowMinimum, 7, 20, 10, 3);
            RolledStatArray array = Engine(3,7,8,9,10,11, 7,8,9,10,11,12).Generate(rule);
            AssertEx.SequenceEqual(new[] { 7,8,9,10,11,12 }, array);
        }
        private static void TabletopModePreservesLowScore()
        {
            DiceRollRule rule = new DiceRollRule("tabletop", "tabletop", "1d[20]", LowScorePolicy.Tabletop, 7, 20, 10, 10);
            AssertEx.Equal(3, Engine(3,7,8,9,10,11).Generate(rule)[0]);
        }
        private static void NoSilentScoreClamp()
        {
            DiceRollRule rule = new DiceRollRule("bad", "bad", "21", LowScorePolicy.Tabletop, 1, 20, 10, 10);
            AssertEx.Throws<RollValidationException>(() => Engine().Generate(rule));
        }
        private static void ArrayRequiresSixValues() => AssertEx.Throws<RollValidationException>(() => new RolledStatArray(new[] { 1,2,3 }));
        private static void ArrayEnforcesRange() => AssertEx.Throws<RollValidationException>(() => new RolledStatArray(new[] { 0,2,3,4,5,6 }));
        private static void ArrayIsImmutable()
        {
            int[] source = { 16,15,14,12,10,8 };
            var array = new RolledStatArray(source);
            source[0] = 1;
            int[] copy = array.ToArray();
            copy[1] = 1;
            AssertEx.SequenceEqual(new[] { 16,15,14,12,10,8 }, array);
        }
        private static void DuplicateValuesAreValid() => AssertEx.SequenceEqual(new[] { 12,12,12,10,10,8 }, new RolledStatArray(new[] { 12,12,12,10,10,8 }));
        private static void SwapUsesPositions()
        {
            var assignment = new StatAssignment(new RolledStatArray(new[] { 16,12,12,10,8,8 })).Swap(AbilityScore.Strength, AbilityScore.Charisma);
            AssertEx.Equal(8, assignment.GetValue(AbilityScore.Strength));
            AssertEx.Equal(16, assignment.GetValue(AbilityScore.Charisma));
            AssertEx.Equal(5, assignment.GetSourcePosition(AbilityScore.Strength));
        }
        private static void RepeatedSwapRestoresAssignment()
        {
            var original = new StatAssignment(DiagnosticArrays.FixedPhaseTwoArray());
            StatAssignment result = original.Swap(AbilityScore.Strength, AbilityScore.Wisdom).Swap(AbilityScore.Strength, AbilityScore.Wisdom);
            AssertEx.True(original.Equals(result));
        }
        private static void MoveBoundariesAreStable()
        {
            var original = new StatAssignment(DiagnosticArrays.FixedPhaseTwoArray());
            AssertEx.True(ReferenceEquals(original, original.MoveUp(AbilityScore.Strength)));
            AssertEx.True(ReferenceEquals(original, original.MoveDown(AbilityScore.Charisma)));
        }
        private static void PointBuyStandardValues()
        {
            var calculator = new PointBuyEquivalentCalculator();
            int[] scores = { 7,8,9,10,11,12,13,14,15,16,17,18 };
            int[] costs = { -4,-2,-1,0,1,2,3,5,7,10,13,17 };
            for (int index = 0; index < scores.Length; index++) AssertEx.Equal(costs[index], calculator.CalculateScoreCost(scores[index]));
        }
        private static void PointBuyFixedArray()
        {
            PointBuyEquivalent equivalent = new PointBuyEquivalentCalculator().Calculate(DiagnosticArrays.FixedPhaseTwoArray());
            AssertEx.Equal(22, equivalent.Total);
            AssertEx.True(!equivalent.UsesExtendedValues);
        }
        private static void PointBuyExtendedLowValues()
        {
            var calculator = new PointBuyEquivalentCalculator();
            AssertEx.Equal(-6, calculator.CalculateScoreCost(6));
            AssertEx.Equal(-9, calculator.CalculateScoreCost(5));
            AssertEx.Equal(-12, calculator.CalculateScoreCost(4));
            AssertEx.Equal(-16, calculator.CalculateScoreCost(3));
        }
        private static void PointBuyExtendedHighValues()
        {
            var calculator = new PointBuyEquivalentCalculator();
            AssertEx.Equal(21, calculator.CalculateScoreCost(19));
            AssertEx.Equal(26, calculator.CalculateScoreCost(20));
        }
        private static void SavedArrayValidates()
        {
            var record = new SavedRollArrayRecord
            {
                SchemaVersion = 1,
                Values = new[] { 16,15,14,12,10,8 }
            };
            RolledStatArray array;
            string error;
            AssertEx.True(record.TryCreateArray(out array, out error));
            AssertEx.Equal(null, error);
        }
        private static void SavedArrayRejectsMalformedValues()
        {
            var record = new SavedRollArrayRecord { Values = new[] { 1,2 } };
            RolledStatArray array;
            string error;
            AssertEx.True(!record.TryCreateArray(out array, out error));
            AssertEx.True(!string.IsNullOrWhiteSpace(error));
        }
        private static void FixedDiagnosticArray() => AssertEx.SequenceEqual(new[] { 16,15,14,12,10,8 }, DiagnosticArrays.FixedPhaseTwoArray());
        private static void SessionLifecycleHappyPath()
        {
            var lifecycle = new RollSessionLifecycle();
            lifecycle.Activate();
            lifecycle.MarkApplied();
            lifecycle.Complete();
            AssertEx.Equal(RollSessionState.Completed, lifecycle.State);
        }
        private static void SessionLifecycleRejectsInvalidTransition() => AssertEx.Throws<InvalidOperationException>(() => new RollSessionLifecycle().MarkApplied());
        private static void SessionLifecycleCanAbortPointBuyRestore()
        {
            var lifecycle = new RollSessionLifecycle();
            lifecycle.Activate();
            lifecycle.MarkApplied();
            lifecycle.BeginPointBuyRestore();
            lifecycle.AbortPointBuyRestore();
            AssertEx.Equal(RollSessionState.Applied, lifecycle.State);
        }
        private static void LivenessIgnoresFailedObservations()
        {
            var tracker = new SessionLivenessTracker();
            for (int index = 0; index < 20; index++)
            {
                AssertEx.True(!tracker.Observe(false, false, 1f));
            }
            AssertEx.Equal(0f, tracker.MismatchSeconds);
        }
        private static void LivenessProtectsUnconfirmedSession()
        {
            var tracker = new SessionLivenessTracker();
            AssertEx.True(!tracker.Observe(true, false, SessionLivenessTracker.UnconfirmedGraceSeconds - 0.01f));
            AssertEx.True(tracker.Observe(true, false, 0.01f));
        }
        private static void LivenessReleasesConfirmedMismatch()
        {
            var tracker = new SessionLivenessTracker();
            AssertEx.True(!tracker.Observe(true, true, 0f));
            AssertEx.True(tracker.IsConfirmed);
            AssertEx.True(!tracker.Observe(true, false, SessionLivenessTracker.ConfirmedGraceSeconds - 0.01f));
            AssertEx.True(tracker.Observe(true, false, 0.01f));
        }
        private static void LivenessMatchResetsMismatch()
        {
            var tracker = new SessionLivenessTracker();
            tracker.Observe(true, true, 0f);
            tracker.Observe(true, false, 0.5f);
            tracker.Observe(true, true, 0.1f);
            AssertEx.Equal(0f, tracker.MismatchSeconds);
            AssertEx.True(!tracker.Observe(true, false, 0.5f));
        }
        private static void LivenessRejectsInvalidDelta()
        {
            var tracker = new SessionLivenessTracker();
            AssertEx.Throws<ArgumentOutOfRangeException>(() => tracker.Observe(true, false, -0.1f));
            AssertEx.Throws<ArgumentOutOfRangeException>(() => tracker.Observe(true, false, float.NaN));
            AssertEx.Throws<ArgumentOutOfRangeException>(() => tracker.Observe(true, false, float.PositiveInfinity));
        }

        private static void SessionOwnershipUsesIdentity()
        {
            var ownership = new RollSessionOwnership();
            var owner = new object();
            ownership.Claim(owner);
            AssertEx.True(ownership.BelongsTo(owner));
            AssertEx.True(!ownership.BelongsTo(new object()));
        }
        private static void SessionOwnershipTransfersExplicitly()
        {
            var ownership = new RollSessionOwnership();
            var first = new object();
            var second = new object();
            ownership.Claim(first);
            ownership.Transfer(first, second);
            AssertEx.True(ownership.BelongsTo(second));
        }
    }
}
