using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace KingmakerDiceRoller.Integration
{
    public static class PatchOwnerDiagnostics
    {
        // Invoked at a selected respec launch, never from the frame loop. Both installed
        // Harmony surfaces are observed; namespaces alone do not imply a conflict.
        public static string Describe(params MethodBase[] methods)
        {
            var details = new List<string>();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type api = assembly.GetType("HarmonyLib.Harmony") ?? assembly.GetType("Harmony12.HarmonyInstance");
                if (api == null) continue;
                MethodInfo read = api.GetMethod("GetPatchInfo", BindingFlags.Public | BindingFlags.Static,
                    null, new[] { typeof(MethodBase) }, null);
                if (read == null) { details.Add(api.FullName + " patch metadata unavailable"); continue; }
                foreach (MethodBase method in methods)
                {
                    try
                    {
                        object info = read.Invoke(null, new object[] { method });
                        if (info == null) continue;
                        foreach (string kind in new[] { "Prefixes", "Postfixes", "Transpilers", "Finalizers" })
                        {
                            IEnumerable patches = Read(info, kind) as IEnumerable;
                            if (patches == null) continue;
                            foreach (object patch in patches)
                                details.Add(api.FullName + " " + method.DeclaringType.Name + "." + method.Name +
                                    " " + kind + " owner=" + Read(patch, "owner") + " priority=" + Read(patch, "priority"));
                        }
                    }
                    catch (Exception exception) { details.Add(api.FullName + " " + method.Name + " unavailable: " + exception.GetType().Name); }
                }
            }
            return "Respec runtime patch owners/order: " + string.Join("; ", details);
        }
        private static object Read(object value, string name)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            PropertyInfo property = value.GetType().GetProperty(name, flags);
            return property != null ? property.GetValue(value, null) : value.GetType().GetField(name, flags)?.GetValue(value);
        }
    }
}
