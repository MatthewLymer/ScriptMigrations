using Migrator;
using Migrator.Runners;

namespace MigratorConsole
{
    public interface IMigrationServiceFactory
    {
        IMigrationService Create(string scriptsPath, IRunnerFactory runnerFactory);
    }
}