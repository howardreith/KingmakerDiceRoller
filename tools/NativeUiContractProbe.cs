using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

// Read-only checks against shipped IL and the built candidate; never instantiate Unity objects.
public static class NativeUiContractProbe
{
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private static readonly Dictionary<short, OpCode> Codes = typeof(OpCodes).GetFields()
        .Where(f => f.FieldType == typeof(OpCode)).Select(f => (OpCode)f.GetValue(null)).ToDictionary(c => c.Value);

    private static List<object> Operands(MethodBase method)
    {
        var result = new List<object>();
        byte[] bytes = method.GetMethodBody().GetILAsByteArray();
        for (int offset = 0; offset < bytes.Length;)
        {
            short value = bytes[offset++];
            if (value == 0xfe) value = (short)(0xfe00 | bytes[offset++]);
            OpCode code = Codes[value];
            int size;
            switch (code.OperandType)
            {
                case OperandType.InlineNone: size = 0; break;
                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar: size = 1; break;
                case OperandType.InlineVar: size = 2; break;
                case OperandType.InlineI8:
                case OperandType.InlineR: size = 8; break;
                case OperandType.InlineSwitch: size = 4 + 4 * BitConverter.ToInt32(bytes, offset); break;
                default: size = 4; break;
            }
            if (code.OperandType == OperandType.InlineMethod)
                result.Add(method.Module.ResolveMethod(BitConverter.ToInt32(bytes, offset),
                    method.DeclaringType.GetGenericArguments(), method.IsGenericMethod ? method.GetGenericArguments() : null));
            if (code.OperandType == OperandType.InlineString)
                result.Add(method.Module.ResolveString(BitConverter.ToInt32(bytes, offset)));
            offset += size;
        }
        return result;
    }

