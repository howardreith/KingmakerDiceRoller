using System;
using System.Collections.Generic;

namespace KingmakerDiceRoller.CharacterCreation
{
    public enum RuntimeActivation
    {
        NotEnabled,
        Enabled,
        EnableFailed,
        Disabled
    }

    public sealed class RuntimeDiagnostics
    {
        private readonly object sync = new object();
        private readonly Queue<string> recent = new Queue<string>();
        private readonly HashSet<string> observedRejectionDetails = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> observedEventDetails = new HashSet<string>(StringComparer.Ordinal);

        public string Status { get; private set; } = "Not enabled.";
        public RuntimeActivation Activation { get; private set; } = RuntimeActivation.NotEnabled;
        // Set when a disable request was refused to keep recovery hooks installed.
        public bool DisableRefused { get; private set; }
        public int AcceptedContexts { get; private set; }
        public int RejectedContexts { get; private set; }
        public int ArraysApplied { get; private set; }
        public int PointBuyRestorations { get; private set; }
        public int SessionsReleased { get; private set; }
        public int FinalizationsVerified { get; private set; }
        public int FinalizationFailures { get; private set; }
        // The most recent finalization verdict; a later verified finalization supersedes a failure.
        public bool LastFinalizationFailed { get; private set; }
        public string LastFinalizationDetail { get; private set; }

        public void SetStatus(string value)
        {
            lock (sync)
            {
                Status = value;
                AddRecent(value);
            }
        }

        public void SetActivation(RuntimeActivation value, string status)
        {
            lock (sync)
            {
                Activation = value;
                DisableRefused = false;
                Status = status;
                AddRecent(status);
            }
        }

        public void RefuseDisable(string status)
        {
            lock (sync)
            {
                DisableRefused = true;
                Status = status;
                AddRecent(status);
            }
        }

        public void Accepted(string detail)
        {
            lock (sync)
            {
                AcceptedContexts++;
                observedRejectionDetails.Clear();
                AddRecent("ACCEPT " + detail);
            }
        }

        public bool Rejected(string detail)
        {
            lock (sync)
            {
                RejectedContexts++;
                if (!observedRejectionDetails.Add(detail))
                {
                    return false;
                }

                AddRecent("REJECT " + detail);
                return true;
            }
        }

        public void Applied(string detail)
        {
            lock (sync)
            {
                ArraysApplied++;
                AddRecent("APPLY " + detail);
            }
        }

        public bool Event(string detail)
        {
            lock (sync)
            {
                if (!observedEventDetails.Add(detail)) return false;
                AddRecent("EVENT " + detail);
                return true;
            }
        }

        public void Restored(string detail)
        {
            lock (sync)
            {
                PointBuyRestorations++;
                AddRecent("RESTORE " + detail);
            }
        }

        public void Released(string detail)
        {
            lock (sync)
            {
                SessionsReleased++;
                AddRecent("RELEASE " + detail);
            }
        }

        public void FinalizationVerified(string detail)
        {
            lock (sync)
            {
                FinalizationsVerified++;
                SessionsReleased++;
                LastFinalizationFailed = false;
                LastFinalizationDetail = detail;
                AddRecent("FINAL PASS " + detail);
            }
        }

        public void FinalizationFailed(string detail)
        {
            lock (sync)
            {
                FinalizationFailures++;
                SessionsReleased++;
                LastFinalizationFailed = true;
                LastFinalizationDetail = detail;
                AddRecent("FINAL FAIL " + detail);
            }
        }

        public string[] SnapshotRecent()
        {
            lock (sync)
            {
                return recent.ToArray();
            }
        }

        private void AddRecent(string value)
        {
            recent.Enqueue(DateTime.UtcNow.ToString("o") + " " + value);
            while (recent.Count > 12) recent.Dequeue();
        }
    }
}
