using System;
using System.Collections.Generic;
using System.Linq;
using Migrator.Exceptions;
using Migrator.Migrations;
using Migrator.Runners;
using Moq;
using NUnit.Framework;

// ReSharper disable ImplicitlyCapturedClosure

namespace Migrator.Tests
{
    internal class MigrationServiceTests
    {
        private class GivenAnEmptyDatabase
        {
            private Mock<IRunnerFactory> _mockRunnerFactory;
            private Mock<IRunner> _mockRunner;

            [SetUp]
            public void BeforeEachTest()
            {
                _mockRunnerFactory = new Mock<IRunnerFactory>();
                _mockRunner = new Mock<IRunner>();

                _mockRunnerFactory.Setup(x => x.Create()).Returns(_mockRunner.Object);
            }

            private class GivenThereAreNoUpScripts : GivenAnEmptyDatabase
            {
                [TestFixture]
                public class WhenTellingTheUpMigrationServiceToPerformAnUp : GivenThereAreNoUpScripts
                {
                    [Test]
                    public void ShouldNotCreateInstanceOfRunner()
                    {
                        // arrange
                        var mockScriptFinder = new Mock<IMigrationFinder>();
                        var migrationService = new MigrationService(mockScriptFinder.Object, _mockRunnerFactory.Object);

                        // act
                        migrationService.Up();

                        // assert
                        _mockRunnerFactory.Verify(x => x.Create(), Times.Never);
                    }
                }
            }

            private class GivenThereIsOneUpScript : GivenAnEmptyDatabase
            {
                private Mock<IMigrationFinder> _mockScriptFinder;
                private readonly UpMigration _upMigration = new UpMigration(25, "my-migration", () => "");

                [SetUp]
                public new void BeforeEachTest()
                {
                    _mockScriptFinder = new Mock<IMigrationFinder>();

                    _mockScriptFinder.Setup(x => x.GetUpScripts()).Returns(new[] {_upMigration});
                }

                [TestFixture]
                public class WhenTellingTheUpMigrationServiceToPerformAnUp : GivenThereIsOneUpScript
                {
                    private MigrationService _migrationService;

                    [SetUp]
                    public new void BeforeEachTest()
                    {
                        _migrationService = new MigrationService(_mockScriptFinder.Object, _mockRunnerFactory.Object);
                    }

                    [Test]
                    public void ShouldExecuteTheMigrationThenCommit()
                    {
                        // act
                        _migrationService.Up();

                        // assert
                        _mockRunner.Verify(x => x.ExecuteUpScript(It.IsAny<UpMigration>()), Times.Once);
                        _mockRunner.Verify(x => x.Commit(), Times.Once);
                    }

                    [Test]
                    public void ShouldFireScriptStartedEventBeforeInvokingRunner()
                    {
                        var eventFired = false;
                        var upScriptFired = false;

                        _migrationService.OnScriptStarted += (o, args) =>
                        {
                            Assert.AreEqual(_migrationService, o);
                            Assert.AreEqual(_upMigration.Version, args.Version);
                            Assert.AreEqual(_upMigration.Name, args.ScriptName);
                            Assert.IsFalse(upScriptFired);
                            eventFired = true;
                        };

                        _mockRunner.Setup(x => x.ExecuteUpScript(It.IsAny<UpMigration>())).Callback(() => upScriptFired = true);

                        _migrationService.Up();

                        Assert.IsTrue(eventFired);
                    }

                    [Test]
                    public void ShouldFireScriptCompletedEventAfterInvokingRunner()
                    {
                        var eventFired = false;
                        var upScriptFired = false;

                        _migrationService.OnScriptCompleted += (o, args) =>
                        {
                            Assert.AreEqual(_migrationService, o);
                            Assert.AreEqual(EventArgs.Empty, args);
                            Assert.IsTrue(upScriptFired);
                            eventFired = true;
                        };

                        _mockRunner.Setup(x => x.ExecuteUpScript(It.IsAny<UpMigration>())).Callback(() => upScriptFired = true);

                        _migrationService.Up();

                        Assert.IsTrue(eventFired);
                    }

                    [Test]
                    public void ShouldDisposeOfRunner()
                    {
                        // act
                        _migrationService.Up();

                        // assert
                        _mockRunner.Verify(x => x.Dispose(), Times.Once);
                    }

