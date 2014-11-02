using System;
using System.Linq;
using Migrator;
using Migrator.Runners;
using Migrator.Scripts;
using MigratorConsole.Properties;
using MigratorConsole.Wrappers;
using Moq;
using NUnit.Framework;

namespace MigratorConsole.Tests
{
    class MigratorCommandsTests
    {
        internal class GivenAMigratorCommandsInstance
        {
            protected const int FailureExitCode = 1;

            protected Mock<IConsoleWrapper> MockConsoleWrapper { get; private set; }

            protected Mock<IMigrationServiceFactory> MockMigrationServiceFactory { get; private set; }

            protected Mock<IActivatorFacade> MockActivatorFacade { get; set; }

            protected MigratorCommands Commands { get; private set; }

            protected static string CreateQualifiedName(Type type)
            {
                var assemblyName = type.Assembly.FullName.Split(',')[0];
                return string.Format("{0}, {1}", assemblyName, type.FullName);
            }

            protected void AssertAssemblyLoadFailure(string runnerQualifiedName)
            {
                MockConsoleWrapper.Verify(x => x.WriteErrorLine(Resources.CouldNotLoadRunnerAssemblyFormat, runnerQualifiedName));

                Assert.AreEqual(FailureExitCode, Environment.ExitCode);
            }

            protected void AssertTypeLoadFailure(string runnerQualifiedName)
            {
                MockConsoleWrapper.Verify(x => x.WriteErrorLine(Resources.CouldNotCreateRunnerFactoryType, runnerQualifiedName));

                Assert.AreEqual(FailureExitCode, Environment.ExitCode);
            }

            [SetUp]
            public void BeforeEachTest()
            {
                MockConsoleWrapper = new Mock<IConsoleWrapper>();
                MockMigrationServiceFactory = new Mock<IMigrationServiceFactory>();
                MockActivatorFacade = new Mock<IActivatorFacade>();

                Commands = new MigratorCommands(MockConsoleWrapper.Object, MockMigrationServiceFactory.Object, MockActivatorFacade.Object);
            }
        }

        [TestFixture]
        public class WhenTellingMigratorCommandsToShowHelp : GivenAMigratorCommandsInstance
        {
            [Test]
            public void ShouldWriteHelpInformationToConsole()
            {
                Commands.ShowHelp();

                MockConsoleWrapper.Verify(x => x.WriteLine(Resources.HelpUsage));
            }
        }

        [TestFixture]
        public class WhenTellingMigratorCommandsToShowErrors : GivenAMigratorCommandsInstance
        {
            [Test]
            public void ShouldDisplayHeaderMessage()
            {
                Commands.ShowErrors(Enumerable.Empty<string>());

                MockConsoleWrapper.Verify(x => x.WriteErrorLine(Resources.ErrorHeading));
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
                    MockConsoleWrapper.Verify(x => x.WriteErrorLine("> {0}", currentError));
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

                MockActivatorFacade.Setup(
                    x => x.CreateInstance<IRunnerFactory>(runnerQualifiedName, It.IsAny<object[]>()))
                    .Returns(new ActivatorResult<IRunnerFactory>(ActivatorResultCode.UnableToResolveAssembly));

                Commands.MigrateUp(runnerQualifiedName, string.Empty, string.Empty);
                
                AssertAssemblyLoadFailure(runnerQualifiedName);
            }

            [Test]
            public void ShouldGiveErrorIfRunnerFactoryTypeCannotBeInstanciated()
            {
                const string runnerQualifiedName = "MigratorConsole.Tests, notatypename";

                MockActivatorFacade.Setup(
                    x => x.CreateInstance<IRunnerFactory>(runnerQualifiedName, It.IsAny<object[]>()))
                    .Returns(new ActivatorResult<IRunnerFactory>(ActivatorResultCode.UnableToResolveType));

                Commands.MigrateUp(runnerQualifiedName, string.Empty, string.Empty);

                AssertTypeLoadFailure(runnerQualifiedName);
            }
            
            [Test]
            [TestCase("server=foo", "C:/zorp")]
            [TestCase("server=bar", "C:/part")]
            public void ShouldExecuteUpOnMigrationService(string connectionString, string scriptsPath)
            {
                const string runnerQualifiedName = "MyRunnerAssembly, MyRunnerFactoryType";

                var runnerFactory = new Mock<IRunnerFactory>().Object;
                

                MockActivatorFacade.Setup(
                    x => x.CreateInstance<IRunnerFactory>(runnerQualifiedName, It.IsAny<object[]>()))
                    .Returns(new ActivatorResult<IRunnerFactory>(runnerFactory));


                var mockMigrationService = new Mock<IMigrationService>();

                MockMigrationServiceFactory
                    .Setup(x => x.Create(scriptsPath, runnerFactory))
                    .Returns(mockMigrationService.Object);

                Commands.MigrateUp(runnerQualifiedName, connectionString, scriptsPath);

                mockMigrationService.Verify(x => x.Up(), Times.Once);
            }

