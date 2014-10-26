using System;
using System.Collections.Generic;
using System.Linq;
using Migrator.Exceptions;
using Migrator.Runners;
using Migrator.Scripts;

namespace Migrator
{
    public sealed class MigrationService : IMigrationService
    {
        private readonly IScriptFinder _scriptFinder;
        private readonly IRunnerFactory _runnerFactory;

        public event Action<object, UpScriptStartedEventArgs> OnUpScriptStartedEvent;

        public MigrationService(IScriptFinder scriptFinder, IRunnerFactory runnerFactory)
        {
            _scriptFinder = scriptFinder;
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
                    runner.ExecuteUpScript(migration);
                }

                runner.Commit();
            }
        }

        public void DownToZero()
        {
            PerformDown(0);
        }

        public void DownToVersion(long version)
        {
            if (version <= 0)
            {
                throw new ArgumentOutOfRangeException("version");
            }

            PerformDown(version);
        }

        private List<UpScript> GetAndVerifyUpScripts()
        {
            var upScripts = _scriptFinder.GetUpScripts().ToList();

            if (upScripts.GroupBy(u => u.Version).Any(g => g.Count() > 1))
            {
                throw new DuplicateMigrationVersionException();
            }

            return upScripts;
        }

        private void PerformDown(long version)
        {
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

        private IEnumerable<DownScript> GetAndVerifyDownScripts()
        {
            var downScripts = _scriptFinder.GetDownScripts().ToList();

            if (downScripts.GroupBy(u => u.Version).Any(g => g.Count() > 1))
            {
                throw new DuplicateMigrationVersionException();
            }

            return downScripts;
        }

        private static IEnumerable<UpScript> ExcludeExecutedUpScripts(IEnumerable<UpScript> upScripts, IEnumerable<long> executedMigrations)
        {
            return upScripts.Where(x => !executedMigrations.Contains(x.Version));
        }

        private void RemoveMigrations(IRunner runner, IEnumerable<long> migrationsToRemove)
        {
            var downScripts = GetAndVerifyDownScripts().ToDictionary(x => x.Version);
            
            foreach (var executedMigration in migrationsToRemove.OrderByDescending(x => x))
            {
                DownScript downScript;
                
                if (!downScripts.TryGetValue(executedMigration, out downScript))
                {
                    throw new MigrationScriptMissingException();
                }

                runner.ExecuteDownScript(downScript);
            }
        }
    }
}