using System;
using System.Runtime.CompilerServices;
using KingmakerDiceRoller.Integration;

namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class MercenaryFinalizationService
    {
        private readonly KingmakerStatAccess statAccess;

        public MercenaryFinalizationService(KingmakerStatAccess statAccess)
        {
            this.statAccess = statAccess ?? throw new ArgumentNullException(nameof(statAccess));
        }

        public bool TryApplyAuthoritativeAssignment(
            RollSession session,
            object controller,
            object finalDescriptor,
            KingmakerContracts contracts,
            out MercenaryFinalizationObservation observation,
            out string error)
        {
            FinalizationContext context;
            if (!TryResolveExactContext(
                session,
                controller,
                finalDescriptor,
                contracts,
                true,
                out context,
                out error))
            {
                observation = BuildFailure(session, context, error);
                return false;
            }
            if (!session.IsApplied || session.Assignment == null)
            {
                error = "The mercenary assignment was not verified on the current preview generation.";
                observation = BuildFailure(session, context, error);
                return false;
            }
            if (session.FinalizationDescriptor != null &&
                !ReferenceEquals(session.FinalizationDescriptor, finalDescriptor))
            {
                error = "A different final descriptor was already observed for this session.";
                observation = BuildFailure(session, context, error);
                return false;
            }

            int[] expected = session.Assignment.ToAssignedArray();
            try
            {
                int[] before = statAccess.ReadUnitBaseValues(finalDescriptor, contracts);
                if (!SequenceEquals(before, expected))
                {
                    statAccess.WriteUnitBaseValues(finalDescriptor, expected, contracts);
                }
                int[] observed = statAccess.ReadUnitBaseValues(finalDescriptor, contracts);
                if (!SequenceEquals(observed, expected))
                {
                    error = "The authoritative descriptor did not retain the expected base values after the finalization write.";
                    observation = BuildObservation(context, expected, observed, false, error);
                    return false;
                }

                session.MarkAuthoritativeFinalizationApplied(controller, finalDescriptor);
                observation = BuildObservation(context, expected, observed, true, null);
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = "Authoritative base-value application failed with " +
                    exception.GetType().Name + ": " + exception.Message;
                observation = BuildFailure(session, context, error);
                return false;
            }
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
            if (!session.AuthoritativeFinalizationApplied)
            {
                error = "The native finalization replay completed without an authoritative mercenary assignment.";
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
                if (!SequenceEquals(observed, expected))
                {
                    error = "The stable descriptor no longer matches the rolled assignment after the native success callback.";
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
                if (requireActiveController &&
                    (context.State == null ||
                     !contracts.LevelUpStateType.IsInstanceOfType(context.State)))
                {
                    error = "The native finalization LevelUpState is unavailable.";
                    return false;
                }

                if (requireActiveController)
                {
                    object stateUnit = ReflectionAccess.Read(
                        contracts.LevelUpStateUnitMember,
                        context.State);
                    object firstLevel = ReflectionAccess.Read(
                        contracts.LevelUpStateIsFirstLevelMember,
                        context.State);
                    object employee = ReflectionAccess.Read(
                        contracts.LevelUpStateIsEmployeeMember,
                        context.State);
                    object mode = ReflectionAccess.Read(
                        contracts.LevelUpStateModeMember,
                        context.State);
                    object stableCustom = contracts.UnitHelperIsCustomCompanionMethod.Invoke(
                        null,
                        new[] { context.SourceDescriptor });
                    if (!ReferenceEquals(stateUnit, finalDescriptor) ||
                        !(firstLevel is bool) || !(bool)firstLevel ||
                        !(employee is bool) || !(bool)employee ||
                        !(stableCustom is bool) || !(bool)stableCustom ||
                        mode == null ||
                        !string.Equals(mode.ToString(), "CharGen", StringComparison.Ordinal) ||
                        Convert.ToInt32(mode) != 1)
                    {
                        error = "The authoritative target failed exact first-level CharGen custom-mercenary verification.";
                        return false;
                    }
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

        private static bool SequenceEquals(int[] left, int[] right)
        {
            if (left == null || right == null || left.Length != right.Length) return false;
            for (int index = 0; index < left.Length; index++)
            {
                if (left[index] != right[index]) return false;
            }
            return true;
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
