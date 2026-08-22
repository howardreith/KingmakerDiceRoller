using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KingmakerDiceRoller.CharacterCreation;
using KingmakerDiceRoller.Domain;
using KingmakerDiceRoller.Integration;

namespace KingmakerDiceRoller.DomainTests
{
    internal static class CharacterCreationContextPolicyTests
    {
        internal static void NoMainCharacterValuePermitsCandidate()
        {
            ContextHarness harness = CreateValidHarness();
            harness.Player.MainCharacter = new ValueWrapper { Value = null };
            AssertAccepted(harness.Evaluate(), MainCharacterIdentityRelation.Absent);
        }

        internal static void DirectSameMainCharacterPermitsCandidate()
        {
            ContextHarness harness = CreateValidHarness();
            harness.Player.MainCharacter = harness.Candidate;
            AssertAccepted(harness.Evaluate(), MainCharacterIdentityRelation.SameAsCandidate);
        }

        internal static void ValueWrapperSameMainCharacterPermitsCandidate()
        {
            ContextHarness harness = CreateValidHarness();
            harness.Player.MainCharacter = new ValueWrapper { Value = harness.Candidate };
            AssertAccepted(harness.Evaluate(), MainCharacterIdentityRelation.SameAsCandidate);
        }

        internal static void DescriptorWrapperSameMainCharacterPermitsCandidate()
        {
            ContextHarness harness = CreateValidHarness();
            harness.Player.MainCharacter = new DescriptorWrapper { Descriptor = harness.Candidate };
            AssertAccepted(harness.Evaluate(), MainCharacterIdentityRelation.SameAsCandidate);
        }

        internal static void UnitWrapperSameMainCharacterPermitsCandidate()
        {
            ContextHarness harness = CreateValidHarness();
            harness.Player.MainCharacter = new UnitWrapper { Unit = harness.Candidate };
            AssertAccepted(harness.Evaluate(), MainCharacterIdentityRelation.SameAsCandidate);
        }

        internal static void ControllerSourceMainCharacterPermitsOwnedPreview()
        {
            ContextHarness harness = CreateValidHarness();
            CharacterCreationContextDecision decision = harness.Evaluate();
            AssertAccepted(decision, MainCharacterIdentityRelation.SameAsControllerUnit);
            AssertEx.True(decision.Reason.Contains("controllerPreviewMatches=true"));
            AssertEx.True(decision.Reason.Contains("mainMatchesCandidate=false"));
            AssertEx.True(decision.Reason.Contains("mainMatchesControllerUnit=true"));
        }

        internal static void DifferentMainCharacterIsRejected()
        {
            ContextHarness harness = CreateValidHarness();
            harness.Player.MainCharacter = WrapDescriptor(new FakeUnitDescriptor());
            CharacterCreationContextDecision decision = harness.Evaluate();
            AssertRejected(decision, MainCharacterIdentityRelation.DifferentFromCandidate);
            AssertEx.True(decision.Reason.Contains("different UnitDescriptor"));
        }

        internal static void UnresolvableMainCharacterFailsClosed()
        {
            ContextHarness harness = CreateValidHarness();
            harness.Player.MainCharacter = new BrokenWrapper { Something = harness.Candidate };
            CharacterCreationContextDecision decision = harness.Evaluate();
            AssertRejected(decision, MainCharacterIdentityRelation.Unresolved);
            AssertEx.True(decision.Reason.Contains("fails closed"));
        }

        internal static void RespecRemainsRejectedWhenMainMatches()
        {
            ContextHarness harness = CreateValidHarness();
            harness.Mode = FakeMode.Respec;
            harness.Player.MainCharacter = harness.Candidate;
            CharacterCreationContextDecision decision = harness.Evaluate();
            AssertEx.True(!decision.Accepted);
            AssertEx.True(decision.Reason.Contains("Respecialization"));
        }

        internal static void NonFirstLevelRemainsRejectedWhenMainMatches()
        {
            ContextHarness harness = CreateValidHarness();
            harness.State.IsFirstLevel = false;
            harness.Player.MainCharacter = harness.Candidate;
            CharacterCreationContextDecision decision = harness.Evaluate();
            AssertEx.True(!decision.Accepted);
            AssertEx.True(decision.Reason.Contains("not first-level"));
        }

