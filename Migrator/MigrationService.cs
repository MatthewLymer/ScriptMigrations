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
            var migrations = _scriptFinder.GetUpMigrations();

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

        public void Down(int version)
        {
            if (version > 0)
            {
                throw new MigrationVersionNeverExecutedException();
            }

            using (var runner = _runnerFactory.Create())
            {
                foreach (var downMigration in _scriptFinder.GetDownMigrations().OrderByDescending(m => m.Version))
                {
                    runner.ExecuteDownMigration(downMigration);
                }    

                runner.Commit();
            }
        }
    }
}