                    [Test]
                    public void ShouldNotCommitIfThereWasAnExceptionWhenExecutingMigration()
                    {
                        // arrange
                        var migrationService = new MigrationService(_mockScriptFinder.Object, _mockRunnerFactory.Object);

                        _mockRunner.Setup(x => x.ExecuteUpScript(It.IsAny<UpMigration>()))
                            .Throws<MigrationFailedException>();

                        // act
                        try
                        {
                            migrationService.Up();
                            Assert.Fail();
                        }
                        catch (MigrationFailedException)
                        {
                            // assert
                            _mockRunner.Verify(x => x.Commit(), Times.Never);
                        }
                    }
                }
            }

            private class GivenThereAreMultipleUpScripts : GivenAnEmptyDatabase
            {
                private Mock<IMigrationFinder> _mockScriptFinder;
                private List<UpMigration> _upMigrations;

                [SetUp]
                public new void BeforeEachTest()
                {
                    _upMigrations = new List<UpMigration> {
                        new UpMigration(25, "", () => ""),
                        new UpMigration(45, "", () => ""),
                        new UpMigration(13, "", () => "")
                    };

                    _mockScriptFinder = new Mock<IMigrationFinder>();

                    _mockScriptFinder.Setup(x => x.GetUpScripts()).Returns(_upMigrations);
                }

                [TestFixture]
                public class WhenTellingTheUpMigrationServiceToPerformAnUp : GivenThereAreMultipleUpScripts
                {
                    private MigrationService _migrationService;

                    [SetUp]
                    public new void BeforeEachTest()
                    {
                        _migrationService = new MigrationService(_mockScriptFinder.Object, _mockRunnerFactory.Object);
                    }

                    [Test]
                    public void ShouldThrowExceptionIfMultipleMigrationScriptsHaveTheSameVersion()
                    {
                        // arrange
                        var duplicatedUpScript = new UpMigration(666, "", () => "");
                        _upMigrations.Add(duplicatedUpScript);
                        _upMigrations.Add(duplicatedUpScript);

                        try
                        {
                            // act
                            _migrationService.Up();
                            Assert.Fail();
                        }
                        catch (DuplicateMigrationVersionException)
                        {
                            // assert
                            Assert.Pass();
                        }
                    }

                    [Test]
                    public void ShouldRunAllMigrationsInAscendingOrder()
                    {
                        // arrange
                        long lastVersionExecuted = 0;

                        _mockRunner.Setup(x => x.ExecuteUpScript(It.IsAny<UpMigration>()))
                            .Callback(new Action<UpMigration>(migration => {
                                Assert.Less(lastVersionExecuted, migration.Version);
                                lastVersionExecuted = migration.Version;
                            }));

                        // act
                        _migrationService.Up();

                        // assert
                        _mockRunner.Verify(x => x.ExecuteUpScript(It.IsAny<UpMigration>()), Times.Exactly(3));
                        _mockRunner.Verify(x => x.Commit(), Times.Once);
                    }
                }
            }
        }

        private class GivenADatabaseWithMigrations
        {
            private List<long> _executedMigrations;

            private Mock<IRunnerFactory> _mockRunnerFactory;
            private Mock<IRunner> _mockRunner;

            private void EnsureDownScriptsExecutedInDescendingOrder()
            {
                var lastVersionExecuted = long.MaxValue;

                _mockRunner.Setup(x => x.ExecuteDownScript(It.IsAny<DownMigration>()))
                    .Callback(new Action<DownMigration>(migration => {
                        Assert.Greater(lastVersionExecuted, migration.Version);
                        lastVersionExecuted = migration.Version;
                    }));
            }

            [SetUp]
            public void BeforeEachTest()
            {
                _mockRunnerFactory = new Mock<IRunnerFactory>();
                _mockRunner = new Mock<IRunner>();

                _executedMigrations = new List<long> {10, 52, 96};

                _mockRunnerFactory.Setup(x => x.Create()).Returns(_mockRunner.Object);

                _mockRunner.Setup(x => x.GetExecutedMigrations()).Returns(_executedMigrations.AsEnumerable());
            }

            [TestFixture]
            public class WhenTellingTheMigrationServiceToPerformAnUp : GivenADatabaseWithMigrations
            {
                private Mock<IMigrationFinder> _mockScriptFinder;
                private List<UpMigration> _upMigrations;

