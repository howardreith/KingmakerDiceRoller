using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using KingmakerDiceRoller.CharacterCreation;

namespace KingmakerDiceRoller.Integration
{
    // Optional: failure disables this adapter, never the existing creation contracts.
    public sealed class NativeRespecContracts
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        public MethodInfo SelectorConfirm { get; private set; }
        public MethodInfo BuildStarted { get; private set; }
        public MethodInfo Cancel { get; private set; }
        public MethodInfo PlayerRespec { get; private set; }
        internal PropertyInfo selected, descriptor, referenceValue;
        internal FieldInfo shown, callback, original, rebuild, playerField;
        internal MethodInfo copyCallback, refreshDerived;
        internal PropertyInfo party, remote;
        internal MemberInfo progression, level;
        internal KingmakerContracts Game { get; }

        private NativeRespecContracts(KingmakerContracts game) { Game = game; }
        public static NativeRespecContracts Resolve(KingmakerContracts game)
        {
            if (game.GameAssembly.ManifestModule.ModuleVersionId != new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7"))
                throw new ContractResolutionException("Native respec requires the inspected Kingmaker 2.1.7b assembly.");
            var result = new NativeRespecContracts(game);
            Assembly assembly = game.GameAssembly;
            Type selector = assembly.GetType("Kingmaker.UI.CharSelect.CharSelectWindow", true);
            Type player = assembly.GetType("Kingmaker.Player", true);
            Type entity = assembly.GetType("Kingmaker.EntitySystem.Entities.UnitEntityData", true);
            Type unitRef = assembly.GetType("Kingmaker.EntitySystem.Entities.UnitReference", true);
            Type closure = assembly.GetType("Kingmaker.Player+<>c__DisplayClass171_0", true);
            Type build = game.CharacterBuildLevelUpControllerMember.DeclaringType;
            Type controller = game.LevelUpCommitMethod.DeclaringType;
            result.SelectorConfirm = RequireMethod(selector, "OnButtonOk", Type.EmptyTypes, typeof(void));
            result.PlayerRespec = RequireMethod(player, "RespecCompanion", new[] { entity, typeof(Action) }, typeof(void));
            result.BuildStarted = build.GetMethods(Flags).Single(m => m.Name == "HandleLevelUpStart" && m.GetParameters().Length == 4);
            ParameterInfo[] parameters = result.BuildStarted.GetParameters();
            if (result.BuildStarted.ReturnType != typeof(void) || parameters[0].ParameterType != game.UnitDescriptorType ||
                parameters[1].ParameterType.FullName != "Newtonsoft.Json.Linq.JToken" || parameters[2].ParameterType != typeof(Action) ||
                parameters[3].ParameterType != game.CharBuildModeType) throw new ContractResolutionException("Unexpected respec launch signature.");
            result.Cancel = RequireMethod(controller, "Cancel", Type.EmptyTypes, typeof(void));
            result.refreshDerived = RequireMethod(game.LevelUpStateType, "OnApplyAction", Type.EmptyTypes, typeof(void));
            result.selected = selector.GetProperty("CurrentCharacter", Flags);
            result.shown = selector.GetField("m_IsShowed", Flags);
            result.descriptor = entity.GetProperty("Descriptor", Flags);
            result.referenceValue = unitRef.GetProperty("Value", Flags);
            result.callback = controller.GetField("m_OnSuccess", Flags);
            result.original = closure.GetField("unit", Flags);
            result.rebuild = closure.GetField("newUnit", Flags);
            result.playerField = closure.GetField("<>4__this", Flags);
            result.copyCallback = RequireMethod(closure, "<RespecCompanion>b__0", Type.EmptyTypes, typeof(void));
            result.party = player.GetProperty("PartyCharacters", Flags);
            result.remote = player.GetProperty("RemoteCompanions", Flags);
            result.progression = ReflectionAccess.RequireInstanceMember(game.UnitDescriptorType, "Progression");
            Type progressionType = result.progression is FieldInfo ? ((FieldInfo)result.progression).FieldType : ((PropertyInfo)result.progression).PropertyType;
            result.level = ReflectionAccess.RequireInstanceMember(progressionType, "CharacterLevel");
            if (result.selected == null || result.selected.PropertyType != entity || result.shown == null || result.shown.FieldType != typeof(bool) ||
                result.descriptor == null || result.descriptor.PropertyType != game.UnitDescriptorType || result.referenceValue == null || result.referenceValue.PropertyType != entity ||
                result.callback == null || result.callback.FieldType != typeof(Action) || result.original == null || result.original.FieldType != entity ||
                result.rebuild == null || result.rebuild.FieldType != entity || result.playerField == null || result.playerField.FieldType != player ||
                result.party == null || result.remote == null) throw new ContractResolutionException("Incomplete native respec ownership/copy contracts.");
            return result;
        }

        private static MethodInfo RequireMethod(Type type, string name, Type[] args, Type result)
        {
            MethodInfo method = type.GetMethod(name, Flags, null, args, null);
            if (method == null || method.ReturnType != result || method.IsStatic) throw new ContractResolutionException("Missing exact " + type.FullName + "." + name);
            return method;
        }
    }
}
