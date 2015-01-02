using System;
using System.Linq;
using SystemWrappers.Interfaces;
using Migrator;
using Migrator.Runners;
using MigratorConsole.Properties;
using Moq;
using NUnit.Framework;

// ReSharper disable ImplicitlyCapturedClosure

namespace MigratorConsole.Tests
{
    class MigratorCommandsTests
    {
        internal class GivenAMigratorCommandsInstance
        {
            protected const int FailureExitCode = 1;
            protected const string RunnerQualifiedName = "MyRunnerAssembly, MyRunnerFactoryType";
            protected const string ConnectionString = "server=localhost";
            protected const string ScriptsPath = ".";

            private Mock<IActivatorFacade> _mockActivatorFacade;
            private Mock<IMigrationServiceFactory> _mockMigrationServiceFactory;

            protected Mock<IConsole> MockConsole { get; private set; }
            protected MigratorCommands Commands { get; private set; }

            protected static string CreateQualifiedName(Type type)
            {
                var assemblyName = type.Assembly.FullName.Split(',')[0];
                return string.Format("{0}, {1}", assemblyName, type.FullName);
            }

            protected void AssertAssemblyLoadFailure(string runnerQualifiedName)
            {
                MockConsole.Verify(x => x.WriteErrorLine(Resources.CouldNotLoadRunnerAssemblyFormat, runnerQualifiedName));

                Assert.AreEqual(FailureExitCode, Environment.ExitCode);
            }

            protected void AssertTypeLoadFailure(string runnerQualifiedName)
            {
                MockConsole.Verify(x => x.WriteErrorLine(Resources.CouldNotCreateRunnerFactoryType, runnerQualifiedName));

                Assert.AreEqual(FailureExitCode, Environment.ExitCode);
            }

            protected void SetupActivatorFacade(string runnerQualifiedName, string connectionString, ActivatorResultCode resultCode)
            {
                _mockActivatorFacade.Setup(
                    x => x.CreateInstance<IRunnerFactory>(runnerQualifiedName, connectionString))
                    .Returns(new ActivatorResult<IRunnerFactory>(resultCode));
            }

            protected void SetupActivatorFacade(string runnerQualifiedName, string connectionString, IRunnerFactory runnerFactory)
            {
                _mockActivatorFacade.Setup(
                    x => x.CreateInstance<IRunnerFactory>(runnerQualifiedName, connectionString))
                    .Returns(new ActivatorResult<IRunnerFactory>(runnerFactory));
            }

            protected void SetupMigrationServiceFactory(IRunnerFactory runnerFactory, string scriptsPath, IMigrationService migrationService)
            {
                _mockMigrationServiceFactory
                    .Setup(x => x.Create(scriptsPath, runnerFactory))
                    .Returns(migrationService);
            }

            protected void SetupActivatorWithServiceFactory(string runnerQualifiedName, string connectionString, string scriptsPath, IMigrationService migrationService)
            {
                var runnerFactory = new Mock<IRunnerFactory>().Object;
                SetupActivatorFacade(runnerQualifiedName, connectionString, runnerFactory);
                SetupMigrationServiceFactory(runnerFactory, scriptsPath, migrationService);
            }

            [SetUp]
            public void BeforeEachTest()
            {
                MockConsole = new Mock<IConsole>();
                _mockMigrationServiceFactory = new Mock<IMigrationServiceFactory>();
                _mockActivatorFacade = new Mock<IActivatorFacade>();

                Commands = new MigratorCommands(MockConsole.Object, _mockMigrationServiceFactory.Object, _mockActivatorFacade.Object);
            }
        }

        [TestFixture]
        public class WhenTellingMigratorCommandsToShowHelp : GivenAMigratorCommandsInstance
        {
            [Test]
            public void ShouldWriteHelpInformationToConsole()
            {
                Commands.ShowHelp();

                MockConsole.Verify(x => x.WriteLine(Resources.HelpUsage));
            }
        }

        [TestFixture]
        public class WhenTellingMigratorCommandsToShowErrors : GivenAMigratorCommandsInstance
        {
            [Test]
            public void ShouldDisplayHeaderMessage()
            {
                Commands.ShowErrors(Enumerable.Empty<string>());

                MockConsole.Verify(x => x.WriteErrorLine(Resources.ErrorHeading));
            }

            [Test]
            public void ShouldListAllErrors()
            {
                var errors = new[]
                {
                    "You forgot to flip the vip",
                    "Cascade Failure"
                };

                Commands.ShowErrors(errors);

                foreach (var error in errors)
                {
                    var currentError = error;
                    MockConsole.Verify(x => x.WriteErrorLine("> {0}", currentError));
                }
            }

            [Test]
            public void ShouldSetExitCode()
            {
                const int expectedExitCode = 1;

                Commands.ShowErrors(Enumerable.Empty<string>());

                Assert.AreEqual(expectedExitCode, Environment.ExitCode);
            }
        }

        [TestFixture]
        public class WhenTellingMigratorCommandsToMigrateUp : GivenAMigratorCommandsInstance
        {
            [Test]
            public void ShouldGiveErrorIfAssemblyCannotBeFound()
            {
                const string runnerQualifiedName = "notanassembly, notatypename";

                SetupActivatorFacade(runnerQualifiedName, ConnectionString, ActivatorResultCode.UnableToResolveAssembly);
                
                Commands.MigrateUp(runnerQualifiedName, ConnectionString, string.Empty);
                
                AssertAssemblyLoadFailure(runnerQualifiedName);
            }

