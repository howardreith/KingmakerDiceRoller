using System;
using System.Collections;
using System.Runtime.CompilerServices;
using KingmakerDiceRoller.Integration;

namespace KingmakerDiceRoller.CharacterCreation
{
    // Native Commit() replays every ILevelUpAction against the stable mercenary source
    // (LevelUpController.Unit) inside ApplyLevelup. Exact 2.1.7b IL proves LevelUpState is
    // constructed on that source only inside Commit; UpdatePreview only ever constructs on a
    // preview clone. The one-use commit ticket therefore stages the verified rolled assignment
    // on the fresh commit state before native Check/Apply consume ability values, then verifies
    // the replay instead of correcting six numbers after choices were already validated
    // against different scores.
    public sealed class MercenaryFinalizationService
    {
        private readonly KingmakerStatAccess statAccess;
        private CommitTicket pending;
        public bool HasPendingCommit => pending != null;

        public MercenaryFinalizationService(KingmakerStatAccess statAccess)
        {
            this.statAccess = statAccess ?? throw new ArgumentNullException(nameof(statAccess));
        }

        public bool TryBeginReplay(
            RollSession session,
            object controller,
            object state,
            object unit,
            KingmakerContracts contracts,
            out string error)
        {
            error = null;
            if (session == null || controller == null || state == null || unit == null || contracts == null)
            {
                return false;
            }
            if (session.CreationKind != SupportedCharacterCreationKind.Mercenary ||
                !session.IsRollMode || !session.IsApplied || session.Assignment == null ||
                !session.OwnsStableOwner(controller, unit))
            {
                return false;
            }
            // Only the commit replay constructs a state on the stable source; every preview
            // generation (including this session's own) is a different unit.
            if (session.OwnsState(state) || session.OwnsUnit(unit))
            {
                return false;
            }

            if (pending != null)
            {
                if (ReferenceEquals(pending.Owner, session.Controller))
                {
                    Abort(controller, "A new native Commit superseded an incomplete mercenary commit ticket.");
                }
                else
                {
                    return false;
                }
            }

            try
            {
                object stateUnit = ReflectionAccess.Read(contracts.LevelUpStateUnitMember, state);
                object firstLevel = ReflectionAccess.Read(contracts.LevelUpStateIsFirstLevelMember, state);
                object employee = ReflectionAccess.Read(contracts.LevelUpStateIsEmployeeMember, state);
                object mode = ReflectionAccess.Read(contracts.LevelUpStateModeMember, state);
                object stableCustom = contracts.UnitHelperIsCustomCompanionMethod.Invoke(null, new[] { unit });
                if (!ReferenceEquals(stateUnit, unit) ||
                    !(firstLevel is bool) || !(bool)firstLevel ||
                    !(employee is bool) || !(bool)employee ||
                    !(stableCustom is bool) || !(bool)stableCustom ||
                    mode == null || !string.Equals(mode.ToString(), "CharGen", StringComparison.Ordinal))
                {
                    return false;
                }

                object distribution = ReflectionAccess.Read(contracts.LevelUpStateDistributionMember, state);
                int[] expected = session.Assignment.ToAssignedArray();
                GenerationRollbackSnapshot before = GenerationRollbackSnapshot.Capture(
                    1,
                    distribution,
                    unit,
                    contracts,
                    statAccess);
                var ticket = new CommitTicket
                {
                    Owner = session.Controller,
                    StableOwner = session.StableOwner,
                    Distribution = distribution,
                    Rollback = before,
                    Expected = expected,
                    PreReplayActions = ReadActionInventory(controller, contracts)
                };
                statAccess.WriteDistributionValues(distribution, expected, contracts);
                statAccess.WriteUnitBaseValues(unit, expected, contracts);
                statAccess.DisablePointBuyAllocator(distribution, contracts);
                if (!SequenceEquals(expected, statAccess.ReadUnitBaseValues(unit, contracts)) ||
                    statAccess.ReadDistributionAvailable(distribution, contracts))
                {
                    throw new InvalidOperationException(
                        "The staged authoritative replay state did not retain the rolled assignment.");
                }
                pending = ticket;
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = "Authoritative mercenary pre-replay staging failed: " + exception.Message;
                return false;
            }
        }

