using System;
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
            var migrations = _scriptFinder.GetUpScripts();

            if (!migrations.Any())
            {
                return;
            }

            using (var runner = _runnerFactory.Create())
            {
                var executedMigrations = runner.GetExecutedMigrations();
                
                foreach (var migration in migrations.Where(x => !executedMigrations.Contains(x.Version)).OrderBy(x => x.Version))
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
                foreach (var downMigration in _scriptFinder.GetDownScripts().OrderByDescending(m => m.Version))
                {
                    runner.ExecuteDownScript(downMigration);
                }

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
                var executedMigrations = runner.GetExecutedMigrations();

                if (!executedMigrations.Contains(version))
                {
                    throw new VersionNeverExecutedException();   
                }

                var downMigrations = _scriptFinder.GetDownScripts().ToList();

                foreach (var executedMigration in executedMigrations.Where(m => m > version).OrderByDescending(m => m))
                {
                    var downMigration = downMigrations.SingleOrDefault(m => m.Version == executedMigration);

                    runner.ExecuteDownScript(downMigration);
                }

                runner.Commit();
            }
        }
    }
}