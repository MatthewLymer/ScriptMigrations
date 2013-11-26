using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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

        class GivenThereIsNoMigrationToRun : GivenAnEmptyDatabase
        {
            [TestFixture]
            public class WhenTellingTheMigrationServiceToPerformAnUp : GivenThereIsNoMigrationToRun
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

        class GivenThereIsOneMigration : GivenAnEmptyDatabase
        {
            private Mock<IScriptFinder> _mockScriptFinder;

            [SetUp]
            public new void BeforeEachTest()
            {
                _mockScriptFinder = new Mock<IScriptFinder>();

                _mockScriptFinder.Setup(x => x.GetUpMigration()).Returns(new[]{new UpMigration(25)});
            }

            [TestFixture]
            public class WhenTellingTheMigrationServiceToPerformAnUp : GivenThereIsOneMigration
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

        class GivenThereAreMultipleMigrations : GivenAnEmptyDatabase
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

                _mockScriptFinder.Setup(x => x.GetUpMigration()).Returns(_upMigrations);
            }

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

    class GivenADatabaseWithMigrations
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

        [TestFixture]
        public class WhenTellingTheMigrationServiceToPerformAn : GivenADatabaseWithMigrations
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

                _mockScriptFinder.Setup(x => x.GetUpMigration()).Returns(_upMigrations);
            }

            [Test]
            public void ShouldOnlyExecuteMigrationsWhichHaveNotAlreadyBeenRun()
            {
                // arrange
                var executedMigrations = new List<long> { 10, 52 };

                var migrationService = new MigrationService(_mockScriptFinder.Object, _mockRunnerFactory.Object);

                // act
                migrationService.Up();

                // assert
                foreach (var migration in _upMigrations)
                {
                    //var times = executedMigrations.Contains(migration.Version) ? Times.Never : Times.Once;
                    var wasRunBefore = executedMigrations.Contains(migration.Version);

                    if (wasRunBefore)
                    {
                        _mockRunner.
                    }

                    _mockRunner.Verify(x => x.ExecuteUpMigration(migration), times);
                }
            }
        }
    }

    public class MigrationFailedException : Exception
    {
    }

    public class UpMigration
    {
        public UpMigration(long version)
        {
            Version = version;
        }

        public long Version { get; private set; }
    }

    public interface IRunner : IDisposable
    {
        void Commit();
        void ExecuteUpMigration(UpMigration migration);
    }

    public class MigrationService
    {
        private readonly IScriptFinder _scriptFinder;
        private readonly IRunnerFactory _runnerFactory;

        public MigrationService(IScriptFinder scriptFinder, IRunnerFactory runnerFactory)
        {
            _scriptFinder = scriptFinder;
            _runnerFactory = runnerFactory;
        }

        public void Up()
        {
            var migrations = _scriptFinder.GetUpMigration();

            if (!migrations.Any())
            {
                return;
            }

            using (var runner = _runnerFactory.Create())
            {
                foreach (var migration in migrations.OrderBy(x => x.Version))
                {
                    runner.ExecuteUpMigration(migration);
                }

                runner.Commit();
            }
        }
    }

    public interface IScriptFinder
    {
        IEnumerable<UpMigration> GetUpMigration();
    }


    public interface IRunnerFactory
    {
        IRunner Create();
    }
}
