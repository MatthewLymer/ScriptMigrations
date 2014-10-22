using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
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
            protected Mock<IConsoleWrapper> MockConsoleWrapper { get; private set; }
            protected MigratorCommands Commands { get; private set; }

            [SetUp]
            public void BeforeEachTest()
            {
                MockConsoleWrapper = new Mock<IConsoleWrapper>();
                Commands = new MigratorCommands(MockConsoleWrapper.Object);
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
            private const int FailureExitCode = 1;

            [Test]
            public void ShouldGiveErrorIfAssemblyCannotBeFound()
            {
                const string assemblyName = "notanassembly";

                var qualifiedName = string.Format("{0}, notatypename", assemblyName);

                Commands.MigrateUp(qualifiedName, string.Empty, string.Empty);
                
                MockConsoleWrapper.Verify(x => x.WriteErrorLine(Resources.CouldNotLoadRunnerAssemblyFormat, assemblyName));

                Assert.AreEqual(FailureExitCode, Environment.ExitCode);
            }

            [Test]
            public void ShouldGiveErrorIfRunnerFactoryTypeCannotBeInstanciated()
            {
                const string typeName = "notatypename";

                var qualifiedName = string.Format("MigratorConsole.Tests, {0}", typeName);

                Commands.MigrateUp(qualifiedName, string.Empty, string.Empty);

                MockConsoleWrapper.Verify(x => x.WriteErrorLine(Resources.CouldNotCreateRunnerFactoryType, typeName));

                Assert.AreEqual(FailureExitCode, Environment.ExitCode);
            }
        }
    }
}
