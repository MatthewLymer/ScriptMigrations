using System.Linq;
using FluentValidation.Results;
using MigratorConsole.CommandLine;
using MigratorConsole.Properties;
using NUnit.Framework;

namespace MigratorConsole.Tests.CommandLine
{
    class MigratorCommandLineParserModelValidatorTests
    {
        [TestFixture]
        public class WhenValidatingAMigratorCommandLineParserModelInstance
        {
            private MigratorCommandLineParserModelValidator _validator;

            private static void AssertError(ValidationResult result, string errorMessage)
            {
                Assert.IsFalse(result.IsValid);

                CollectionAssert.Contains(result.Errors.Select(e => e.ErrorMessage), errorMessage);
            }

            private void TestValidatorForError(MigratorCommandLineParserModel model, string errorMessage)
            {
                var result = _validator.Validate(model);

                AssertError(result, errorMessage);
            }

            [SetUp]
            public void BeforeEachTest()
            {
                _validator = new MigratorCommandLineParserModelValidator();
            }

            [Test]
            [TestCase(null)]
            [TestCase("")]
            [TestCase("    ")]
            public void ShouldFailIfRunnerQualifiedNameIsEmptyWhenMigrating(string runnerQualifiedName)
            {
                var model = new MigratorCommandLineParserModel 
                {
                    RunnerQualifiedName = runnerQualifiedName,
                    MigrateUp = true,
                    MigrateDown = true
                };

                TestValidatorForError(model, Resources.RunnerQualifiedNameIsRequired);
            }

            [Test]
            [TestCase("justanassembly")]
            [TestCase(", justatype")]
            public void ShouldFailIfRunnerQualifiedNameIsBadFormat(string runnerQualifiedName)
            {
                var model = new MigratorCommandLineParserModel
                {
                    RunnerQualifiedName = runnerQualifiedName
                };

                TestValidatorForError(model, Resources.RunnerQualifiedNameBadFormat);
            }

            [Test]
            [TestCase(null)]
            [TestCase("")]
            [TestCase("   ")]
            public void ShouldFailIfConnectionStringIsEmptyWhenMigrating(string connectionString)
            {
                var model = new MigratorCommandLineParserModel
                {
                    ConnectionString = connectionString,
                    MigrateUp = true,
                    MigrateDown = true
                };

                TestValidatorForError(model, Resources.ConnectionStringIsRequired);
            }

            [Test]
            [TestCase(null)]
            [TestCase("")]
            [TestCase("   ")]
            public void ShouldFailIfScriptsPathIsEmptyWhenMigrating(string scriptsPath)
            {
                var model = new MigratorCommandLineParserModel
                {
                    ScriptsPath = scriptsPath,
                    MigrateUp = true,
                    MigrateDown = true
                };

                TestValidatorForError(model, Resources.ScriptsPathIsRequired);
            }

            [Test]
            public void ShouldFailIfVersionIsNullWhenMigratingDown()
            {
                var model = new MigratorCommandLineParserModel
                {
                    Version = null,
                    MigrateDown = true
                };

                TestValidatorForError(model, Resources.VersionIsRequired);
            }

            [Test]
            public void ShouldFailIfVersionIsNegative()
            {
                var model = new MigratorCommandLineParserModel
                {
                    Version = -5L
                };               
 
                TestValidatorForError(model, Resources.VersionMustBeZeroOrMore);
            }

            [Test]
            public void ShouldFailIfMigrateUpAndMigrateDownAreBothSet()
            {
                var model = new MigratorCommandLineParserModel 
                {
                    MigrateUp = true,
                    MigrateDown = true
                };

                TestValidatorForError(model, Resources.MigrateUpDownMutuallyExclusive);
            }

            [Test]
            public void ShouldFailIfShowHelpAndMigrateUpAreBothSet()
            {
                var model = new MigratorCommandLineParserModel
                {
                    ShowHelp = true,
                    MigrateUp = true
                };

                TestValidatorForError(model, Resources.ShowHelpMigrateMutuallyExclusive);
            }

            [Test]
            public void ShouldFailIfShowHelpAndMigrateDownAreBothSet()
            {
                var model = new MigratorCommandLineParserModel
                {
                    ShowHelp = true,
                    MigrateDown = true
                };

                TestValidatorForError(model, Resources.ShowHelpMigrateMutuallyExclusive);
            }

            [Test]
            [TestCase(false, false, false, null, "", "", "")]
            [TestCase(true, false, false, null, "", "", "")]
            [TestCase(false, true, false, null, "assembly, namespace.type", "C:/windows", "server=blah")]
            [TestCase(false, false, true, 0L, "assembly, namespace.class+type", "C:/windows", "server=blah")]
            [TestCase(false, false, true, 5L, "assembly, type", "C:/windows", "server=blah")]
            public void ShouldBeValid(bool showHelp, bool migrateUp, bool migrateDown, long? version, string runnerQualifiedName, string scriptsPath, string connectionString)
            {
                var model = new MigratorCommandLineParserModel {
                    ShowHelp = showHelp,
                    MigrateDown = migrateDown,
                    MigrateUp = migrateUp,
                    Version = version,
                    RunnerQualifiedName = runnerQualifiedName,
                    ScriptsPath = scriptsPath,
                    ConnectionString = connectionString
                };

                var result = _validator.Validate(model);

                Assert.IsTrue(result.IsValid);
            }
        }
    }
}