        public bool AfterReplay(
            object controller,
            object target,
            IList survivingActions,
            KingmakerContracts contracts)
        {
            CommitTicket ticket = pending;
            if (ticket == null || !ReferenceEquals(ticket.Owner, controller) ||
                !ReferenceEquals(ticket.StableOwner, target))
            {
                return false;
            }
            try
            {
                ticket.Applied = SequenceEquals(
                    ticket.Expected,
                    statAccess.ReadUnitBaseValues(target, contracts));
            }
            catch (Exception exception)
            {
                ticket.Applied = false;
                ticket.Failure = exception.Message;
            }
            if (!ticket.Applied)
            {
                ticket.Failure = "Native replay overwrote the staged starting assignment; no corrective late write was made.";
                return false;
            }
            if (survivingActions != null && ticket.PreReplayActions != null)
            {
                int survivors = survivingActions.Count;
                int recorded = ticket.PreReplayActions.Length;
                if (survivors < recorded)
                {
                    ticket.Failure = recorded - survivors + " of " + recorded +
                        " recorded level-up actions failed native replay checks under the rolled scores and were not applied.";
                }
            }
            return true;
        }

        public bool TryVerifyAfterSuccessCallback(
            RollSession session,
            object controller,
            KingmakerContracts contracts,
            out MercenaryFinalizationObservation observation,
            out string error)
        {
            object finalDescriptor = session == null
                ? null
                : session.FinalizationDescriptor ?? session.StableOwner;
            FinalizationContext context;
            if (!TryResolveExactContext(
                session,
                controller,
                finalDescriptor,
                contracts,
                false,
                out context,
                out error))
            {
                observation = BuildFailure(session, context, error);
                return false;
            }
            CommitTicket ticket = pending;
            if (ticket == null || !ReferenceEquals(ticket.Owner, controller))
            {
                error = "The native completion had no verified authoritative mercenary replay.";
                observation = BuildFailure(session, context, error);
                return false;
            }
            if (!ticket.Applied)
            {
                error = ticket.Failure ?? "Native replay did not retain the staged starting assignment.";
                observation = BuildFailure(session, context, error);
                return false;
            }

            int[] expected = session.Assignment == null
                ? null
                : session.Assignment.ToAssignedArray();
            if (expected == null)
            {
                error = "The expected rolled assignment is unavailable at final verification.";
                observation = BuildFailure(session, context, error);
                return false;
            }

            try
            {
                int[] observed = statAccess.ReadUnitBaseValues(context.FinalDescriptor, contracts);
                if (!SequenceEquals(expected, observed))
                {
                    error = "The stable descriptor no longer matches the rolled assignment after the native success callback.";
                    observation = BuildObservation(context, expected, observed, false, error);
                    return false;
                }
                if (ticket.Failure != null)
                {
                    error = ticket.Failure;
                    observation = BuildObservation(context, expected, observed, false, error);
                    return false;
                }

                session.MarkFinalizationVerified(controller, context.FinalDescriptor);
                observation = BuildObservation(context, expected, observed, true, null);
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = "Final descriptor verification failed with " +
                    exception.GetType().Name + ": " + exception.Message;
                observation = BuildFailure(session, context, error);
                return false;
            }
        }

        public bool Complete(object controller, KingmakerContracts contracts)
        {
            CommitTicket ticket = pending;
            if (ticket == null || !ReferenceEquals(ticket.Owner, controller)) return false;
            // SetupNewCharacher and the success callback have already consumed the source.
            // A replay mismatch is reported as a failure; no post-commit corrective write.
            pending = null;
            return true;
        }

        public bool ExpireInterruptedCommit(KingmakerContracts contracts)
        {
            if (pending == null) return false;
            CommitTicket ticket = pending;
            Abort(
                ticket.Owner,
                "Native Commit exited without its completion postfix (exception/interruption); no late writes permitted.",
                contracts);
            return true;
        }

        public void Abort(object controller, string reason)
        {
            Abort(controller, reason, null);
        }

        public void Abort(object controller, string reason, KingmakerContracts contracts)
        {
            CommitTicket ticket = pending;
            if (ticket == null) return;
            if (controller != null && !ReferenceEquals(ticket.Owner, controller)) return;
            pending = null;
            // The commit never reached its completion postfix, so the source was not inserted;
            // restore the exact captured pre-commit state so a failed commit cannot leak the
            // staged rolled assignment into a character whose actions were validated without it.
            if (ticket.Rollback != null && contracts != null)
            {
                try
                {
                    ticket.Rollback.Restore(ticket.Distribution, ticket.StableOwner, contracts, statAccess);
                }
                catch
                {
                    // Reflection or ownership already failed; the diagnostic failure below
                    // preserves the evidence.
                }
            }
            ticket.Failure = ticket.Failure ?? reason;
        }

