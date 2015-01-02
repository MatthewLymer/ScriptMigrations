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

            private class GivenThereAreNoUpMigrations : GivenAnEmptyDatabase
            {
                [TestFixture]
                public class WhenTellingTheUpMigrationServiceToPerformAnUp : GivenThereAreNoUpMigrations
                {
                    [Test]
                    public void ShouldNotCreateInstanceOfRunner()
                    {
                        // arrange
                        var mockMigrationFinder = new Mock<IMigrationFinder>();
                        var migrationService = new MigrationService(mockMigrationFinder.Object, _mockRunnerFactory.Object);

                        // act
                        migrationService.Up();

                        // assert
                        _mockRunnerFactory.Verify(x => x.Create(), Times.Never);
                    }
                }
            }

            private class GivenThereIsOneUpMigration : GivenAnEmptyDatabase
            {
                private Mock<IMigrationFinder> _mockMigrationFinder;
                private readonly UpMigration _upMigration = new UpMigration(25, "my-migration", () => "");

                [SetUp]
                public new void BeforeEachTest()
                {
                    _mockMigrationFinder = new Mock<IMigrationFinder>();

                    _mockMigrationFinder.Setup(x => x.GetUpMigrations()).Returns(new[] {_upMigration});
                }

                [TestFixture]
                public class WhenTellingTheUpMigrationServiceToPerformAnUp : GivenThereIsOneUpMigration
                {
                    private MigrationService _migrationService;

                    [SetUp]
                    public new void BeforeEachTest()
                    {
                        _migrationService = new MigrationService(_mockMigrationFinder.Object, _mockRunnerFactory.Object);
                    }

                    [Test]
                    public void ShouldExecuteTheMigrationThenCommit()
                    {
                        // act
                        _migrationService.Up();

                        // assert
                        _mockRunner.Verify(x => x.ExecuteUpMigration(It.IsAny<UpMigration>()), Times.Once);
                        _mockRunner.Verify(x => x.Commit(), Times.Once);
                    }

                    [Test]
                    public void ShouldFireMigrationStartedEventBeforeInvokingRunner()
                    {
                        var eventFired = false;
                        var upMigrationFired = false;

                        _migrationService.OnMigrationStarted += (o, args) =>
                        {
                            Assert.AreEqual(_migrationService, o);
                            Assert.AreEqual(_upMigration.Version, args.Version);
                            Assert.AreEqual(_upMigration.Name, args.Name);
                            Assert.IsFalse(upMigrationFired);
                            eventFired = true;
                        };

                        _mockRunner.Setup(x => x.ExecuteUpMigration(It.IsAny<UpMigration>())).Callback(() => upMigrationFired = true);

                        _migrationService.Up();

                        Assert.IsTrue(eventFired);
                    }

                    [Test]
                    public void ShouldFireMigrationCompletedEventAfterInvokingRunner()
                    {
                        var eventFired = false;
                        var upMigrationFired = false;

                        _migrationService.OnMigrationCompleted += (o, args) =>
                        {
                            Assert.AreEqual(_migrationService, o);
                            Assert.AreEqual(EventArgs.Empty, args);
                            Assert.IsTrue(upMigrationFired);
                            eventFired = true;
                        };

                        _mockRunner.Setup(x => x.ExecuteUpMigration(It.IsAny<UpMigration>())).Callback(() => upMigrationFired = true);

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
                        var migrationService = new MigrationService(_mockMigrationFinder.Object, _mockRunnerFactory.Object);

                        _mockRunner.Setup(x => x.ExecuteUpMigration(It.IsAny<UpMigration>()))
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

            private class GivenThereAreMultipleUpMigrations : GivenAnEmptyDatabase
            {
                private Mock<IMigrationFinder> _mockMigrationFinder;
                private List<UpMigration> _upMigrations;

                [SetUp]
                public new void BeforeEachTest()
                {
                    _upMigrations = new List<UpMigration> {
                        new UpMigration(25, "", () => ""),
                        new UpMigration(45, "", () => ""),
                        new UpMigration(13, "", () => "")
                    };

                    _mockMigrationFinder = new Mock<IMigrationFinder>();

                    _mockMigrationFinder.Setup(x => x.GetUpMigrations()).Returns(_upMigrations);
                }

                [TestFixture]
                public class WhenTellingTheUpMigrationServiceToPerformAnUp : GivenThereAreMultipleUpMigrations
                {
                    private MigrationService _migrationService;

                    [SetUp]
                    public new void BeforeEachTest()
                    {
                        _migrationService = new MigrationService(_mockMigrationFinder.Object, _mockRunnerFactory.Object);
                    }

                    [Test]
                    public void ShouldThrowExceptionIfMultipleMigrationMigrationsHaveTheSameVersion()
                    {
                        // arrange
                        var duplicatedUpMigration = new UpMigration(666, "", () => "");
                        _upMigrations.Add(duplicatedUpMigration);
                        _upMigrations.Add(duplicatedUpMigration);

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

                        _mockRunner.Setup(x => x.ExecuteUpMigration(It.IsAny<UpMigration>()))
                            .Callback(new Action<UpMigration>(migration => {
                                Assert.Less(lastVersionExecuted, migration.Version);
                                lastVersionExecuted = migration.Version;
                            }));

                        // act
                        _migrationService.Up();

                        // assert
                        _mockRunner.Verify(x => x.ExecuteUpMigration(It.IsAny<UpMigration>()), Times.Exactly(3));
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

            private void EnsureDownMigrationsExecutedInDescendingOrder()
            {
                var lastVersionExecuted = long.MaxValue;

                _mockRunner.Setup(x => x.ExecuteDownMigration(It.IsAny<DownMigration>()))
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
                private Mock<IMigrationFinder> _mockMigrationFinder;
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

                    _mockMigrationFinder = new Mock<IMigrationFinder>();

                    _mockMigrationFinder.Setup(x => x.GetUpMigrations()).Returns(_upMigrations);
                }

                [Test]
                public void ShouldOnlyExecuteMigrationsWhichHaveNotAlreadyBeenRun()
                {
                    // arrange
                    var migrationService = new MigrationService(_mockMigrationFinder.Object, _mockRunnerFactory.Object);

                    // act
                    migrationService.Up();

                    // assert
                    foreach (var mig in _upMigrations)
                    {
                        var migration = mig;
                        var times = _executedMigrations.Contains(migration.Version) ? Times.Never() : Times.Once();
                        _mockRunner.Verify(x => x.ExecuteUpMigration(migration), times);
                    }
                }
            }

            private class GivenThereIsACompleteSetOfDownMigrations : GivenADatabaseWithMigrations
            {
                private List<DownMigration> _downMigrations;
                private Mock<IMigrationFinder> _mockMigrationFinder;
                private MigrationService _migrationService;

                [SetUp]
                public new void BeforeEachTest()
                {
                    _downMigrations = new List<DownMigration> {
                        new DownMigration(10, "", () => ""),
                        new DownMigration(52, "", () => ""),
                        new DownMigration(96, "", () => "")
                    };

                    _mockMigrationFinder = new Mock<IMigrationFinder>();

                    _mockMigrationFinder.Setup(x => x.GetDownMigrations()).Returns(_downMigrations.AsEnumerable());

                    _migrationService = new MigrationService(_mockMigrationFinder.Object, _mockRunnerFactory.Object);
                }

                [TestFixture]
                public class WhenTellingTheMigrationServiceToPerformADownToZero : GivenThereIsACompleteSetOfDownMigrations
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
                        _mockRunner.Setup(x => x.ExecuteDownMigration(It.IsAny<DownMigration>()))
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
                    public void ShouldExecuteAllDownMigrationsInDescendingOrder()
                    {
                        // arrange
                        EnsureDownMigrationsExecutedInDescendingOrder();

                        // act
                        _migrationService.Down(0);

                        // assert
                        _mockRunner.Verify(x => x.ExecuteDownMigration(It.IsAny<DownMigration>()), Times.Exactly(3));
                        _mockRunner.Verify(x => x.Commit(), Times.Once);
                    }

                    [Test]
                    public void ShouldFireMigrationStartedEventBeforeInvokingRunner()
                    {
                        var migrationsFired = new List<long>();
                        var eventsFired = new List<long>();
                        
                        _migrationService.OnMigrationStarted += (o, args) =>
                        {
                            Assert.AreEqual(_migrationService, o);

                            var migration = _downMigrations.Single(s => s.Version == args.Version);
                            
                            Assert.AreEqual(migration.Version, args.Version);
                            Assert.AreEqual(migration.Name, args.Name);
                            Assert.IsFalse(migrationsFired.Contains(args.Version));
                            eventsFired.Add(args.Version);
                        };

                        _mockRunner.Setup(x => x.ExecuteDownMigration(It.IsAny<DownMigration>())).Callback<DownMigration>(ds => migrationsFired.Add(ds.Version));

                        _migrationService.Down(0);

                        Assert.IsTrue(_downMigrations.All(ds => eventsFired.Contains(ds.Version)));
                    }

                    [Test]
                    public void ShouldFireMigrationCompletedEventAfterInvokingRunner()
                    {
                        var eventFiredCount = 0;
                        var migrationFired = false;

                        _migrationService.OnMigrationCompleted += (o, args) =>
                        {
                            Assert.AreEqual(_migrationService, o);
                            Assert.AreEqual(EventArgs.Empty, args);
                            Assert.IsTrue(migrationFired);

                            eventFiredCount++;
                            migrationFired = false;
                        };

                        _mockRunner.Setup(x => x.ExecuteDownMigration(It.IsAny<DownMigration>())).Callback(() => migrationFired = true);

                        _migrationService.Down(0);

                        Assert.AreEqual(_downMigrations.Count, eventFiredCount);
                    }
                }

                [TestFixture]
                public class WhenTellingTheMigrationServiceToPerformADownToASpecificVersion : GivenThereIsACompleteSetOfDownMigrations
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

                        EnsureDownMigrationsExecutedInDescendingOrder();

                        // act
                        _migrationService.Down(requestedVersion);

                        // assert
                        foreach (var executedMigration in _executedMigrations)
                        {
                            var times = executedMigration > requestedVersion ? Times.Once() : Times.Never();
                            var downMigration = _downMigrations.Single(m => m.Version == executedMigration);
                            _mockRunner.Verify(x => x.ExecuteDownMigration(downMigration), times);
                        }

                        _mockRunner.Verify(x => x.Commit(), Times.Once);
                    }
                }
            }

            private class GivenThereIsAnIncompleteSetOfDownMigrations : GivenADatabaseWithMigrations
            {
                private List<DownMigration> _downMigrations;
                private Mock<IMigrationFinder> _mockMigrationFinder;
                private MigrationService _migrationService;

                [SetUp]
                public new void BeforeEachTest()
                {
                    _downMigrations = new List<DownMigration> {
                        new DownMigration(10, "", () => ""),
                        new DownMigration(96, "", () => "")
                    };

                    _mockMigrationFinder = new Mock<IMigrationFinder>();

                    _mockMigrationFinder.Setup(x => x.GetDownMigrations()).Returns(_downMigrations.AsEnumerable());

                    _migrationService = new MigrationService(_mockMigrationFinder.Object, _mockRunnerFactory.Object);
                }

                [TestFixture]
                public class WhenTellingTheMigrationServiceToPerformADownToZero : GivenThereIsAnIncompleteSetOfDownMigrations
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
                        catch (MigrationMissingException)
                        {
                            // assert
                            Assert.Pass();
                        }
                    }

                    [Test]
                    public void ShouldThrowExceptionIfMultipleMigrationMigrationsHaveTheSameVersion()
                    {
                        // arrange
                        var duplicatedDownMigration = new DownMigration(666, "", () => "");
                        _downMigrations.Add(duplicatedDownMigration);
                        _downMigrations.Add(duplicatedDownMigration);

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
                public class WhenTellingTheMigrationServiceToPerformADownToASpecificVersion : GivenThereIsAnIncompleteSetOfDownMigrations
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
                        catch (MigrationMissingException)
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