            [Test]
            public void ShouldGiveErrorIfRunnerFactoryTypeCannotBeInstanciated()
            {
                const string runnerQualifiedName = "MigratorConsole.Tests, notatypename";

                SetupActivatorFacade(runnerQualifiedName, ConnectionString, ActivatorResultCode.UnableToResolveType);

                Commands.MigrateUp(runnerQualifiedName, ConnectionString, string.Empty);

                AssertTypeLoadFailure(runnerQualifiedName);
            }
            
            [Test]
            [TestCase("server=foo", "C:/zorp")]
            [TestCase("server=bar", "C:/part")]
            public void ShouldExecuteUpOnMigrationService(string connectionString, string scriptsPath)
            {
                var mockMigrationService = new Mock<IMigrationService>();

                SetupActivatorWithServiceFactory(RunnerQualifiedName, connectionString, scriptsPath, mockMigrationService.Object);

                Commands.MigrateUp(RunnerQualifiedName, connectionString, scriptsPath);

                mockMigrationService.Verify(x => x.Up(), Times.Once);
            }

            [Test]
            [TestCase(1, "my-script")]
            [TestCase(2, "your-script")]
            public void ShouldWriteWhenScriptStarts(long version, string scriptName)
            {
                var mockMigrationService = new Mock<IMigrationService>();

                SetupActivatorWithServiceFactory(RunnerQualifiedName, ConnectionString, ScriptsPath, mockMigrationService.Object);

                var eventArgs = new MigrationStartedEventArgs(version, scriptName);
                mockMigrationService.Setup(x => x.Up()).Callback(() => mockMigrationService.Raise(x => x.OnMigrationStarted += null, eventArgs));

                Commands.MigrateUp(RunnerQualifiedName, ConnectionString, ScriptsPath);

                MockConsole.Verify(x => x.Write(Resources.StartingMigrationMessageFormat, version, scriptName));
            }

            [Test]
            public void ShouldWriteWhenMigrationCompletes()
            {
                var mockMigrationService = new Mock<IMigrationService>();

                SetupActivatorWithServiceFactory(RunnerQualifiedName, ConnectionString, ScriptsPath, mockMigrationService.Object);

                mockMigrationService.Setup(x => x.Up()).Callback(() => mockMigrationService.Raise(x => x.OnMigrationCompleted += null, EventArgs.Empty));

                Commands.MigrateUp(RunnerQualifiedName, ConnectionString, ScriptsPath);

                MockConsole.Verify(x => x.WriteLine(Resources.CompletedMigrationMessage));                
            }
        }

        [TestFixture]
        public class WhenTellingMigratorCommandsToMigrateDown : GivenAMigratorCommandsInstance
        {
            [Test]
            public void ShouldGiveErrorIfAssemblyCannotBeFound()
            {
                const string assemblyMissingQualifiedName = "notanassembly, notatypename";

                const ActivatorResultCode resultCode = ActivatorResultCode.UnableToResolveAssembly;
                
                SetupActivatorFacade(assemblyMissingQualifiedName, ConnectionString, resultCode);

                Commands.MigrateDown(assemblyMissingQualifiedName, ConnectionString, string.Empty, 0);

                AssertAssemblyLoadFailure(assemblyMissingQualifiedName);                
            }

            [Test]
            public void ShouldGiveErrorIfRunnerFactoryTypeCannotBeInstanciated()
            {
                const string typeMissingQualifiedName = "MigratorConsole.Tests, notatypename";

                SetupActivatorFacade(typeMissingQualifiedName, ConnectionString, ActivatorResultCode.UnableToResolveType);

                Commands.MigrateDown(typeMissingQualifiedName, ConnectionString, string.Empty, 0);

                AssertTypeLoadFailure(typeMissingQualifiedName);
            }

            [Test]
            [TestCase("server=foo", "C:/zorp", 0)]
            [TestCase("server=baz", "C:/ping", 1)]
            [TestCase("server=bar", "C:/part", 2)]
            public void ShouldExecuteDownOnMigrationService(string connectionString, string scriptsPath, long version)
            {
                var mockMigrationService = new Mock<IMigrationService>();

                SetupActivatorWithServiceFactory(RunnerQualifiedName, connectionString, scriptsPath, mockMigrationService.Object);

                Commands.MigrateDown(RunnerQualifiedName, connectionString, scriptsPath, version);

                mockMigrationService.Verify(x => x.Down(version), Times.Once);
            }

            [Test]
            [TestCase(1, "my-script")]
            [TestCase(2, "your-script")]
            public void ShouldWriteWhenScriptStarts(long version, string scriptName)
            {
                var mockMigrationService = new Mock<IMigrationService>();

                SetupActivatorWithServiceFactory(RunnerQualifiedName, ConnectionString, ScriptsPath, mockMigrationService.Object);

                var eventArgs = new MigrationStartedEventArgs(version, scriptName);

                mockMigrationService.Setup(x => x.Down(version)).Callback(() => mockMigrationService.Raise(x => x.OnMigrationStarted += null, eventArgs));

                Commands.MigrateDown(RunnerQualifiedName, ConnectionString, ScriptsPath, version);

                MockConsole.Verify(x => x.Write(Resources.StartingMigrationMessageFormat, version, scriptName));                
            }

            [Test]
            public void ShouldWriteWhenScriptCompletes()
            {
                var mockMigrationService = new Mock<IMigrationService>();

                SetupActivatorWithServiceFactory(RunnerQualifiedName, ConnectionString, ScriptsPath, mockMigrationService.Object);

                mockMigrationService.Setup(x => x.Down(0)).Callback(() => mockMigrationService.Raise(x => x.OnMigrationCompleted += null, EventArgs.Empty));

                Commands.MigrateDown(RunnerQualifiedName, ConnectionString, ScriptsPath, 0);

                MockConsole.Verify(x => x.WriteLine(Resources.CompletedMigrationMessage));
            }
        }
    }
}
