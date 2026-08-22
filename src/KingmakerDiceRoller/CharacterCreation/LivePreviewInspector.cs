using System;
using System.Linq;
using KingmakerDiceRoller.Integration;

namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class LivePreviewInspector
    {
        private readonly KingmakerStatAccess statAccess;

        public LivePreviewInspector(KingmakerStatAccess statAccess)
        {
            this.statAccess = statAccess ?? throw new ArgumentNullException(nameof(statAccess));
        }

        public LivePreviewObservation Observe(
            RollSession session,
            int[] expectedValues,
            KingmakerContracts contracts)
        {
            return Observe(session, expectedValues, expectedValues, contracts);
        }

        public LivePreviewObservation Observe(
            RollSession session,
            int[] expectedDistributionValues,
            int[] expectedUnitValues,
            KingmakerContracts contracts)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (expectedDistributionValues == null || expectedDistributionValues.Length != 6)
            {
                throw new ArgumentException("Exactly six expected distribution values are required.", nameof(expectedDistributionValues));
            }
            if (expectedUnitValues == null || expectedUnitValues.Length != 6)
            {
                throw new ArgumentException("Exactly six expected unit values are required.", nameof(expectedUnitValues));
            }
            if (contracts == null) throw new ArgumentNullException(nameof(contracts));

            try
            {
                object controller;
                object sourceUnit;
                object currentState;
                object currentPreview;
                if (!contracts.TryGetLevelUpControllerContext(
                    out controller,
                    out sourceUnit,
                    out currentState,
                    out currentPreview))
                {
                    return Failed("Controller context reflection failed.");
                }

                bool sameStableOwner = session.OwnsStableOwner(controller, sourceUnit);
                bool stateMatches = ReferenceEquals(currentState, session.State);
                bool previewMatches = ReferenceEquals(currentPreview, session.Unit);
                object currentDistribution = null;
                bool distributionMatches = false;
                if (currentState != null)
                {
                    currentDistribution = ReflectionAccess.Read(contracts.LevelUpStateDistributionMember, currentState);
                    distributionMatches = ReferenceEquals(currentDistribution, session.Distribution);
                }

                bool distributionValuesMatch = false;
                if (currentDistribution != null && contracts.StatsDistributionType.IsInstanceOfType(currentDistribution))
                {
                    distributionValuesMatch = expectedDistributionValues.SequenceEqual(
                        statAccess.ReadDistributionValues(currentDistribution, contracts));
                }

                bool unitValuesMatch = false;
                if (currentPreview != null && contracts.UnitDescriptorType.IsInstanceOfType(currentPreview))
                {
                    unitValuesMatch = expectedUnitValues.SequenceEqual(
                        statAccess.ReadUnitBaseValues(currentPreview, contracts));
                }

                return new LivePreviewObservation(
                    true,
                    sameStableOwner,
                    stateMatches,
                    previewMatches,
                    distributionMatches,
                    distributionValuesMatch,
                    unitValuesMatch,
                    null);
            }
            catch (Exception exception)
            {
                return Failed("Live preview observation failed with " + exception.GetType().Name + ".");
            }
        }

        private static LivePreviewObservation Failed(string failure)
        {
            return new LivePreviewObservation(false, false, false, false, false, false, false, failure);
        }
    }
}