        internal static void PetCandidateRemainsRejected()
        {
            ContextHarness harness = CreateValidHarness();
            harness.Candidate.IsPet = true;
            CharacterCreationContextDecision decision = harness.Evaluate();
            AssertEx.True(!decision.Accepted);
            AssertEx.True(decision.Reason.Contains("Pets are excluded"));
        }

        internal static void EnemyCandidateRemainsRejected()
        {
            ContextHarness harness = CreateValidHarness();
            harness.Candidate.IsPlayersEnemy = true;
            CharacterCreationContextDecision decision = harness.Evaluate();
            AssertEx.True(!decision.Accepted);
            AssertEx.True(decision.Reason.Contains("Enemies are excluded"));
        }

        internal static void ControllerOwnershipRemainsMandatory()
        {
            ContextHarness harness = CreateValidHarness();
            harness.CharacterBuildController.LevelUpController = null;
            CharacterCreationContextDecision decision = harness.Evaluate();
            AssertEx.True(!decision.Accepted);
            AssertEx.True(decision.Reason.Contains("LevelUpController is null"));
        }

        internal static void DifferentMainCharacterCannotOpenSession()
        {
            ContextHarness harness = CreateValidHarness();
            harness.Player.MainCharacter = WrapDescriptor(new FakeUnitDescriptor());
            CharacterCreationContextDecision decision = harness.Evaluate();
            var sessions = new RollSessionManager();
            if (decision.Accepted)
            {
                RollSession ignored;
                string ignoredReason;
                sessions.TryOpenOrRebind(
                    decision,
                    generation => CreatePristine(generation),
                    generation => CreateRollback(generation),
                    () => new StatAssignment(DiagnosticArrays.FixedPhaseTwoArray()),
                    out ignored,
                    out ignoredReason);
            }

            AssertEx.Equal(null, sessions.Active);
        }

        internal static void DifferentMainCharacterCannotRebindSession()
        {
            ContextHarness harness = CreateValidHarness();
            CharacterCreationContextDecision accepted = harness.Evaluate();
            var sessions = new RollSessionManager();
            RollSession opened;
            string reason;
            AssertEx.True(sessions.TryOpenOrRebind(
                accepted,
                generation => CreatePristine(generation),
                generation => CreateRollback(generation),
                () => new StatAssignment(DiagnosticArrays.FixedPhaseTwoArray()),
                out opened,
                out reason));

            FakeState originalState = harness.State;
            harness.State = NewState(harness.Candidate);
            harness.Controller.State = harness.State;
            harness.Player.MainCharacter = WrapDescriptor(new FakeUnitDescriptor());
            CharacterCreationContextDecision rejected = harness.Evaluate();
            AssertRejected(rejected, MainCharacterIdentityRelation.DifferentFromCandidate);
            AssertEx.True(ReferenceEquals(originalState, sessions.Active.State));
        }

        internal static void RebuiltStateReusesFixedAssignment()
        {
            ContextHarness harness = CreateValidHarness();
            CharacterCreationContextDecision firstDecision = harness.Evaluate();
            var sessions = new RollSessionManager();
            var fixedAssignment = new StatAssignment(DiagnosticArrays.FixedPhaseTwoArray());
            RollSession firstSession;
            string reason;
            AssertEx.True(sessions.TryOpenOrRebind(
                firstDecision,
                generation => CreatePristine(generation),
                generation => CreateRollback(generation),
                () => fixedAssignment,
                out firstSession,
                out reason));

            harness.State = NewState(harness.Candidate);
            harness.Controller.State = harness.State;
            CharacterCreationContextDecision rebuiltDecision = harness.Evaluate();
            RollSession reboundSession;
            var replacementAssignment = new StatAssignment(new RolledStatArray(new[] { 8, 10, 12, 14, 15, 16 }));
            AssertEx.True(sessions.TryOpenOrRebind(
                rebuiltDecision,
                generation => CreatePristine(generation),
                generation => CreateRollback(generation),
                () => replacementAssignment,
                out reboundSession,
                out reason));

            AssertEx.True(ReferenceEquals(firstSession, reboundSession));
            AssertEx.True(ReferenceEquals(fixedAssignment, reboundSession.Assignment));
            AssertEx.SequenceEqual(new[] { 16, 15, 14, 12, 10, 8 }, reboundSession.Assignment.ToAssignedArray());
            AssertEx.True(reason.Contains("Rebound"));
        }

