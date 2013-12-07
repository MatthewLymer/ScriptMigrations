using System;
using System.Collections.Generic;
using System.Linq;

namespace Migrator
{
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
            var upScripts = _scriptFinder.GetUpScripts().ToList();

            if (!upScripts.Any())
            {
                return;
            }

            using (var runner = _runnerFactory.Create())
            {
                var executedMigrations = runner.GetExecutedMigrations();
                
                foreach (var migration in upScripts.Where(x => !executedMigrations.Contains(x.Version)).OrderBy(x => x.Version))
                {
                    runner.ExecuteUpMigration(migration);
                }

                runner.Commit();
            }
        }

        public void DownToZero()
        {
            using (var runner = _runnerFactory.Create())
            {
                RemoveMigrations(runner, runner.GetExecutedMigrations());

                runner.Commit();
            }            
        }

        public void DownToVersion(long version)
        {
            if (version <= 0)
            {
                throw new ArgumentOutOfRangeException("version");
            }

            using (var runner = _runnerFactory.Create())
            {
                var executedMigrations = runner.GetExecutedMigrations().ToList();

                if (!executedMigrations.Contains(version))
                {
                    throw new VersionNeverExecutedException();   
                }

                RemoveMigrations(runner, executedMigrations.Where(x => x > version));

                runner.Commit();
            }
        }

        private void RemoveMigrations(IRunner runner, IEnumerable<long> migrationsToRemove)
        {
            var downScripts = _scriptFinder.GetDownScripts().ToDictionary(x => x.Version);
            
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