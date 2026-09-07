using System;
using System.Linq;
using KingmakerDiceRoller.Integration;

namespace KingmakerDiceRoller.CharacterCreation
{
    // Native first-level Commit copies the rebuilt source to the original entity before catch-up.
    // Completion owns a one-use write ticket, separate from preview/session generation tracking.
    public sealed class RespecLifecycleService
    {
        private readonly KingmakerStatAccess stats;
        private CommitTicket pending;
        public RespecLifecycleService(KingmakerStatAccess stats) { this.stats = stats; }
        public RespecOwnership Active { get; private set; }
        public bool HasPendingCommit => pending != null;
        public string LastResult { get; private set; }
        public bool? LastPassed { get; private set; }

        public bool Bind(RespecOwnership owner)
        {
            if (owner == null || !owner.IsCurrent || Active != null || pending != null) return false;
            Active = owner;
            LastPassed = null;
            LastResult = null;
            return true;
        }

        public bool BeginCommit(RollSession session, object controller, KingmakerContracts contracts, Func<bool> inProgress)
        {
            // A re-entered Commit is a new invocation, including after an exception without Update.
            if (pending != null && ReferenceEquals(pending.Owner.Controller, controller))
                Abort(controller, "A new Commit invocation superseded an incomplete respec commit.");
            if (Active == null || session == null || session.CreationKind != SupportedCharacterCreationKind.Respec ||
                !Active.Owns(controller, session.StableOwner) || pending != null) return false;
            bool verified = false;
            if (session.IsRollMode && session.IsApplied && session.Assignment != null)
            {
                object active, source, state, preview;
                verified = contracts.TryGetLevelUpControllerContext(out active, out source, out state, out preview) &&
                    ReferenceEquals(active, controller) && ReferenceEquals(source, session.StableOwner) &&
                    session.OwnsState(state) && session.OwnsUnit(preview) &&
                    session.Assignment.ToAssignedArray().SequenceEqual(stats.ReadUnitBaseValues(preview, contracts));
            }
            if (session.IsRollMode)
            {
                pending = new CommitTicket { Owner = Active,
                    InProgress = inProgress, Expected = verified ? session.Assignment.ToAssignedArray() : null };
            }
            else Active.Close();
            Active = null;
            // No constructor, preview rebuild, callback, or catch-up may reattach this assignment.
            return true;
        }

        public void BeforeReplay(object state, object target, KingmakerContracts contracts)
        {
            CommitTicket ticket = pending;
            if (ticket == null || ticket.Consumed || ticket.InProgress == null || !ticket.InProgress() || !ticket.Owner.Owns(ticket.Owner.Controller, target)) return;
            ticket.Consumed = true; // A duplicate event can verify later, but never write twice.
            GenerationRollbackSnapshot before = null;
            object distribution = null;
            try
            {
                object active, source, currentState, preview;
                object controller = ticket.Owner.Controller;
                if (ticket.Expected == null ||
                    !contracts.TryGetLevelUpControllerContext(out active, out source, out currentState, out preview) ||
                    !ReferenceEquals(active, controller) || !ReferenceEquals(source, target) ||
                    !ReferenceEquals(ReflectionAccess.Read(contracts.LevelUpStateUnitMember, state), target) ||
                    !Equals(ReflectionAccess.Read(contracts.LevelUpStateIsFirstLevelMember, state), true) ||
                    !string.Equals(ReflectionAccess.Read(contracts.LevelUpStateModeMember, state).ToString(), "Respec", StringComparison.Ordinal))
                    throw new InvalidOperationException("Owned first-level respec replay was not verified.");
                distribution = ReflectionAccess.Read(contracts.LevelUpStateDistributionMember, state);
                before = GenerationRollbackSnapshot.Capture(1, distribution, target, contracts, stats);
                stats.WriteDistributionValues(distribution, ticket.Expected, contracts);
                stats.WriteUnitBaseValues(target, ticket.Expected, contracts);
                stats.DisablePointBuyAllocator(distribution, contracts);
                ticket.Staged = ticket.Expected.SequenceEqual(stats.ReadUnitBaseValues(target, contracts));
            }
            catch (Exception exception)
            {
                ticket.Failure = exception.Message;
                if (before != null && ticket.Owner.Owns(ticket.Owner.Controller, target) && ticket.InProgress())
                {
                    try { before.Restore(distribution, target, contracts, stats); }
                    catch (Exception rollback) { ticket.Failure += "; exact-source rollback failed: " + rollback.Message; }
                }
            }
        }