        internal static void DiagnosticRelationDistinguishesSameAndDifferent()
        {
            ContextHarness sameHarness = CreateValidHarness();
            sameHarness.Player.MainCharacter = sameHarness.Candidate;
            CharacterCreationContextDecision same = sameHarness.Evaluate();

            ContextHarness differentHarness = CreateValidHarness();
            differentHarness.Player.MainCharacter = WrapDescriptor(new FakeUnitDescriptor());
            CharacterCreationContextDecision different = differentHarness.Evaluate();

            AssertEx.Equal(MainCharacterIdentityRelation.SameAsCandidate, same.MainCharacterRelation.Value);
            AssertEx.Equal(MainCharacterIdentityRelation.DifferentFromCandidate, different.MainCharacterRelation.Value);
            AssertEx.True(same.Reason.Contains("mainMatchesCandidate=true"));
            AssertEx.True(different.Reason.Contains("mainMatchesCandidate=false"));
        }

        private static ContextHarness CreateValidHarness()
        {
            var candidate = new FakeUnitDescriptor
            {
                IsMainCharacter = false,
                IsPlayerFaction = true,
                IsPet = false,
                IsPlayersEnemy = false
            };
            var source = new FakeUnitDescriptor
            {
                IsMainCharacter = true,
                IsPlayerFaction = true,
                IsPet = false,
                IsPlayersEnemy = false
            };
            FakeState state = NewState(candidate);
            var controller = new FakeLevelUpController
            {
                State = state,
                Unit = source,
                Preview = candidate,
                m_RecalculatePreview = false
            };
            var characterBuildController = new FakeCharacterBuildController
            {
                LevelUpController = controller
            };
            var player = new FakePlayer
            {
                MainCharacter = WrapDescriptor(source)
            };
            FakeGame.Instance = new FakeGame
            {
                Player = player,
                UI = new FakeUi { CharacterBuildController = characterBuildController }
            };
            return new ContextHarness
            {
                Candidate = candidate,
                Source = source,
                State = state,
                Controller = controller,
                CharacterBuildController = characterBuildController,
                Player = player,
                Mode = FakeMode.LevelUp,
                Contracts = CreateContracts()
            };
        }

        private static FakeState NewState(FakeUnitDescriptor unit)
        {
            return new FakeState
            {
                Unit = unit,
                StatsDistribution = new FakeDistribution(),
                IsFirstLevel = true
            };
        }

        private static object WrapDescriptor(FakeUnitDescriptor descriptor)
        {
            return new ValueWrapper
            {
                Value = new FakeUnitEntityData { Descriptor = descriptor }
            };
        }

        private static PristinePointBuyState CreatePristine(int generation)
        {
            ConstructorInfo constructor = typeof(PristinePointBuyState).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[]
                {
                    typeof(int), typeof(string), typeof(int), typeof(AbilityValueSnapshot),
                    typeof(bool), typeof(int), typeof(int)
                },
                null);
            return (PristinePointBuyState)constructor.Invoke(
                new object[] { 25, "test budget", generation, CreateAbilitySnapshot(), true, 25, 25 });
        }

