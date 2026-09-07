using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Harmony12;
using KingmakerDiceRoller.Integration;

namespace KingmakerDiceRoller.ContractTests
{
    public static class InvocationScopeChecks
    {
        private static NativeInvocationScope scope;
        private static bool inside;
        private static bool throwInside;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Target()
        {
            inside = scope != null && scope.IsActive();
            if (throwInside) throw new InvalidOperationException("owned exception fixture");
        }
        public static class Bridge
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void Prefix() { scope = NativeInvocationScope.Capture(typeof(Bridge)); }
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Invoke() { Target(); }
        public static void OtherPrefix() { }
        public static int Run()
        {
            return RunConfiguration(false) + RunConfiguration(true);
        }
        private static int RunConfiguration(bool mixed)
        {
            string owner = "howardreith.kingmakerdiceroller.scope-contract-test";
            HarmonyInstance harmony = HarmonyInstance.Create(owner);
            var modern = new HarmonyLib.Harmony(owner + ".mixed");
            try
            {
                throwInside = false;
                if (mixed) modern.Patch(typeof(InvocationScopeChecks).GetMethod("Target"),
                    new HarmonyLib.HarmonyMethod(typeof(InvocationScopeChecks).GetMethod("OtherPrefix")));
                harmony.Patch(typeof(InvocationScopeChecks).GetMethod("Target"), new HarmonyMethod(typeof(Bridge).GetMethod("Prefix")), null);
                Invoke();
                if (!inside || scope.IsActive()) throw new Exception("Harmony12 caller lease failed normal return.");
                throwInside = true;
                try { Invoke(); } catch (InvalidOperationException) { }
                if (!inside || scope.IsActive()) throw new Exception("Harmony12 caller lease failed exception unwind.");
                return 2;
            }
            finally { harmony.UnpatchAll(owner); modern.UnpatchAll(owner + ".mixed"); }
        }
    }
}