                [SetUp]
                public new void BeforeEachTest()
                {
                    _upMigrations = new List<UpMigration> {
                        new UpMigration(10, "", () => ""),
                        new UpMigration(15, "", () => ""),
                        new UpMigration(52, "", () => ""),
                        new UpMigration(88, "", () => "")
                    };

                    _mockScriptFinder = new Mock<IMigrationFinder>();

                    _mockScriptFinder.Setup(x => x.GetUpScripts()).Returns(_upMigrations);
                }

                [Test]
                public void ShouldOnlyExecuteMigrationsWhichHaveNotAlreadyBeenRun()
                {
                    // arrange
                    var migrationService = new MigrationService(_mockScriptFinder.Object, _mockRunnerFactory.Object);

                    // act
                    migrationService.Up();

                    // assert
                    foreach (var mig in _upMigrations)
                    {
                        var migration = mig;
                        var times = _executedMigrations.Contains(migration.Version) ? Times.Never() : Times.Once();
                        _mockRunner.Verify(x => x.ExecuteUpScript(migration), times);
                    }
                }
            }

            private class GivenThereIsACompleteSetOfDownScripts : GivenADatabaseWithMigrations
            {
                private List<DownMigration> _downScripts;
                private Mock<IMigrationFinder> _mockScriptFinder;
                private MigrationService _migrationService;

                [SetUp]
                public new void BeforeEachTest()
                {
                    _downScripts = new List<DownMigration> {
                        new DownMigration(10, "", () => ""),
                        new DownMigration(52, "", () => ""),
                        new DownMigration(96, "", () => "")
                    };

                    _mockScriptFinder = new Mock<IMigrationFinder>();

                    _mockScriptFinder.Setup(x => x.GetDownScripts()).Returns(_downScripts.AsEnumerable());

                    _migrationService = new MigrationService(_mockScriptFinder.Object, _mockRunnerFactory.Object);
                }

                [TestFixture]
                public class WhenTellingTheMigrationServiceToPerformADownToZero : GivenThereIsACompleteSetOfDownScripts
                {
                    [Test]
                    public void ShouldThrowExceptionIfVersionIsOutOfRange()
                    {
                        try
                        {
                            // act
                            _migrationService.Down(-1);
                            Assert.Fail();
                        }
                        catch (ArgumentOutOfRangeException e)
                        {
                            // assert
                            Assert.AreEqual("version", e.ParamName);
                        }
                    }
                    
                    [Test]
                    public void ShouldNotCommitIfThereWasAnExceptionWhenExecutingMigration()
                    {
                        // arrange
                        _mockRunner.Setup(x => x.ExecuteDownScript(It.IsAny<DownMigration>()))
                            .Throws<MigrationFailedException>();

                        // act
                        try
                        {
                            _migrationService.Down(0);
                            Assert.Fail();
                        }
                        catch (MigrationFailedException)
                        {
                            // assert
                            _mockRunner.Verify(x => x.Commit(), Times.Never);
                        }
                    }

                    [Test]
                    public void ShouldExecuteAllDownScriptsInDescendingOrder()
                    {
                        // arrange
                        EnsureDownScriptsExecutedInDescendingOrder();

                        // act
                        _migrationService.Down(0);

                        // assert
                        _mockRunner.Verify(x => x.ExecuteDownScript(It.IsAny<DownMigration>()), Times.Exactly(3));
                        _mockRunner.Verify(x => x.Commit(), Times.Once);
                    }

                    [Test]
                    public void ShouldFireScriptStartedEventBeforeInvokingRunner()
                    {
                        var scriptsFired = new List<long>();
                        var eventsFired = new List<long>();
                        
                        _migrationService.OnScriptStarted += (o, args) =>
                        {
                            Assert.AreEqual(_migrationService, o);

                            var script = _downScripts.Single(s => s.Version == args.Version);
                            
                            Assert.AreEqual(script.Version, args.Version);
                            Assert.AreEqual(script.Name, args.ScriptName);
                            Assert.IsFalse(scriptsFired.Contains(args.Version));
                            eventsFired.Add(args.Version);
                        };

                        _mockRunner.Setup(x => x.ExecuteDownScript(It.IsAny<DownMigration>())).Callback<DownMigration>(ds => scriptsFired.Add(ds.Version));

                        _migrationService.Down(0);

                        Assert.IsTrue(_downScripts.All(ds => eventsFired.Contains(ds.Version)));
                    }

