using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;

namespace KingmakerDiceRoller.Integration
{
    // Harmony12 has no finalizer API. A captured caller frame provides a synchronous lease
    // that expires on return OR exception, even when a Harmony postfix does not execute.
    public sealed class NativeInvocationScope
    {
        private readonly MethodBase caller;
        private readonly int thread;
        private NativeInvocationScope(MethodBase caller) { this.caller = caller; thread = Thread.CurrentThread.ManagedThreadId; }
        public static NativeInvocationScope Capture(Type bridge)
        {
            StackFrame[] frames = new StackTrace().GetFrames() ?? new StackFrame[0];
            for (int i = 0; i + 1 < frames.Length; i++)
                if (frames[i].GetMethod().DeclaringType == bridge)
                    return new NativeInvocationScope(frames[i + 1].GetMethod());
            return new NativeInvocationScope(null);
        }
        public bool IsActive()
        {
            if (caller == null || Thread.CurrentThread.ManagedThreadId != thread) return false;
            foreach (StackFrame frame in new StackTrace().GetFrames() ?? new StackFrame[0])
                if (Equals(frame.GetMethod(), caller)) return true;
            return false;
        }
    }
}
