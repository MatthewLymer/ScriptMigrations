using System;
using System.Collections.Generic;
using System.Linq;
using Migrator.Exceptions;
using Migrator.Migrations;
using Migrator.Runners;

namespace Migrator
{
    public sealed class MigrationService : IMigrationService
    {
        private readonly IMigrationFinder _migrationFinder;
        private readonly IRunnerFactory _runnerFactory;

        public event EventHandler<MigrationStartedEventArgs> OnMigrationStarted;
        public event EventHandler<EventArgs> OnMigrationCompleted;

        public MigrationService(IMigrationFinder migrationFinder, IRunnerFactory runnerFactory)
        {
            _migrationFinder = migrationFinder;
            _runnerFactory = runnerFactory;
        }

        public void Up()
        {
            var upMigrations = GetAndVerifyUpMigrations();
            
            if (!upMigrations.Any())
            {
                return;
            }

            using (var runner = _runnerFactory.Create())
            {
                var executedMigrations = runner.GetExecutedMigrations();

                foreach (var migration in ExcludeExecutedUpMigrations(upMigrations, executedMigrations).OrderBy(x => x.Version))
                {
                    RaiseEvent(OnMigrationStarted, this, new MigrationStartedEventArgs(migration.Version, migration.Name));

                    runner.ExecuteUpMigration(migration);

                    RaiseEvent(OnMigrationCompleted, this, EventArgs.Empty);
                }

                runner.Commit();
            }
        }

        public void Down(long version)
        {
            if (version < 0)
            {
                throw new ArgumentOutOfRangeException("version");
            }

            using (var runner = _runnerFactory.Create())
            {
                var executedMigrations = runner.GetExecutedMigrations().ToList();

                if (version > 0 && !executedMigrations.Contains(version))
                {
                    throw new VersionNeverExecutedException();
                }

                RemoveMigrations(runner, executedMigrations.Where(x => x > version));

                runner.Commit();
            }
        }

        private List<UpMigration> GetAndVerifyUpMigrations()
        {
            var migrations = _migrationFinder.GetUpMigrations().ToList();

            if (migrations.GroupBy(u => u.Version).Any(g => g.Count() > 1))
            {
                throw new DuplicateMigrationVersionException();
            }

            return migrations;
        }

        private IEnumerable<DownMigration> GetAndVerifyDownMigrations()
        {
            var migrations = _migrationFinder.GetDownMigrations().ToList();

            if (migrations.GroupBy(u => u.Version).Any(g => g.Count() > 1))
            {
                throw new DuplicateMigrationVersionException();
            }

            return migrations;
        }

        private static IEnumerable<UpMigration> ExcludeExecutedUpMigrations(IEnumerable<UpMigration> migrations, IEnumerable<long> executedMigrations)
        {
            return migrations.Where(x => !executedMigrations.Contains(x.Version));
        }

        private void RemoveMigrations(IRunner runner, IEnumerable<long> migrationsToRemove)
        {
            var migrations = GetAndVerifyDownMigrations().ToDictionary(x => x.Version);
            
            foreach (var executedMigration in migrationsToRemove.OrderByDescending(x => x))
            {
                DownMigration migration;
                
                if (!migrations.TryGetValue(executedMigration, out migration))
                {
                    throw new MigrationMissingException();
                }

                RaiseEvent(OnMigrationStarted, this, new MigrationStartedEventArgs(migration.Version, migration.Name));

                runner.ExecuteDownMigration(migration);

                RaiseEvent(OnMigrationCompleted, this, EventArgs.Empty);
            }
        }

        private static void RaiseEvent<T>(EventHandler<T> @event, object sender, T args) where T : EventArgs
        {
            if (@event != null)
            {
                @event(sender, args);
            }
        }
    }
}