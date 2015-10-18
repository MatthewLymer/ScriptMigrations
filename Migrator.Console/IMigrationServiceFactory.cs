using Migrator.Core;
using Migrator.Shared.Runners;

namespace Migrator.Console
{
    public interface IMigrationServiceFactory
    {
        IMigrationService Create(string scriptsPath, IRunnerFactory runnerFactory);
    }
}