using System;
using System.Reflection;
using KingmakerDiceRoller.Integration;
using KingmakerDiceRoller.Logging;

namespace KingmakerDiceRoller.CharacterCreation
{
    // Exact 2.1.7b IL proves the skills page caches its presentation: the red
    // remaining-points badge is repainted by CharBSkillsAllocator.FillLevelUpData (through
    // CharBPhaseSkills.FillData when the phase is dirty), the phase IsUnlocked cache that
    // gates every forward transition is recomputed only by DefinePhases inside SetupUI,
    // and the feature-selection sections are rebuilt by DefineAvailibleData. A native
    // skill click runs exactly: DefineAvailibleData -> Skills.IsDirty = true -> SetupUI.
    // Staging a rolled assignment changes the model without any of those, so this service
    // replays that exact native refresh sequence after every relevant change.
    public sealed class SkillsPhaseSynchronizationService
    {
        public const string NativeRefreshPath =
            "CharacterBuildController.DefineAvailibleData() + Skills.IsDirty + CharacterBuildController.SetupUI()";

        private readonly IModLogger logger;
        private RollSession lastSession;
        private object lastState;
        private int lastRevision;
        private bool syncInProgress;

        public SkillsPhaseSynchronizationService(IModLogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool IsRefreshInProgress => syncInProgress;
        public int TotalSyncCount { get; private set; }

        public bool IsSynchronizationPending(RollSession session)
        {
            if (session == null) return false;
            return !ReferenceEquals(lastSession, session) ||
                !ReferenceEquals(lastState, session.State) ||
                lastRevision != session.AssignmentRevision;
        }

        public bool TrySynchronize(
            RollSession session,
            KingmakerContracts contracts,
            out int syncedRevision,
            out string error)
        {
            syncedRevision = session == null ? 0 : session.AssignmentRevision;
            if (session == null || contracts == null)
            {
                error = "The skills-page synchronization requires an active session and Kingmaker contracts.";
                return false;
            }
            if (session.IsRestoringPointBuy)
            {
                error = "The skills-page synchronization cannot run mid-restore.";
                return false;
            }
            if (syncInProgress)
            {
                error = "A skills-page synchronization is already in progress; nested sync was refused.";
                return false;
            }
            if (!IsSynchronizationPending(session))
            {
                // Idempotent: repeated open/close or unchanged commands never replay the page.
                error = null;
                return true;
            }

            object characterBuildController;
            bool abilityPhaseActive;
            object abilityPhase;
            object allocator;
            if (!contracts.TryGetAbilityPhasePresentationContext(
                    out characterBuildController,
                    out abilityPhaseActive,
                    out abilityPhase,
                    out allocator) ||
                characterBuildController == null ||
                abilityPhase == null)
            {
                error = "The native Skills phase context is unavailable for synchronization.";
                return false;
            }

            syncInProgress = true;
            try
            {
                contracts.CharacterBuildDefineAvailibleDataMethod.Invoke(characterBuildController, null);
                contracts.CharBPhaseIsDirtyField.SetValue(abilityPhase, true);
                contracts.CharacterBuildSetupUiMethod.Invoke(characterBuildController, null);
                TotalSyncCount++;
                lastSession = session;
                lastState = session.State;
                lastRevision = session.AssignmentRevision;
                syncedRevision = session.AssignmentRevision;
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                Exception actual = exception is TargetInvocationException && exception.InnerException != null
                    ? exception.InnerException
                    : exception;
                logger.Exception("Synchronize native skills-page presentation", actual);
                error = "The native skills-page refresh failed with " +
                    actual.GetType().Name + ": " + actual.Message;
                return false;
            }
            finally
            {
                syncInProgress = false;
            }
        }
    }
}
