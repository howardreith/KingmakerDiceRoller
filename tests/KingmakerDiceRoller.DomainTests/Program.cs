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
                new TestCase("session lifecycle happy path", SessionLifecycleHappyPath),
                new TestCase("session lifecycle rejects invalid transition", SessionLifecycleRejectsInvalidTransition),
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
        private static void NestedKeepCount() => AssertEx.Equal(9, Parse("4d[6]kh(1d[2]+1)").Evaluate(new SequenceRandomSource(6, 5, 4, 1, 1)));
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
            var record = new SavedRollArrayRecord { Values = new[] { 16,15,14,12,10,8 } };
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
