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

        public event EventHandler<ScriptStartedEventArgs> OnScriptStarted;
        public event EventHandler<EventArgs> OnScriptCompleted;

        public MigrationService(IMigrationFinder migrationFinder, IRunnerFactory runnerFactory)
        {
            _migrationFinder = migrationFinder;
            _runnerFactory = runnerFactory;
        }

        public void Up()
        {
            var upScripts = GetAndVerifyUpScripts();
            
            if (!upScripts.Any())
            {
                return;
            }

            using (var runner = _runnerFactory.Create())
            {
                var executedMigrations = runner.GetExecutedMigrations();

                foreach (var migration in ExcludeExecutedUpScripts(upScripts, executedMigrations).OrderBy(x => x.Version))
                {
                    RaiseEvent(OnScriptStarted, this, new ScriptStartedEventArgs(migration.Version, migration.Name));

                    runner.ExecuteUpScript(migration);

                    RaiseEvent(OnScriptCompleted, this, EventArgs.Empty);
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

        private List<UpMigration> GetAndVerifyUpScripts()
        {
            var upScripts = _migrationFinder.GetUpScripts().ToList();

            if (upScripts.GroupBy(u => u.Version).Any(g => g.Count() > 1))
            {
                throw new DuplicateMigrationVersionException();
            }

            return upScripts;
        }

        private IEnumerable<DownMigration> GetAndVerifyDownScripts()
        {
            var downScripts = _migrationFinder.GetDownScripts().ToList();

            if (downScripts.GroupBy(u => u.Version).Any(g => g.Count() > 1))
            {
                throw new DuplicateMigrationVersionException();
            }

            return downScripts;
        }

        private static IEnumerable<UpMigration> ExcludeExecutedUpScripts(IEnumerable<UpMigration> upScripts, IEnumerable<long> executedMigrations)
        {
            return upScripts.Where(x => !executedMigrations.Contains(x.Version));
        }

        private void RemoveMigrations(IRunner runner, IEnumerable<long> migrationsToRemove)
        {
            var downScripts = GetAndVerifyDownScripts().ToDictionary(x => x.Version);
            
            foreach (var executedMigration in migrationsToRemove.OrderByDescending(x => x))
            {
                DownMigration migration;
                
                if (!downScripts.TryGetValue(executedMigration, out migration))
                {
                    throw new MigrationScriptMissingException();
                }

                RaiseEvent(OnScriptStarted, this, new ScriptStartedEventArgs(migration.Version, migration.Name));

                runner.ExecuteDownScript(migration);

                RaiseEvent(OnScriptCompleted, this, EventArgs.Empty);
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