using System;
using System.Globalization;
using NUnit.Framework;

namespace MigratorConsole.Tests
{
    class MessageFormatterTests
    {
        [TestFixture]
        public class WhenTellingMessageFormatterToFormatAStartingMigrationMessage
        {
            [Test]
            [TestCase(0, "my-script")]
            [TestCase(200000000000000, "this-is-a-really-really-really-really-really-really-really-long-script-name")]
            public void ShouldReturnMessageOfApproperiateLength(long version, string scriptName)
            {
                var message = MessageFormatter.FormatMigrationStartedMessage(version, scriptName);

                Assert.AreEqual(64, message.Length);
            }

            [Test]
            [TestCase(0)]
            [TestCase(123)]
            [TestCase(200000000000000)]
            public void ShouldContainMigrationVersion(long version)
            {
                var message = MessageFormatter.FormatMigrationStartedMessage(version, "not-relevant");

                Assert.IsTrue(message.Contains(version.ToString(CultureInfo.InvariantCulture)));
            }
        }

        [TestFixture]
        public class WhenTellingMessageFormatterToFormatACompletedMigrationMessage
        {
            [Test]
            [TestCase(0.000, "Done! (0.000 s)")]
            [TestCase(0.0015, "Done! (0.002 s)")]
            [TestCase(0.0025, "Done! (0.003 s)")]
            [TestCase(3.001, "Done! (3.001 s)")]
            [TestCase(5.100, "Done! (5.100 s)")]
            [TestCase(50.10, "Done! (50.10 s)")]
            [TestCase(500.1, "Done! (500.1 s)")]
            [TestCase(9999,  "Done! (9999 s)")]
            [TestCase(10000, "Done! (9999 s)")]
            public void ShouldReturnApproperiateMessage(double timeInSeconds, string expectedMessage)
            {
                var actualMessage = MessageFormatter.FormatMigrationCompletedMessage(TimeSpan.FromSeconds(timeInSeconds));

                Assert.AreEqual(expectedMessage, actualMessage);
            }
        }
    }
}
