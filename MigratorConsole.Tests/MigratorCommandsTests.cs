using System;
using System.Linq;
using System.Runtime.Remoting.Channels;
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

                Commands = new MigratorCommands(MockConsoleWrapper.Object, MockMigrationServiceFactory.Object);
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

                Commands.MigrateUp(runnerQualifiedName, string.Empty, string.Empty);
                
                AssertAssemblyLoadFailure(runnerQualifiedName);
            }

            [Test]
            public void ShouldGiveErrorIfRunnerFactoryTypeCannotBeInstanciated()
            {
                const string runnerQualifiedName = "MigratorConsole.Tests, notatypename";

                Commands.MigrateUp(runnerQualifiedName, string.Empty, string.Empty);

                AssertTypeLoadFailure(runnerQualifiedName);
            }

            [Test]
            [TestCase("server=foo", "C:/zorp")]
            [TestCase("server=bar", "C:/part")]
            public void ShouldExecuteUpOnMigrationService(string connectionString, string scriptsPath)
            {
                var mockMigrationService = new Mock<IMigrationService>();

                MockMigrationServiceFactory
                    .Setup(x => x.Create(scriptsPath, It.Is<StubRunnerFactory>(s => s.ConnectionString == connectionString)))
                    .Returns(mockMigrationService.Object);

                var type = typeof (StubRunnerFactory);

                Commands.MigrateUp(CreateQualifiedName(type), connectionString, scriptsPath);

                mockMigrationService.Verify(x => x.Up(), Times.Once);
            }

            [Test]
            [TestCase(1, "my-script")]
            public void ShouldWriteWhenUpScriptStartedEventFires(long version, string scriptName)
            {
                var mockMigrationService = new Mock<IMigrationService>();

                MockMigrationServiceFactory
                    .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<IRunnerFactory>()))
                    .Returns(mockMigrationService.Object);

                var type = typeof(StubRunnerFactory);

                var eventArgs = new UpScriptStartedEventArgs(new UpScript(version, scriptName, "select * from nothing"));
                mockMigrationService.Setup(x => x.Up()).Callback(() => mockMigrationService.Raise(x => x.OnUpScriptStartedEvent += null, eventArgs));

                Commands.MigrateUp(CreateQualifiedName(type), "", "");

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

                Commands.MigrateDown(runnerQualifiedName, string.Empty, string.Empty, 0);

                AssertAssemblyLoadFailure(runnerQualifiedName);                
            }

            [Test]
            public void ShouldGiveErrorIfRunnerFactoryTypeCannotBeInstanciated()
            {
                const string runnerQualifiedName = "MigratorConsole.Tests, notatypename";

                Commands.MigrateDown(runnerQualifiedName, string.Empty, string.Empty, 0);

                AssertTypeLoadFailure(runnerQualifiedName);
            }

            [Test]
            [TestCase("server=foo", "C:/zorp", 0)]
            [TestCase("server=baz", "C:/ping", 1)]
            [TestCase("server=bar", "C:/part", 2)]
            public void ShouldExecuteDownOnMigrationService(string connectionString, string scriptsPath, long version)
            {
                var mockMigrationService = new Mock<IMigrationService>();

                MockMigrationServiceFactory
                    .Setup(x => x.Create(scriptsPath, It.Is<StubRunnerFactory>(s => s.ConnectionString == connectionString)))
                    .Returns(mockMigrationService.Object);

                var type = typeof(StubRunnerFactory);

                Commands.MigrateDown(CreateQualifiedName(type), connectionString, scriptsPath, version);

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

        public class StubRunnerFactory : IRunnerFactory
        {
            public StubRunnerFactory(string connectionString)
            {
                ConnectionString = connectionString;
            }

            public string ConnectionString { get; private set; }

            public IRunner Create()
            {
                throw new NotImplementedException();
            }
        }
    }
}
