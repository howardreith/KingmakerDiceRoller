using System;
using System.Globalization;

namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class MercenaryFinalizationObservation
    {
        internal MercenaryFinalizationObservation(
            string controllerIdentity,
            string sourceIdentity,
            string previewIdentity,
            string finalIdentity,
            int[] expectedBaseValues,
            int[] observedFinalBaseValues,
            bool passed,
            string failure)
        {
            ControllerIdentity = controllerIdentity ?? "null";
            SourceIdentity = sourceIdentity ?? "null";
            PreviewIdentity = previewIdentity ?? "null";
            FinalIdentity = finalIdentity ?? "null";
            ExpectedBaseValues = CopySix(expectedBaseValues);
            ObservedFinalBaseValues = CopySix(observedFinalBaseValues);
            Passed = passed;
            Failure = failure ?? string.Empty;
        }

        public SupportedCharacterCreationKind CreationKind =>
            SupportedCharacterCreationKind.Mercenary;
        public string ControllerIdentity { get; }
        public string SourceIdentity { get; }
        public string PreviewIdentity { get; }
        public string FinalIdentity { get; }
        public int[] ExpectedBaseValues { get; }
        public int[] ObservedFinalBaseValues { get; }
        public bool Passed { get; }
        public string Failure { get; }

        public string BuildFacts()
        {
            return "creationKind=" + CreationKind +
                "; controller=" + ControllerIdentity +
                "; source=" + SourceIdentity +
                "; preview=" + PreviewIdentity +
                "; final=" + FinalIdentity +
                "; expectedBase=" + Format(ExpectedBaseValues) +
                "; observedFinalBase=" + Format(ObservedFinalBaseValues) +
                "; passed=" + (Passed ? "true" : "false") +
                (string.IsNullOrWhiteSpace(Failure) ? string.Empty : "; failure=" + Failure);
        }

        private static int[] CopySix(int[] values)
        {
            if (values == null) return null;
            if (values.Length != 6)
            {
                throw new ArgumentException("Exactly six ability values are required.", nameof(values));
            }
            return (int[])values.Clone();
        }

        private static string Format(int[] values)
        {
            if (values == null) return "unavailable";
            return "[" + string.Join(",", Array.ConvertAll(
                values,
                value => value.ToString(CultureInfo.InvariantCulture))) + "]";
        }
    }
}
