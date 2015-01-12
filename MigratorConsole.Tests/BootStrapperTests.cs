﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using MigratorConsole.CommandLine;
using Moq;
using NUnit.Framework;

namespace MigratorConsole.Tests
{
    class BootstrapperTests
    {
        [TestFixture]
        public class WhenCreatingABoostrapperInstance
        {
            [Test]
            [ExpectedException(typeof(ArgumentNullException))]
            public void ShouldThrowExceptionForArgumentMigratorCommands()
            {
                Assert.IsNotNull(new Bootstrapper(null, null));
            }

            [Test]
            [ExpectedException(typeof(ArgumentNullException))]
            public void ShouldThrowExceptionForArgumentCommandLineBinder()
            {
                var migratorCommands = new Mock<IMigratorCommands>().Object;
                Assert.IsNotNull(new Bootstrapper(migratorCommands, null));
            }
        }

        [TestFixture]
        public class WhenTellingBootstrapperToStart
        {
            private readonly static Expression<Action<IMigratorCommands>>[] MigratorCommandsExpressions = {
                m => m.ShowHelp(),
                m => m.MigrateUp(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                m => m.MigrateDown(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>())
            };

            private Mock<IMigratorCommands> _mockMigratorCommands;
            private Mock<ICommandLineBinder<MigratorCommandLineParserModel>> _mockCommandLineBinder;
            
            private Bootstrapper _bootstrapper;

            private void InvokeBootstrapper(MigratorCommandLineParserModel model)
            {
                InvokeBootstrapper(model, Enumerable.Empty<string>());
            }

            private void InvokeBootstrapper(IEnumerable<string> errors)
            {
                InvokeBootstrapper(new MigratorCommandLineParserModel(), errors);
            }

            private void InvokeBootstrapper(MigratorCommandLineParserModel model, IEnumerable<string> errors)
            {
                var bindingResult = new CommandLineBinderResult<MigratorCommandLineParserModel>(model, errors);

                var args = new string[0];

                _mockCommandLineBinder.Setup(x => x.Bind(args)).Returns(bindingResult);

                _bootstrapper.Start(args);
            }

            private void AssertOneCommandExecuted(Expression<Action<IMigratorCommands>> expression)
            {
                var forbiddenExpressions = MigratorCommandsExpressions.Where(ae => GetExpressionMethodName(ae) != GetExpressionMethodName(expression));

                foreach (var forbiddenExpression in forbiddenExpressions)
                {
                    _mockMigratorCommands.Verify(forbiddenExpression, Times.Never);
                }

                _mockMigratorCommands.Verify(expression, Times.Once);
            }

            private static string GetExpressionMethodName(Expression<Action<IMigratorCommands>> expression)
            {
                return ((MethodCallExpression)expression.Body).Method.Name;
            }

            [SetUp]
            public void BeforeEachTest()
            {
                _mockMigratorCommands = new Mock<IMigratorCommands>();
                _mockCommandLineBinder = new Mock<ICommandLineBinder<MigratorCommandLineParserModel>>();

                _bootstrapper = new Bootstrapper(_mockMigratorCommands.Object, _mockCommandLineBinder.Object);
            }
            
            [Test]
            public void ShouldExecuteShowHelpCommandIfRequestedExplicitly()
            {
                var model = new MigratorCommandLineParserModel 
                {
                    ShowHelp = true
                };

                InvokeBootstrapper(model);

                AssertOneCommandExecuted(m => m.ShowHelp());
            }

            [Test]
            public void ShouldExecuteShowHelpCommandIfNothingIsExplicitlyAsked()
            {
                var model = new MigratorCommandLineParserModel();

                InvokeBootstrapper(model);

                AssertOneCommandExecuted(m => m.ShowHelp());
            }

            [Test]
            [TestCase("runner", "server=blah", @"C:\scripts\")]
            [TestCase("walker", "server=gorp", @"C:\files\")]
            public void ShouldExecuteMigrateUpCommand(string runnerQualifiedName, string connectionString, string scriptsPath)
            {
                var model = new MigratorCommandLineParserModel {
                    MigrateUp = true,
                    ScriptsPath = scriptsPath,
                    ConnectionString = connectionString,
                    RunnerQualifiedName = runnerQualifiedName
                };

                InvokeBootstrapper(model);

                AssertOneCommandExecuted(m => m.MigrateUp(runnerQualifiedName, connectionString, scriptsPath));
            }

            [Test]
            public void ShouldExecuteShowErrorsCommand()
            {
                var errors = new[] {
                    "You made a boo-boo",
                    "Bad things happened to you"
                };

                InvokeBootstrapper(errors);

                AssertOneCommandExecuted(m => m.ShowErrors(errors));
            }

            [Test]
            [TestCase("runner", "server=blah", @"C:\scripts\", 0)]
            [TestCase("walker", "server=gorp", @"C:\files\", 100)]
            public void ShouldExecuteMigrateDownCommand(string runnerQualifiedName, string connectionString, string scriptsPath, long version)
            {
                var model = new MigratorCommandLineParserModel {
                    MigrateDown = true,
                    RunnerQualifiedName = runnerQualifiedName,
                    ConnectionString = connectionString,
                    ScriptsPath = scriptsPath,
                    Version = version
                };

                InvokeBootstrapper(model);

                AssertOneCommandExecuted(m => m.MigrateDown(runnerQualifiedName, connectionString, scriptsPath, version));
            }
        }
    }
}
