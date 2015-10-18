using SystemWrappers.Wrappers.IO;
using Migrator.Core;
using Migrator.Core.Migrations;
using Migrator.Shared.Runners;

namespace Migrator.Console
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