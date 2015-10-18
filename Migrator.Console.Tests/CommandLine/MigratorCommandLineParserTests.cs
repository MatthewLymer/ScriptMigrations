using Migrator.Console.CommandLine;
using NUnit.Framework;

namespace Migrator.Console.Tests.CommandLine
{
    class MigratorCommandLineParserTests
    {
        [TestFixture]
        public class WhenTellingMigratorCommandLineParserTestsToParseACommandLineArray
        {
            private static MigratorCommandLineParserModel ParseCommandLine(string commandLine)
            {
                var args = NativeMethods.CommandLineToArgs("foo.exe " + commandLine);

                var parser = new MigratorCommandLineParser<MigratorCommandLineParserModel>();

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
            [TestCase("/up")]
            [TestCase("/UP")]
            [TestCase("/uP")]
            public void ShouldHaveCommandPropertySetToMigrateUpIfRequested(string commandLine)
            {
                var result = ParseCommandLine(commandLine);

                Assert.IsTrue(result.MigrateUp);
            }

            [Test]
            [TestCase("/down")]
            [TestCase("/DoWn")]
            [TestCase("/DOWN")]
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