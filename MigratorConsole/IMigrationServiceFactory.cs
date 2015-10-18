using Migrator;
using Migrator.Shared.Runners;

namespace MigratorConsole
{
    public interface IMigrationServiceFactory
    {
        IMigrationService Create(string scriptsPath, IRunnerFactory runnerFactory);
    }
}