    private static List<MethodBase> Calls(MethodBase method) { return Operands(method).OfType<MethodBase>().ToList(); }
    private static MethodInfo Method(Type type, string name, int parameters = -1)
    {
        return type.GetMethods(All).Single(m => m.Name == name && (parameters < 0 || m.GetParameters().Length == parameters));
    }
    private static void Check(List<string> results, bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException("Native UI contract failed: " + description);
        results.Add(description);
    }
    public static string[] Run(string managedDirectory, string candidatePath)
    {
        var results = new List<string>();
        Assembly game = Assembly.LoadFrom(Path.Combine(managedDirectory, "Assembly-CSharp.dll"));
        Assembly ui = Assembly.LoadFrom(Path.Combine(managedDirectory, "UnityEngine.UI.dll"));
        Assembly candidate = Assembly.LoadFrom(candidatePath);
        Type button = ui.GetType("UnityEngine.UI.Button", true);
        Type selectable = ui.GetType("UnityEngine.UI.Selectable", true);
        Type nativeButton = game.GetType("Kingmaker.UI.Constructor.ButtonPF", true);
        Type soundType = game.GetType("Kingmaker.UI.UISoundType", true);
        Type sound = game.GetType("Kingmaker.UI.UISoundManager", true);
        Type theme = candidate.GetType("KingmakerDiceRoller.UI.NativeBookTheme", true);
        Type host = candidate.GetType("KingmakerDiceRoller.UI.NativeRollPanelHost", true);
        Check(results, game.ManifestModule.ModuleVersionId == new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7"), "Kingmaker 2.1.7b MVID");
        Check(results, nativeButton.BaseType == button, "ButtonPF derives from shipped Unity Button");
        Check(results, !nativeButton.GetMethods(All | BindingFlags.DeclaredOnly).Any(m => m.Name == "Awake" || m.Name == "OnEnable" || m.Name == "OnSubmit"), "ButtonPF has no extra Awake/OnEnable/Submit override");
        var press = Calls(Method(button, "Press"));
        Check(results, press.Count(m => m.Name == "IsActive") == 1 && press.Count(m => m.Name == "IsInteractable") == 1 &&
            press.Count(m => m.Name == "Invoke") == 1, "Unity Press has active/interactable checks and one event invocation");
        Check(results, Calls(Method(button, "OnPointerClick")).Count(m => m.Name == "Press") == 1, "Unity pointer route calls Press once");
        Check(results, Calls(Method(button, "OnSubmit")).Count(m => m.Name == "Press") == 1, "Unity submit route calls Press once");
        var transitions = Calls(Method(selectable, "DoStateTransition"));
        Check(results, transitions.Any(m => m.Name == "get_pressedSprite") && transitions.Any(m => m.Name == "get_highlightedSprite") &&
            transitions.Any(m => m.Name == "get_disabledSprite") && transitions.Any(m => m.Name == "DoSpriteSwap"), "Selectable supports the donor sprite states");
        Check(results, (int)Enum.Parse(soundType, "ButtonClick") == 0, "Native ButtonClick enum value is zero");
        Check(results, Calls(Method(nativeButton, "OnPointerClick")).Any(m => m.DeclaringType == sound && m.Name == "Play"), "ButtonPF routes pointer audio through UISoundManager");
        Check(results, Calls(Method(sound, "Play", 1)).Any(m => m.DeclaringType == sound && m.Name == "Play" && m.GetParameters().Length == 2), "Ordinary UI sound overload selects Common emitter");
        Check(results, Calls(Method(sound, "Play", 2)).Count(m => m.DeclaringType.Name == "AkSoundEngine" && m.Name == "PostEvent") == 1, "Native manager posts one Wwise event");
        Type settings = game.GetType("Kingmaker.UI.SettingsUI.SoundSettingsController", true);
        Check(results, Operands(Method(settings, "SettingsToRealMasterVolume")).Contains("AudioLevel") &&
            Operands(Method(settings, "ToggleMuteMasterVolume")).Contains("AudioLevel"), "Master slider and mute use native AudioLevel RTPC");
        var audioCalls = Calls(Method(theme, "PlayClick"));
        Check(results, audioCalls.Count(m => m.DeclaringType == sound && m.Name == "Play") == 1 &&
            !audioCalls.Any(m => m.DeclaringType.Name == "AkSoundEngine"), "Candidate click uses exactly one ordinary UI sound call");
        var create = Calls(Method(host, "CreateButton"));
        Check(results, create.Count(m => m.Name == "AddComponent" && m.IsGenericMethod && m.GetGenericArguments()[0] == button) == 1 &&
            !create.Any(m => m.Name == "Instantiate"), "Candidate constructs one plain Unity Button without cloning");
        Check(results, create.Count(m => m.IsConstructor && m.DeclaringType.Name == "ButtonClickedEvent") == 1 &&
            create.Count(m => m.Name == "set_onClick") == 1 && create.Count(m => m.Name == "AddListener") == 1,
            "Candidate replaces the click event and binds one listener");
        var callbacks = host.GetNestedTypes(All).SelectMany(t => t.GetMethods(All | BindingFlags.DeclaredOnly))
            .Where(m => m.Name.StartsWith("<CreateButton>b__", StringComparison.Ordinal)).ToArray();
        Check(results, callbacks.Count(callback => Calls(callback).Count(m => m.Name == "Activate" &&
            m.DeclaringType.FullName == "KingmakerDiceRoller.CharacterCreation.NativeUiPresentation") == 1) == 1 &&
            callbacks.Sum(callback => Calls(callback).Count(m => m.Name == "Activate")) == 1,
            "Candidate accepted activation uses one presentation dispatcher");
        Type themeResolver = candidate.GetType("KingmakerDiceRoller.UI.NativeThemeResolver", true);
        Type lookup = candidate.GetType("KingmakerDiceRoller.UI.NativeUiDonorLookup", true);
        Type bindings = candidate.GetType("KingmakerDiceRoller.UI.NativeThemeBindings", true);
        Type recovery = candidate.GetType("KingmakerDiceRoller.UI.NativeThemeRecovery", true);
        Check(results, Calls(Method(theme, "Resolve")).Count(m => m.DeclaringType == themeResolver && m.Name == "Resolve") == 1,
            "Unity theme entry uses the shared production capability resolver");
        Check(results, Calls(Method(themeResolver, "ResolveCapability")).Count(m => m.DeclaringType == lookup && m.Name == "RequireLiteralChild") == 1 &&
            (string)themeResolver.GetField("ButtonLabelName", All).GetRawConstantValue() == "Next/Complete text",
            "Button label uses exact literal-child lookup, independently of action artwork");
        Type backend = theme.GetNestedType("UnitySource", All);
        Check(results, Calls(Method(backend, "Child")).Any(m => m.Name == "GetChild") &&
            Calls(Method(backend, "Components")).Any(m => m.Name == "GetComponents") &&
            !backend.GetMethods(All | BindingFlags.DeclaredOnly).SelectMany(m => Calls(m)).Any(m => m.Name == "Find" || m.Name == "FindObjectsOfTypeAll"),
            "Unity adapter reads bounded direct children/local components without scene searches");
        Type phase = game.GetType("Kingmaker.UI.LevelUp.Phase.CharBPhase", true);
        Type skills = game.GetType("Kingmaker.UI.LevelUp.Phase.CharBPhaseSkills", true);
        Type allocator = game.GetType("Kingmaker.UI.LevelUp.CharBAbilityScoresAllocator", true);
        Check(results, Calls(Method(phase, "UpdateData")).Any(m => m.Name == "FillData") &&
            Calls(Method(skills, "FillData")).Count(m => m.DeclaringType == allocator && m.Name == "FillData") == 1,
            "Existing native Skills phase update reaches allocator FillData exactly once");
        var retryCalls = Calls(Method(host, "RecoverTheme"));
        Check(results, retryCalls.Count(m => m.DeclaringType == recovery && m.Name == "TryBegin") == 1 &&
            retryCalls.Any(m => m.DeclaringType == bindings && m.Name == "Apply") &&
            !retryCalls.Any(m => m.Name == "CreateOwnedView" || m.Name == "DestroyAttachedView" || m.Name == "AddListener" || m.Name == "Execute"),
            "Recovery applies existing bindings through one bounded gate without rebuilding or dispatching");
        Check(results, (int)recovery.GetField("MaximumAttempts", All).GetRawConstantValue() == 3 &&
            Calls(Method(host, "DestroyAttachedView")).Any(m => m.DeclaringType == bindings && m.Name == "Clear"),
            "Three-attempt attachment budget and owned binding teardown are wired");
        return results.ToArray();
    }
}
