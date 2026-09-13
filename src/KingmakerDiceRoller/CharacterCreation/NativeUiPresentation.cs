using System;

namespace KingmakerDiceRoller.CharacterCreation
{
    // Cosmetic failures never enter the coordinator's mechanic recovery path.
    public static class NativeUiPresentation
    {
        public static T ResolveTheme<T>(Func<T> resolve, Action<string> diagnostic) where T : class
        {
            try { return resolve(); }
            catch (Exception exception)
            {
                diagnostic("Native Dice Roller theme unavailable; using fallback: " + exception.Message);
                return null;
            }
        }

        public static void Activate(Action command, Action clickFeedback, Action<string> diagnostic)
        {
            try { clickFeedback(); }
            catch (Exception exception)
            {
                diagnostic("Native Dice Roller click feedback unavailable: " + exception.Message);
            }
            command();
        }
    }
}
