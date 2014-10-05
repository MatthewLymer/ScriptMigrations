using NUnit.Framework;

namespace MigratorConsole.Tests
{
    class MigratorCommandLineParserTests
    {
        [TestFixture]
        public class WhenTellingMigratorCommandLineParserTestsToParseACommandLineArray
        {
            private static MigratorCommandLineParserResult ParseCommandLine(string commandLine)
            {
                var args = CommandLineSplitter.CommandLineToArgs("foo.exe " + commandLine);

                var parser = new MigratorCommandLineParser<MigratorCommandLineParserResult>();

                return parser.Parse(args);
            }

            [Test]
            public void ShouldHaveAllPropertiesDisabledIfNoArgumentsPassed()
            {
                var result = ParseCommandLine("");

                Assert.IsFalse(result.ShowHelp);
                Assert.IsFalse(result.MigrateUp);
                Assert.IsFalse(result.MigrateDown);
                Assert.IsNull(result.Version);
                Assert.IsNull(result.RunnerQualifiedName);
                Assert.IsNull(result.ScriptsPath);
                Assert.IsNull(result.ConnectionString);
            }
            
            [Test]
            [TestCase("/?", true)]
            public void ShouldHaveShowHelpPropertySetIfRequested(string commandLine, bool shouldShowHelp)
            {
                var result = ParseCommandLine(commandLine);

                Assert.IsTrue(result.ShowHelp);
            }

            [Test]
            [TestCase("/command=up")]
            [TestCase("/COMMAND=UP")]
            [TestCase("/coMMAnd=uP")]
            public void ShouldHaveCommandPropertySetToMigrateUpIfRequested(string commandLine)
            {
                var result = ParseCommandLine(commandLine);

                Assert.IsTrue(result.MigrateUp);
            }

            [Test]
            [TestCase("/command=down")]
            [TestCase("/commAND=DoWn")]
            [TestCase("/COMMAND=DOWN")]
            public void ShouldHaveCommandPropertySetToMigrateDownIfRequested(string commandLine)
            {
                var result = ParseCommandLine(commandLine);

                Assert.IsTrue(result.MigrateDown);
            }

            [Test]
            [TestCase("/version=0", 0)]
            [TestCase("/veRSioN=1", 1)]
            [TestCase("/VERSION=20051012059193", 20051012059193L)]
            public void ShouldHaveVersionPropertySetIfRequested(string commandLine, long expectedVersion)
            {
                var result = ParseCommandLine(commandLine);

                Assert.AreEqual(expectedVersion, result.Version);
            }

            [Test]
            [TestCase(@"/runner=""Namespace.Type, AssemblyName""", "Namespace.Type, AssemblyName")]
            [TestCase("/RUNNER=Namespace.Type,AssemblyName", "Namespace.Type,AssemblyName")]
            public void ShouldHaveRunnerQualifiedNamePropertySetIfRequested(string commandLine, string expectedRunnerQualifiedName)
            {
                var result = ParseCommandLine(commandLine);

                Assert.AreEqual(expectedRunnerQualifiedName, result.RunnerQualifiedName);
            }

            [Test]
            [TestCase("/scripts=folder", "folder")]
            [TestCase(@"/SCRIPTS=""C:\Program Files\folder""", @"C:\Program Files\folder")]
            public void ShouldHaveScriptsPathPropertySetIfRequested(string commandLine, string expectedScriptsPath)
            {
                var result = ParseCommandLine(commandLine);

                Assert.AreEqual(expectedScriptsPath, result.ScriptsPath);
            }

            [Test]
            [TestCase(@"/CoNnectiONSTRing=""My Connection String""", "My Connection String")]
            [TestCase(@"/connectionstring=Server=a;Database=b;Trusted_Connection=c;", "Server=a;Database=b;Trusted_Connection=c;")]
            public void ShouldHaveConnectionStringPropertySetIfRequested(string commandLine, string expectedConnectionString)
            {
                var result = ParseCommandLine(commandLine);

                Assert.AreEqual(expectedConnectionString, result.ConnectionString);
            }
        }
    }
}