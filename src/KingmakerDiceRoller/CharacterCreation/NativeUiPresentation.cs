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

        public static void BuildThemedView(Action buildThemed, Action buildFallback, Action<string> diagnostic)
        {
            try { buildThemed(); }
            catch (Exception exception)
            {
                diagnostic("Native Dice Roller view styling failed; rebuilding fallback: " + exception.Message);
                // The caller discards only its partial view. Do not retry a
                // failed fallback or enter the coordinator's recovery path.
                buildFallback();
            }
        }

        public static bool CloseDrawer(NativeRollPanelState state, Action notifyClosed)
        {
            if (!state.IsExpanded) return false;
            state.Close();
            notifyClosed();
            return true;
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
