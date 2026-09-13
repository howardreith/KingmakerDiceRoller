using System;
using System.Collections.Generic;

namespace KingmakerDiceRoller.UI
{
    internal sealed class NativeThemeRecovery
    {
        internal const int MaximumAttempts = 3; // Initial resolve plus at most two FillData boundaries.
        private object allocator;
        private object owner;
        private bool resolving;
        internal int Attempts { get; private set; }

        internal void Bind(object currentAllocator, object currentOwner)
        {
            if (ReferenceEquals(allocator, currentAllocator) && ReferenceEquals(owner, currentOwner)) return;
            allocator = currentAllocator;
            owner = currentOwner;
            Attempts = 0;
        }
        internal bool TryBegin(bool needsRecovery, bool allocatorFilled)
        {
            if (resolving || !needsRecovery || Attempts >= MaximumAttempts || (Attempts > 0 && !allocatorFilled)) return false;
            Attempts++;
            resolving = true;
            return true;
        }
        internal void Complete() { resolving = false; }
        internal void Reset() { allocator = owner = null; Attempts = 0; resolving = false; }
    }

    // Actions here only set presentation properties on already-owned widgets.
    // Recovery never reconstructs controls or registers their command listeners.
    internal sealed class NativeThemeBindings
    {
        private sealed class Binding
        {
            internal NativeThemeCapability Capability;
            internal Action<object[]> Apply;
            internal Action Fallback;
        }
        private readonly List<Binding> bindings = new List<Binding>();
        private readonly Dictionary<NativeThemeCapability, NativeThemeResource> applied = new Dictionary<NativeThemeCapability, NativeThemeResource>();
        internal int Count => bindings.Count;
        internal void Add(NativeThemeCapability capability, Action<object[]> apply, Action fallback)
        {
            bindings.Add(new Binding { Capability = capability, Apply = apply, Fallback = fallback });
        }
        internal void Clear() { bindings.Clear(); applied.Clear(); }
        private static bool Same(NativeThemeResource first, NativeThemeResource second)
        {
            if (first == null || second == null) return first == second;
            if (first.Components.Length != second.Components.Length) return false;
            for (int index = 0; index < first.Components.Length; index++)
                if (!ReferenceEquals(first.Components[index], second.Components[index])) return false;
            return true;
        }
        internal void Apply(NativeThemeResolution resolution, Action<string> diagnostic)
        {
            foreach (NativeThemeCapability capability in NativeThemeResolution.Capabilities)
            {
                NativeThemeResource resource = resolution == null ? null : resolution.Get(capability);
                NativeThemeResource previous;
                if (applied.TryGetValue(capability, out previous) && Same(previous, resource)) continue;
                if (resource != null)
                {
                    try
                    {
                        foreach (Binding binding in bindings)
                            if (binding.Capability == capability) binding.Apply(resource.Components);
                        applied[capability] = resource;
                        continue;
                    }
                    catch (Exception exception)
                    {
                        resolution.Reject(capability, "applying owned style failed: " + exception.Message + "; " + resource.Identity);
                    }
                }
                applied[capability] = null;
                foreach (Binding binding in bindings)
                {
                    if (binding.Capability != capability) continue;
                    try { binding.Fallback(); }
                    catch (Exception exception) { diagnostic("Native Dice Roller " + capability + " fallback failed: " + exception.Message); }
                }
            }
        }
    }
}
