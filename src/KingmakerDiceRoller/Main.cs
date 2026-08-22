using System;
using KingmakerDiceRoller.Logging;
using UnityModManagerNet;

namespace KingmakerDiceRoller
{
    public static class Main
    {
        private static CompositionRoot root;
        private static Settings settings;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            if (modEntry == null) return false;
            try
            {
                settings = UnityModManager.ModSettings.Load<Settings>(modEntry) ?? new Settings();
                root = new CompositionRoot(settings, new UmmLogger(modEntry.Logger));
                modEntry.OnToggle = OnToggle;
                modEntry.OnGUI = OnGui;
                modEntry.OnUpdate = OnUpdate;
                modEntry.OnSaveGUI = OnSaveGui;
                modEntry.OnUnload = OnUnload;
                modEntry.Logger.Log("Kingmaker Dice Roller " + ProductMetadata.Version +
                    " loaded; patches are installed only when enabled.");
                return true;
            }
            catch (Exception exception)
            {
                modEntry.Logger.LogException("Load Kingmaker Dice Roller", exception);
                root = null;
                settings = null;
                return false;
            }
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            try { return root != null && root.SetEnabled(value); }
            catch (Exception exception) { modEntry.Logger.LogException("Toggle Kingmaker Dice Roller", exception); return false; }
        }

        private static void OnUpdate(UnityModManager.ModEntry modEntry, float deltaTime)
        {
            try { root?.Update(deltaTime); }
            catch (Exception exception) { modEntry.Logger.LogException("Update Kingmaker Dice Roller", exception); }
        }

        private static void OnGui(UnityModManager.ModEntry modEntry)
        {
            try { root?.DrawGui(); }
            catch (Exception exception) { modEntry.Logger.LogException("Draw Kingmaker Dice Roller settings", exception); }
        }

        private static void OnSaveGui(UnityModManager.ModEntry modEntry)
        {
            try { root?.Save(modEntry); }
            catch (Exception exception) { modEntry.Logger.LogException("Save Kingmaker Dice Roller settings", exception); }
        }

        private static bool OnUnload(UnityModManager.ModEntry modEntry)
        {
            try
            {
                if (root != null && !root.TryUnload()) return false;
                root = null;
                settings = null;
                return true;
            }
            catch (Exception exception)
            {
                modEntry.Logger.LogException("Unload Kingmaker Dice Roller", exception);
                return false;
            }
        }
    }
}
