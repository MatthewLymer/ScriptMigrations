using System;
using Migrator.Console.Properties;

namespace Migrator.Console
{
    static class MessageFormatter
    {
        private const int StartingMigrationMessageLength = 64;
        private const int MaxMigrationNameLength = 22;

        public static string FormatMigrationStartedMessage(long version, string scriptName)
        {
            return Resources.StartingMigrationMessageFormat.FormatWith(
                version,
                scriptName.Truncate(MaxMigrationNameLength))
                .PadRight(StartingMigrationMessageLength);
        }

        public static string FormatMigrationCompletedMessage(TimeSpan executionTimeSpan)
        {
            return Resources.CompletedMigrationMessage.FormatWith(FormatTimeSpan(executionTimeSpan));
        }

        private static string FormatTimeSpan(TimeSpan timeSpan)
        {
            var timeInSeconds = Math.Min(9999d, timeSpan.TotalSeconds);

            return timeInSeconds.ToString("0.000").Substring(0, 5).Trim('.');
        }
    }
}