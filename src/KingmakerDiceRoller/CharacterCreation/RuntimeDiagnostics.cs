using System;
using System.Collections.Generic;

namespace KingmakerDiceRoller.CharacterCreation
{
    public sealed class RuntimeDiagnostics
    {
        private readonly object sync = new object();
        private readonly Queue<string> recent = new Queue<string>();
        private string lastRejectedDetail;

        public string Status { get; private set; } = "Not enabled.";
        public int AcceptedContexts { get; private set; }
        public int RejectedContexts { get; private set; }
        public int ArraysApplied { get; private set; }
        public int PointBuyRestorations { get; private set; }
        public int SessionsReleased { get; private set; }

        public void SetStatus(string value)
        {
            lock (sync)
            {
                Status = value;
                AddRecent(value);
            }
        }

        public void Accepted(string detail)
        {
            lock (sync)
            {
                AcceptedContexts++;
                lastRejectedDetail = null;
                AddRecent("ACCEPT " + detail);
            }
        }

        public bool Rejected(string detail)
        {
            lock (sync)
            {
                RejectedContexts++;
                if (string.Equals(lastRejectedDetail, detail, StringComparison.Ordinal))
                {
                    return false;
                }

                lastRejectedDetail = detail;
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
