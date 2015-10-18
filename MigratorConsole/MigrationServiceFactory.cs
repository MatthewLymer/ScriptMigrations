using SystemWrappers.Wrappers.IO;
using Migrator;
using Migrator.Migrations;
using Migrator.Shared.Runners;

namespace MigratorConsole
{
    class MigrationServiceFactory : IMigrationServiceFactory
    {
        public IMigrationService Create(string scriptsPath, IRunnerFactory runnerFactory)
        {
            var fileSystem = new FileSystemWrapper();

            var scriptFinder = new FileSystemMigrationFinder(fileSystem, scriptsPath);

            return new MigrationService(scriptFinder, runnerFactory);
        }
    }
}