        public void AfterReplay(object controller, object target, KingmakerContracts contracts)
        {
            CommitTicket ticket = pending;
            if (ticket == null || !ticket.Staged || ticket.InProgress == null || !ticket.InProgress() ||
                !ticket.Owner.Owns(controller, target)) return;
            try { ticket.Applied = ticket.Expected.SequenceEqual(stats.ReadUnitBaseValues(target, contracts)); }
            catch (Exception exception) { ticket.Applied = false; ticket.Failure = exception.Message; }
            if (!ticket.Applied) ticket.Failure = "Native replay overwrote the starting assignment; no corrective late write was made.";
        }

        public void ObserveCopyCompleted(object copyContext)
        {
            CommitTicket ticket = pending;
            if (ticket != null && ticket.InProgress != null && ticket.InProgress() && ticket.Owner.IsCurrent &&
                ReferenceEquals(ticket.Owner.CopyContext, copyContext)) ticket.CopyCompleted = true;
        }

        public bool Complete(object controller, KingmakerContracts contracts)
        {
            CommitTicket ticket = pending;
            if (ticket == null || !ReferenceEquals(ticket.Owner.Controller, controller)) return false;
            pending = null;
            int[] observed = null;
            object recipient = null;
            try
            {
                recipient = ticket.Owner.ReadFinalDescriptor();
                if (recipient != null) observed = stats.ReadUnitBaseValues(recipient, contracts);
                LastPassed = ticket.Applied && ticket.CopyCompleted && ticket.InProgress != null && ticket.InProgress() &&
                    !ReferenceEquals(recipient, ticket.Owner.Source) && ticket.Expected != null && observed != null && ticket.Expected.SequenceEqual(observed);
                LastResult = "Respec final " + (LastPassed.Value ? "PASS" : "FAIL") +
                    "; provider=" + ticket.Owner.Provider +
                    "; source=" + Identity(ticket.Owner.Source) + "; originalEntity=" + Identity(ticket.Owner.OriginalEntity) +
                    "; recipient=" + Identity(recipient) + "; expected=" + Values(ticket.Expected) +
                    "; observed=" + Values(observed) + "; applied=" + ticket.Applied + "; copyCompleted=" + ticket.CopyCompleted +
                    "; detail=" + (ticket.Failure ?? (ticket.Applied ? "post-callback comparison" : "missing authoritative replay"));
            }
            catch (Exception exception) { LastPassed = false; LastResult = "Respec final FAIL: " + exception.Message; }
            finally { ticket.Owner.Close(); }
            return true;
        }

        public bool ExpireInterruptedCommit()
        {
            if (pending == null || (pending.InProgress != null && pending.InProgress())) return false;
            Abort(null, "Native Commit exited without its completion postfix (exception/interruption); no late writes permitted.");
            return true;
        }

        public void Abort(object controller, string reason)
        {
            if (Active != null && (controller == null || ReferenceEquals(Active.Controller, controller)))
            { Active.Close(); Active = null; }
            if (pending != null && (controller == null || ReferenceEquals(pending.Owner.Controller, controller)))
            {
                pending.Owner.Close(); pending = null;
                LastPassed = false; LastResult = "Respec final FAIL: " + reason;
            }
        }

        private static string Identity(object value) => value == null ? "null" : value.GetType().Name + "@" +
            System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value).ToString("x8");
        private static string Values(int[] values) => values == null ? "unavailable" : string.Join("/", values);
        private sealed class CommitTicket
        {
            internal RespecOwnership Owner;
            internal int[] Expected;
            internal Func<bool> InProgress;
            internal bool Staged;
            internal bool Consumed;
            internal bool Applied;
            internal bool CopyCompleted;
            internal string Failure;
        }
    }
}
