using System;
using System.Collections.Generic;
using System.Configuration;
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

        class GivenThereIsOneUpMigration : GivenAnEmptyDatabase
        {
            private Mock<IScriptFinder> _mockScriptFinder;

            [SetUp]
            public new void BeforeEachTest()
            {
                _mockScriptFinder = new Mock<IScriptFinder>();

                _mockScriptFinder.Setup(x => x.GetUpMigrations()).Returns(new[]{new UpMigration(25)});
            }

            [TestFixture]
            public class WhenTellingTheUpMigrationServiceToPerformAnUp : GivenThereIsOneUpMigration
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
                    _mockRunner.Verify(x => x.ExecuteUpMigration(It.IsAny<UpMigration>()), Times.Once);
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

                    _mockRunner.Setup(x => x.ExecuteUpMigration(It.IsAny<UpMigration>())).Throws<MigrationFailedException>();

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
            private List<UpMigration> _upMigrations;

            [SetUp]
            public new void BeforeEachTest()
            {
                _upMigrations = new List<UpMigration>
                {
                    new UpMigration(25),
                    new UpMigration(45),
                    new UpMigration(13)
                };

                _mockScriptFinder = new Mock<IScriptFinder>();

                _mockScriptFinder.Setup(x => x.GetUpMigrations()).Returns(_upMigrations);
            }

            [TestFixture]
            public class WhenTellingTheUpMigrationServiceToPerformAnUp : GivenThereAreMultipleUpMigrations
            {
                [Test]
                public void ShouldRunAllMigrationsInAscendingOrder()
                {
                    // arrange
                    long lastVersionExecuted = 0;

                    _mockRunner.Setup(x => x.ExecuteUpMigration(It.IsAny<UpMigration>()))
                        .Callback(new Action<UpMigration>(migration =>
                        {
                            Assert.Less(lastVersionExecuted, migration.Version);
                            lastVersionExecuted = migration.Version;
                        }));

                    var migrationService = new MigrationService(_mockScriptFinder.Object, _mockRunnerFactory.Object);

                    // act
                    migrationService.Up();

                    // assert
                    _mockRunner.Verify(x => x.ExecuteUpMigration(It.IsAny<UpMigration>()), Times.Exactly(3));
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

        [SetUp]
        public void BeforeEachTest()
        {
            _mockRunnerFactory = new Mock<IRunnerFactory>();
            _mockRunner = new Mock<IRunner>();

            _executedMigrations = new List<long> { 10, 52 };

            _mockRunnerFactory.Setup(x => x.Create()).Returns(_mockRunner.Object);

            _mockRunner.Setup(x => x.GetExecutedMigrations()).Returns(_executedMigrations.AsEnumerable());
        }

        [TestFixture]
        public class WhenTellingTheMigrationServiceToPerformAnUp : GivenADatabaseWithMigrations
        {
            private Mock<IScriptFinder> _mockScriptFinder;
            private List<UpMigration> _upMigrations;

            [SetUp]
            public new void BeforeEachTest()
            {
                _upMigrations = new List<UpMigration>
                {
                    new UpMigration(10),
                    new UpMigration(15),
                    new UpMigration(52),
                    new UpMigration(88)
                };

                _mockScriptFinder = new Mock<IScriptFinder>();

                _mockScriptFinder.Setup(x => x.GetUpMigrations()).Returns(_upMigrations);
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

        class GivenThereAreNoDownMigrations : GivenADatabaseWithMigrations
        {
            private Mock<IScriptFinder> _mockScriptFinder;
            private MigrationService _migrationService;

            [SetUp]
            public new void BeforeEachTest()
            {
                _mockScriptFinder = new Mock<IScriptFinder>();

                _mockScriptFinder.Setup(x => x.GetDownMigrations()).Returns(new DownMigration[0]);

                _migrationService = new MigrationService(_mockScriptFinder.Object, _mockRunnerFactory.Object);
            }

            [TestFixture]
            public class WhenTellingTheMigrationServiceToPerformADownToZero : GivenThereAreNoDownMigrations
            {
                [Test]
                public void ShouldThrowException()
                {
                    try
                    {
                        // act
                        _migrationService.Down(0);
                        Assert.Fail();
                    }
                    catch (MissingDownMigrationException)
                    {
                        // assert
                        Assert.Pass();
                    }
                }
            }
        }

        class GivenThereIsACompleteSetOfDownMigrations : GivenADatabaseWithMigrations
        {
            private List<DownMigration> _downMigrations;
            private Mock<IScriptFinder> _mockScriptFinder;
            private MigrationService _migrationService;

            [SetUp]
            public new void BeforeEachTest()
            {
                _downMigrations = new List<DownMigration>
                {
                    new DownMigration(10),
                    new DownMigration(52)
                };

                _mockScriptFinder = new Mock<IScriptFinder>();

                _mockScriptFinder.Setup(x => x.GetDownMigrations()).Returns(_downMigrations.AsEnumerable());

                _migrationService = new MigrationService(_mockScriptFinder.Object, _mockRunnerFactory.Object);
            }

            [TestFixture]
            public class WhenTellingTheMigrationServiceToPerformADownToZero : GivenThereIsACompleteSetOfDownMigrations
            {
                [Test]
                public void ShouldNotCommitIfThereWasAnExceptionWhenExecutingMigration()
                {
                    // arrange
                    _mockRunner.Setup(x => x.ExecuteDownMigration(It.IsAny<DownMigration>())).Throws<MigrationFailedException>();

                    // act
                    try
                    {
                        _migrationService.Down(0);
                        Assert.Fail("Exception never occured");
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
                    var lastVersionExecuted = long.MaxValue;

                    _mockRunner.Setup(x => x.ExecuteDownMigration(It.IsAny<DownMigration>()))
                        .Callback(new Action<DownMigration>(migration =>
                        {
                            Assert.Greater(lastVersionExecuted, migration.Version);
                            lastVersionExecuted = migration.Version;
                        }));

                    // act
                    _migrationService.Down(0);

                    // assert
                    _mockRunner.Verify(x => x.ExecuteDownMigration(It.IsAny<DownMigration>()), Times.Exactly(2));
                    _mockRunner.Verify(x => x.Commit(), Times.Once);
                }
            }

            [TestFixture]
            public class WhenTellingTheMigrationServiceToPerformADownToASpecificVersion : GivenThereIsACompleteSetOfDownMigrations
            {
                [Test]
                public void ShouldThrowExceptionIfVersionDoesNotExist()
                {
                    try
                    {
                        // act
                        _migrationService.Down(15);
                        Assert.Fail();
                    }
                    catch (MigrationVersionNeverExecutedException)
                    {
                        // assert
                        Assert.Pass();
                    }
                }
            }
        }
    }
}
