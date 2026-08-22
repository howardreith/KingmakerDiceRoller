using System;

namespace KingmakerDiceRoller.CharacterCreation
{
    public enum MainCharacterIdentityRelation
    {
        Unresolved,
        Absent,
        SameAsCandidate,
        SameAsControllerUnit,
        DifferentFromCandidate
    }

    public static class MainCharacterIdentityClassifier
    {
        public static MainCharacterIdentityRelation Classify(
            bool mainCharacterResolved,
            object mainDescriptor,
            object candidateDescriptor,
            bool controllerUnitResolved,
            object controllerUnitDescriptor)
        {
            if (!mainCharacterResolved || candidateDescriptor == null)
            {
                return MainCharacterIdentityRelation.Unresolved;
            }

            if (mainDescriptor == null)
            {
                return MainCharacterIdentityRelation.Absent;
            }

            if (ReferenceEquals(mainDescriptor, candidateDescriptor))
            {
                return MainCharacterIdentityRelation.SameAsCandidate;
            }

            if (!controllerUnitResolved)
            {
                return MainCharacterIdentityRelation.Unresolved;
            }

            if (controllerUnitDescriptor != null && ReferenceEquals(mainDescriptor, controllerUnitDescriptor))
            {
                return MainCharacterIdentityRelation.SameAsControllerUnit;
            }

            return MainCharacterIdentityRelation.DifferentFromCandidate;
        }
    }
}