        private static GenerationRollbackSnapshot CreateRollback(int generation)
        {
            ConstructorInfo constructor = typeof(GenerationRollbackSnapshot).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(int), typeof(AbilityValueSnapshot), typeof(bool), typeof(int), typeof(int) },
                null);
            return (GenerationRollbackSnapshot)constructor.Invoke(
                new object[] { generation, CreateAbilitySnapshot(), true, 25, 25 });
        }

        private static AbilityValueSnapshot CreateAbilitySnapshot()
        {
            ConstructorInfo constructor = typeof(AbilityValueSnapshot).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(int[]), typeof(int[]) },
                null);
            return (AbilityValueSnapshot)constructor.Invoke(
                new object[] { Enumerable.Repeat(10, 6).ToArray(), Enumerable.Repeat(10, 6).ToArray() });
        }

        private static KingmakerContracts CreateContracts()
        {
            BindingFlags instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            BindingFlags staticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            return new KingmakerContracts(
                Assembly.GetExecutingAssembly(),
                typeof(FakeState),
                typeof(FakeUnitDescriptor),
                typeof(FakeMode),
                typeof(FakeDistribution),
                typeof(int),
                null,
                null,
                null,
                typeof(FakeState).GetProperty("Unit", instance),
                typeof(FakeState).GetProperty("StatsDistribution", instance),
                typeof(FakeState).GetProperty("IsFirstLevel", instance),
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                new object[0],
                typeof(FakeGame).GetProperty("Instance", staticFlags),
                typeof(FakeGame).GetProperty("UI", instance),
                typeof(FakeGame).GetProperty("Player", instance),
                typeof(FakePlayer).GetProperty("MainCharacter", instance),
                typeof(FakeUi).GetProperty("CharacterBuildController", instance),
                typeof(FakeCharacterBuildController).GetProperty("LevelUpController", instance),
                typeof(FakeLevelUpController).GetProperty("State", instance),
                typeof(FakeLevelUpController).GetProperty("Unit", instance),
                typeof(FakeLevelUpController).GetProperty("Preview", instance),
                typeof(FakeLevelUpController).GetField("m_RecalculatePreview", instance),
                typeof(FakeLevelUpController).GetMethod("UpdatePreview", instance),
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                new List<string>());
        }

        private static void AssertAccepted(
            CharacterCreationContextDecision decision,
            MainCharacterIdentityRelation relation)
        {
            AssertEx.True(decision.Accepted, decision.Reason);
            AssertEx.True(decision.MainCharacterRelation.HasValue);
            AssertEx.Equal(relation, decision.MainCharacterRelation.Value);
        }

        private static void AssertRejected(
            CharacterCreationContextDecision decision,
            MainCharacterIdentityRelation relation)
        {
            AssertEx.True(!decision.Accepted, decision.Reason);
            AssertEx.True(decision.MainCharacterRelation.HasValue);
            AssertEx.Equal(relation, decision.MainCharacterRelation.Value);
        }

        private sealed class ContextHarness
        {
            internal FakeUnitDescriptor Candidate;
            internal FakeUnitDescriptor Source;
            internal FakeState State;
            internal FakeLevelUpController Controller;
            internal FakeCharacterBuildController CharacterBuildController;
            internal FakePlayer Player;
            internal FakeMode Mode;
            internal KingmakerContracts Contracts;

            internal CharacterCreationContextDecision Evaluate()
            {
                return new CharacterCreationContextPolicy().Evaluate(State, Candidate, Mode, Contracts);
            }
        }

        private enum FakeMode
        {
            LevelUp = 0,
            CharGen = 1,
            PreGen = 2,
            Respec = 3,
            Unknown = 99
        }

        private sealed class FakeDistribution
        {
        }

        private sealed class FakeUnitDescriptor
        {
            public bool IsMainCharacter { get; set; }
            public bool IsPlayerFaction { get; set; }
            public bool IsPet { get; set; }
            public bool IsPlayersEnemy { get; set; }
        }

        private sealed class FakeState
        {
            public FakeUnitDescriptor Unit { get; set; }
            public FakeDistribution StatsDistribution { get; set; }
            public bool IsFirstLevel { get; set; }
        }

        private sealed class FakeLevelUpController
        {
            public FakeState State { get; set; }
            public FakeUnitDescriptor Unit { get; set; }
            public FakeUnitDescriptor Preview { get; set; }
            public bool m_RecalculatePreview;

            public void UpdatePreview()
            {
            }
        }

        private sealed class FakeCharacterBuildController
        {
            public FakeLevelUpController LevelUpController { get; set; }
        }

        private sealed class FakeUi
        {
            public FakeCharacterBuildController CharacterBuildController { get; set; }
        }

        private sealed class FakePlayer
        {
            public object MainCharacter { get; set; }
        }

        private sealed class FakeGame
        {
            public static FakeGame Instance { get; set; }
            public FakeUi UI { get; set; }
            public FakePlayer Player { get; set; }
        }

        private sealed class FakeUnitEntityData
        {
            public FakeUnitDescriptor Descriptor { get; set; }
        }

        private sealed class ValueWrapper
        {
            public object Value { get; set; }
        }

        private sealed class DescriptorWrapper
        {
            public object Descriptor { get; set; }
        }

        private sealed class UnitWrapper
        {
            public object Unit { get; set; }
        }

        private sealed class BrokenWrapper
        {
            public object Something { get; set; }
        }
    }
}