            [Test]
            [TestCase(1, "my-script")]
            public void ShouldWriteWhenUpScriptStartedEventFires(long version, string scriptName)
            {
                const string runnerQualifiedName = "MyRunnerAssembly, MyRunnerFactoryType";

                var runnerFactory = new Mock<IRunnerFactory>().Object;

                MockActivatorFacade.Setup(
                    x => x.CreateInstance<IRunnerFactory>(It.IsAny<string>(), It.IsAny<object[]>()))
                    .Returns(new ActivatorResult<IRunnerFactory>(runnerFactory));

                var mockMigrationService = new Mock<IMigrationService>();

                MockMigrationServiceFactory
                    .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<IRunnerFactory>()))
                    .Returns(mockMigrationService.Object);

                var eventArgs = new UpScriptStartedEventArgs(new UpScript(version, scriptName, "select * from nothing"));
                mockMigrationService.Setup(x => x.Up()).Callback(() => mockMigrationService.Raise(x => x.OnUpScriptStartedEvent += null, eventArgs));

                Commands.MigrateUp(runnerQualifiedName, "", "");

                MockConsoleWrapper.Verify(x => x.Write(Resources.StartingMigrationMessageFormat, version, scriptName));
            }
        }

        [TestFixture]
        public class WhenTellingMigratorCommandsToMigrateDown : GivenAMigratorCommandsInstance
        {
            [Test]
            public void ShouldGiveErrorIfAssemblyCannotBeFound()
            {
                const string runnerQualifiedName = "notanassembly, notatypename";
                const string connectionString = "server=localhost";

                const ActivatorResultCode resultCode = ActivatorResultCode.UnableToResolveAssembly;
                
                SetupActivatorFacade(runnerQualifiedName, connectionString, resultCode);

                Commands.MigrateDown(runnerQualifiedName, connectionString, string.Empty, 0);

                AssertAssemblyLoadFailure(runnerQualifiedName);                
            }

            private void SetupActivatorFacade(string runnerQualifiedName, string connectionString, ActivatorResultCode resultCode)
            {
                MockActivatorFacade.Setup(
                    x => x.CreateInstance<IRunnerFactory>(runnerQualifiedName, connectionString))
                    .Returns(new ActivatorResult<IRunnerFactory>(resultCode));
            }

            [Test]
            public void ShouldGiveErrorIfRunnerFactoryTypeCannotBeInstanciated()
            {
                const string runnerQualifiedName = "MigratorConsole.Tests, notatypename";
                const string connectionString = "server=localhost";

                MockActivatorFacade.Setup(
                    x => x.CreateInstance<IRunnerFactory>(runnerQualifiedName, connectionString))
                    .Returns(new ActivatorResult<IRunnerFactory>(ActivatorResultCode.UnableToResolveType));

                Commands.MigrateDown(runnerQualifiedName, connectionString, string.Empty, 0);

                AssertTypeLoadFailure(runnerQualifiedName);
            }

            [Test]
            [TestCase("server=foo", "C:/zorp", 0)]
            [TestCase("server=baz", "C:/ping", 1)]
            [TestCase("server=bar", "C:/part", 2)]
            public void ShouldExecuteDownOnMigrationService(string connectionString, string scriptsPath, long version)
            {
                const string runnerQualifiedName = "MyAssemblyName, MyTypeName";

                var runnerFactory = new Mock<IRunnerFactory>().Object;

                MockActivatorFacade
                    .Setup(x => x.CreateInstance<IRunnerFactory>(runnerQualifiedName, connectionString))
                    .Returns(new ActivatorResult<IRunnerFactory>(runnerFactory));
                
                var mockMigrationService = new Mock<IMigrationService>();

                MockMigrationServiceFactory
                    .Setup(x => x.Create(scriptsPath, runnerFactory))
                    .Returns(mockMigrationService.Object);
                
                Commands.MigrateDown(runnerQualifiedName, connectionString, scriptsPath, version);

                if (version > 0)
                {
                    mockMigrationService.Verify(x => x.DownToVersion(version), Times.Once);
                    mockMigrationService.Verify(x => x.DownToZero(), Times.Never);   
                }
                else
                {
                    mockMigrationService.Verify(x => x.DownToZero(), Times.Once);
                    mockMigrationService.Verify(x => x.DownToVersion(It.IsAny<long>()), Times.Never);
                }
            }
        }
    }
}
