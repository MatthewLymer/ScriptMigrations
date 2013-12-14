﻿using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;

namespace Migrator.Tests
{
    class GivenAnEmptyDatabase
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

        class GivenThereIsNoUpMigrationToRun : GivenAnEmptyDatabase
        {
            [TestFixture]
            public class WhenTellingTheUpMigrationServiceToPerformAnUp : GivenThereIsNoUpMigrationToRun
            {
                [Test]
                public void ShouldNotCreateInstanceOfRunner()
                {
                    // arrange
                    var mockScriptFinder = new Mock<IScriptFinder>();
                    var migrationService = new MigrationService(mockScriptFinder.Object, _mockRunnerFactory.Object);

                    // act
                    migrationService.Up();

                    // assert
                    _mockRunnerFactory.Verify(x => x.Create(), Times.Never);
                }
            }
        }

        class GivenThereIsOneUpScript : GivenAnEmptyDatabase
        {
            private Mock<IScriptFinder> _mockScriptFinder;

            [SetUp]
            public new void BeforeEachTest()
            {
                _mockScriptFinder = new Mock<IScriptFinder>();

                _mockScriptFinder.Setup(x => x.GetUpScripts()).Returns(new[]{new UpScript(25, "", "")});
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
                    _mockRunner.Verify(x => x.ExecuteUpMigration(It.IsAny<UpScript>()), Times.Once);
                    _mockRunner.Verify(x => x.Commit(), Times.Once);
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

                    _mockRunner.Setup(x => x.ExecuteUpMigration(It.IsAny<UpScript>())).Throws<MigrationFailedException>();

                    // act
                    try
                    {
                        migrationService.Up();
                        Assert.Fail("Exception never occured");
                    }
                    catch (MigrationFailedException)
                    {
                        // assert
                        _mockRunner.Verify(x => x.Commit(), Times.Never);                        
                    }
                }
            }
        }

        class GivenThereAreMultipleUpMigrations : GivenAnEmptyDatabase
        {
            private Mock<IScriptFinder> _mockScriptFinder;
            private List<UpScript> _upMigrations;

            [SetUp]
            public new void BeforeEachTest()
            {
                _upMigrations = new List<UpScript>
                {
                    new UpScript(25, "", ""),
                    new UpScript(45, "", ""),
                    new UpScript(13, "", "")
                };

                _mockScriptFinder = new Mock<IScriptFinder>();

                _mockScriptFinder.Setup(x => x.GetUpScripts()).Returns(_upMigrations);
            }

            [TestFixture]
            public class WhenTellingTheUpMigrationServiceToPerformAnUp : GivenThereAreMultipleUpMigrations
            {
                [Test]
                public void ShouldRunAllMigrationsInAscendingOrder()
                {
                    // arrange
                    long lastVersionExecuted = 0;

                    _mockRunner.Setup(x => x.ExecuteUpMigration(It.IsAny<UpScript>()))
                        .Callback(new Action<UpScript>(migration =>
                        {
                            Assert.Less(lastVersionExecuted, migration.Version);
                            lastVersionExecuted = migration.Version;
                        }));

                    var migrationService = new MigrationService(_mockScriptFinder.Object, _mockRunnerFactory.Object);

                    // act
                    migrationService.Up();

                    // assert
                    _mockRunner.Verify(x => x.ExecuteUpMigration(It.IsAny<UpScript>()), Times.Exactly(3));
                    _mockRunner.Verify(x => x.Commit(), Times.Once);
                }                
            }
        }
    }

    class GivenADatabaseWithMigrations
    {
        private List<long> _executedMigrations;

        private Mock<IRunnerFactory> _mockRunnerFactory;
        private Mock<IRunner> _mockRunner;

        protected void EnsureDownScriptsExecutedInDescendingOrder()
        {
            var lastVersionExecuted = long.MaxValue;

            _mockRunner.Setup(x => x.ExecuteDownScript(It.IsAny<DownScript>()))
                .Callback(new Action<DownScript>(migration => {
                    Assert.Greater(lastVersionExecuted, migration.Version);
                    lastVersionExecuted = migration.Version;
                }));
        }

        [SetUp]
        public void BeforeEachTest()
        {
            _mockRunnerFactory = new Mock<IRunnerFactory>();
            _mockRunner = new Mock<IRunner>();

            _executedMigrations = new List<long> { 10, 52, 96 };

            _mockRunnerFactory.Setup(x => x.Create()).Returns(_mockRunner.Object);

            _mockRunner.Setup(x => x.GetExecutedMigrations()).Returns(_executedMigrations.AsEnumerable());
        }

        [TestFixture]
        public class WhenTellingTheMigrationServiceToPerformAnUp : GivenADatabaseWithMigrations
        {
            private Mock<IScriptFinder> _mockScriptFinder;
            private List<UpScript> _upMigrations;

            [SetUp]
            public new void BeforeEachTest()
            {
                _upMigrations = new List<UpScript>
                {
                    new UpScript(10, "", ""),
                    new UpScript(15, "", ""),
                    new UpScript(52, "", ""),
                    new UpScript(88, "", "")
                };

                _mockScriptFinder = new Mock<IScriptFinder>();

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
                    _mockRunner.Verify(x => x.ExecuteUpMigration(migration), times);
                }
            }
        }

        class GivenThereIsACompleteSetOfDownScripts : GivenADatabaseWithMigrations
        {
            private List<DownScript> _downScripts;
            private Mock<IScriptFinder> _mockScriptFinder;
            private MigrationService _migrationService;

            [SetUp]
            public new void BeforeEachTest()
            {
                _downScripts = new List<DownScript>
                {
                    new DownScript(10),
                    new DownScript(52),
                    new DownScript(96)
                };

                _mockScriptFinder = new Mock<IScriptFinder>();

                _mockScriptFinder.Setup(x => x.GetDownScripts()).Returns(_downScripts.AsEnumerable());

                _migrationService = new MigrationService(_mockScriptFinder.Object, _mockRunnerFactory.Object);
            }

            [TestFixture]
            public class WhenTellingTheMigrationServiceToPerformADownToZero : GivenThereIsACompleteSetOfDownScripts
            {
                [Test]
                public void ShouldNotCommitIfThereWasAnExceptionWhenExecutingMigration()
                {
                    // arrange
                    _mockRunner.Setup(x => x.ExecuteDownScript(It.IsAny<DownScript>())).Throws<MigrationFailedException>();

                    // act
                    try
                    {
                        _migrationService.DownToZero();
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
                    _migrationService.DownToZero();

                    // assert
                    _mockRunner.Verify(x => x.ExecuteDownScript(It.IsAny<DownScript>()), Times.Exactly(3));
                    _mockRunner.Verify(x => x.Commit(), Times.Once);
                }
            }

            [TestFixture]
            public class WhenTellingTheMigrationServiceToPerformADownToASpecificVersion : GivenThereIsACompleteSetOfDownScripts
            {
                [Test]
                public void ShouldThrowExceptionIfVersionIsLessThanOne()
                {
                    try
                    {
                        // act
                        _migrationService.DownToVersion(0);
                        Assert.Fail();
                    }
                    catch (ArgumentException e)
                    {
                        // assert
                        Assert.AreEqual("version", e.ParamName);
                    }
                }

                [Test]
                public void ShouldThrowExceptionIfVersionRequestedDoesNotExist()
                {
                    try
                    {
                        // act
                        _migrationService.DownToVersion(15);
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
                    _migrationService.DownToVersion(requestedVersion);

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

        class GivenThereIsAnIncompleteSetOfDownScripts : GivenADatabaseWithMigrations
        {
            private List<DownScript> _downScripts;
            private Mock<IScriptFinder> _mockScriptFinder;
            private MigrationService _migrationService;

            [SetUp]
            public new void BeforeEachTest()
            {
                _downScripts = new List<DownScript>
                {
                    new DownScript(10),
                    new DownScript(96)
                };

                _mockScriptFinder = new Mock<IScriptFinder>();

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
                        _migrationService.DownToZero();
                        Assert.Fail();
                    }
                    catch (MigrationScriptMissingException)
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
                        _migrationService.DownToVersion(10);
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
