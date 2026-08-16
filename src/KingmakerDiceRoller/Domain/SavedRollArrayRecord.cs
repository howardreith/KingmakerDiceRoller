using System;

namespace KingmakerDiceRoller.Domain
{
    public sealed class SavedRollArrayRecord
    {
        public int SchemaVersion { get; set; } = 1;
        public int[] Values { get; set; }
        public string RuleId { get; set; }
        public string Expression { get; set; }
        public string SavedAtUtc { get; set; }

        public bool TryCreateArray(out RolledStatArray array, out string error)
        {
            array = null;
            error = null;
            if (SchemaVersion != 1)
            {
                error = "Unsupported saved-array schema version: " + SchemaVersion + ".";
                return false;
            }

            try
            {
                array = new RolledStatArray(Values);
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }
    }
}
