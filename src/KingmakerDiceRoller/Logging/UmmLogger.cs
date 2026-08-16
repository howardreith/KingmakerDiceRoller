using System;
using UnityModManagerNet;

namespace KingmakerDiceRoller.Logging
{
    public sealed class UmmLogger : IModLogger
    {
        private readonly UnityModManager.ModEntry.ModLogger logger;

        public UmmLogger(UnityModManager.ModEntry.ModLogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Info(string message) => logger.Log(message);
        public void Warning(string message) => logger.Warning(message);
        public void Error(string message) => logger.Error(message);
        public void Exception(string context, Exception exception) => logger.LogException(context, exception);
    }
}
