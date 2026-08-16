using System;
using KingmakerDiceRoller.Domain;

namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class RollSession
    {
        public RollSession(object state, object unit, object distribution, PointBuyBaseline baseline, StatAssignment assignment)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            Unit = unit ?? throw new ArgumentNullException(nameof(unit));
            Distribution = distribution ?? throw new ArgumentNullException(nameof(distribution));
            Baseline = baseline ?? throw new ArgumentNullException(nameof(baseline));
            Assignment = assignment ?? throw new ArgumentNullException(nameof(assignment));
            Lifecycle = new RollSessionLifecycle();
            Lifecycle.Activate();
        }

        public object State { get; private set; }
        public object Unit { get; }
        public object Distribution { get; private set; }
        public PointBuyBaseline Baseline { get; private set; }
        public StatAssignment Assignment { get; }
        public RollSessionLifecycle Lifecycle { get; }

        public bool OwnsState(object state) => ReferenceEquals(State, state);
        public bool OwnsDistribution(object distribution) => ReferenceEquals(Distribution, distribution);
        public bool OwnsUnit(object unit) => ReferenceEquals(Unit, unit);
        public bool IsRestoringPointBuy => Lifecycle.State == RollSessionState.RestoringPointBuy;
        public bool IsApplied => Lifecycle.State == RollSessionState.Applied;

        public void Rebind(object state, object distribution, PointBuyBaseline baseline)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            Distribution = distribution ?? throw new ArgumentNullException(nameof(distribution));
            Baseline = baseline ?? throw new ArgumentNullException(nameof(baseline));
        }
    }
}