                    [Test]
                    public void ShouldFireScriptCompletedEventAfterInvokingRunner()
                    {
                        var eventFiredCount = 0;
                        var scriptFired = false;

                        _migrationService.OnScriptCompleted += (o, args) =>
                        {
                            Assert.AreEqual(_migrationService, o);
                            Assert.AreEqual(EventArgs.Empty, args);
                            Assert.IsTrue(scriptFired);

                            eventFiredCount++;
                            scriptFired = false;
                        };

                        _mockRunner.Setup(x => x.ExecuteDownScript(It.IsAny<DownMigration>())).Callback(() => scriptFired = true);

                        _migrationService.Down(0);

                        Assert.AreEqual(_downScripts.Count, eventFiredCount);
                    }
                }

                [TestFixture]
                public class WhenTellingTheMigrationServiceToPerformADownToASpecificVersion : GivenThereIsACompleteSetOfDownScripts
                {
                    [Test]
                    public void ShouldThrowExceptionIfVersionRequestedDoesNotExist()
                    {
                        try
                        {
                            // act
                            _migrationService.Down(15);
                            Assert.Fail();
                        }
                        catch (VersionNeverExecutedException)
                        {
                            // assert
                            Assert.Pass();
                        }
                    }

                    [Test]
                    public void ShouldExecuteAllDownMigrationsThatAreHigherThanRequestedVersionInDescendingOrder()
                    {
                        // arrange
                        const int requestedVersion = 10;

                        EnsureDownScriptsExecutedInDescendingOrder();

                        // act
                        _migrationService.Down(requestedVersion);

                        // assert
                        foreach (var executedMigration in _executedMigrations)
                        {
                            var times = executedMigration > requestedVersion ? Times.Once() : Times.Never();
                            var downMigration = _downScripts.Single(m => m.Version == executedMigration);
                            _mockRunner.Verify(x => x.ExecuteDownScript(downMigration), times);
                        }

                        _mockRunner.Verify(x => x.Commit(), Times.Once);
                    }
                }
            }

            private class GivenThereIsAnIncompleteSetOfDownScripts : GivenADatabaseWithMigrations
            {
                private List<DownMigration> _downScripts;
                private Mock<IMigrationFinder> _mockScriptFinder;
                private MigrationService _migrationService;

                [SetUp]
                public new void BeforeEachTest()
                {
                    _downScripts = new List<DownMigration> {
                        new DownMigration(10, "", () => ""),
                        new DownMigration(96, "", () => "")
                    };

                    _mockScriptFinder = new Mock<IMigrationFinder>();

                    _mockScriptFinder.Setup(x => x.GetDownScripts()).Returns(_downScripts.AsEnumerable());

                    _migrationService = new MigrationService(_mockScriptFinder.Object, _mockRunnerFactory.Object);
                }

                [TestFixture]
                public class WhenTellingTheMigrationServiceToPerformADownToZero : GivenThereIsAnIncompleteSetOfDownScripts
                {
                    [Test]
                    public void ShouldThrowAnException()
                    {
                        try
                        {
                            // act
                            _migrationService.Down(0);
                            Assert.Fail();
                        }
                        catch (MigrationScriptMissingException)
                        {
                            // assert
                            Assert.Pass();
                        }
                    }

                    [Test]
                    public void ShouldThrowExceptionIfMultipleMigrationScriptsHaveTheSameVersion()
                    {
                        // arrange
                        var duplicatedDownScript = new DownMigration(666, "", () => "");
                        _downScripts.Add(duplicatedDownScript);
                        _downScripts.Add(duplicatedDownScript);

                        try
                        {
                            // act
                            _migrationService.Down(0);
                            Assert.Fail();
                        }
                        catch (DuplicateMigrationVersionException)
                        {
                            // assert
                            Assert.Pass();
                        }
                    }
                }

                [TestFixture]
                public class WhenTellingTheMigrationServiceToPerformADownToASpecificVersion : GivenThereIsAnIncompleteSetOfDownScripts
                {
                    [Test]
                    public void ShouldThrowAnException()
                    {
                        try
                        {
                            // act
                            _migrationService.Down(10);
                            Assert.Fail();
                        }
                        catch (MigrationScriptMissingException)
                        {
                            // assert
                            Assert.Pass();
                        }
                    }
                }
            }
        }
    }
}