        private static bool SequenceEquals(int[] left, int[] right)
        {
            if (left == null || right == null || left.Length != right.Length) return false;
            for (int index = 0; index < left.Length; index++)
            {
                if (left[index] != right[index]) return false;
            }
            return true;
        }

        private static string[] ReadActionInventory(object controller, KingmakerContracts contracts)
        {
            try
            {
                object actions = ReflectionAccess.Read(contracts.LevelUpControllerLevelUpActionsMember, controller);
                var list = actions as IList;
                if (list == null) return null;
                var names = new string[list.Count];
                for (int index = 0; index < list.Count; index++)
                {
                    object action = list[index];
                    names[index] = action == null ? "null" : action.GetType().Name;
                }
                return names;
            }
            catch
            {
                return null;
            }
        }

        private static bool TryResolveExactContext(
            RollSession session,
            object controller,
            object finalDescriptor,
            KingmakerContracts contracts,
            bool requireActiveController,
            out FinalizationContext context,
            out string error)
        {
            context = new FinalizationContext
            {
                Controller = controller,
                FinalDescriptor = finalDescriptor
            };
            if (session == null || contracts == null)
            {
                error = "The finalization session or Kingmaker contracts are unavailable.";
                return false;
            }
            if (session.CreationKind != SupportedCharacterCreationKind.Mercenary)
            {
                error = "Only an immutable Mercenary session can use the mercenary finalization seam.";
                return false;
            }
            if (controller == null || finalDescriptor == null ||
                !session.OwnsStableOwner(controller, finalDescriptor))
            {
                error = "The finalization target is not the session's exact controller/source owner.";
                return false;
            }

            object currentController;
            if (requireActiveController &&
                (!contracts.TryGetLevelUpController(out currentController) ||
                 !ReferenceEquals(currentController, controller)))
            {
                error = "The exact active LevelUpController no longer owns finalization.";
                return false;
            }

            try
            {
                context.SourceDescriptor = ReflectionAccess.Read(
                    contracts.LevelUpControllerUnitMember,
                    controller);
                context.State = ReflectionAccess.Read(
                    contracts.LevelUpControllerStateMember,
                    controller);
                context.PreviewDescriptor = ReflectionAccess.Read(
                    contracts.LevelUpControllerPreviewMember,
                    controller);
                if (!ReferenceEquals(context.SourceDescriptor, session.StableOwner) ||
                    !ReferenceEquals(context.SourceDescriptor, finalDescriptor))
                {
                    error = "LevelUpController.Unit is not the accepted stable mercenary descriptor.";
                    return false;
                }
            }
            catch (Exception exception)
            {
                error = "Exact finalization ownership inspection failed with " +
                    exception.GetType().Name + ": " + exception.Message;
                return false;
            }

            error = null;
            return true;
        }

        private static MercenaryFinalizationObservation BuildFailure(
            RollSession session,
            FinalizationContext context,
            string error)
        {
            int[] expected = session == null || session.Assignment == null
                ? null
                : session.Assignment.ToAssignedArray();
            return BuildObservation(context, expected, null, false, error);
        }

        private static MercenaryFinalizationObservation BuildObservation(
            FinalizationContext context,
            int[] expected,
            int[] observed,
            bool passed,
            string failure)
        {
            context = context ?? new FinalizationContext();
            return new MercenaryFinalizationObservation(
                DescribeIdentity(context.Controller),
                DescribeIdentity(context.SourceDescriptor),
                DescribeIdentity(context.PreviewDescriptor),
                DescribeIdentity(context.FinalDescriptor),
                expected,
                observed,
                passed,
                failure);
        }

        private static string DescribeIdentity(object value)
        {
            if (value == null) return "null";
            return value.GetType().Name + "@" +
                RuntimeHelpers.GetHashCode(value).ToString("x8");
        }

        private sealed class CommitTicket
        {
            internal object Owner;
            internal object StableOwner;
            internal object Distribution;
            internal GenerationRollbackSnapshot Rollback;
            internal int[] Expected;
            internal string[] PreReplayActions;
            internal bool Applied;
            internal string Failure;
        }

        private sealed class FinalizationContext
        {
            internal object Controller;
            internal object SourceDescriptor;
            internal object PreviewDescriptor;
            internal object State;
            internal object FinalDescriptor;
        }
    }
}
