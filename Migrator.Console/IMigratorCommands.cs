using System.Collections.Generic;

namespace Migrator.Console
{
    public interface IMigratorCommands
    {
        void ShowHelp();
        void MigrateUp(string runnerQualifiedName, string connectionString, string scriptsPath);
        void ShowErrors(IEnumerable<string> errors);
        void MigrateDown(string runnerQualifiedName, string connectionString, string scriptsPath, long version);
    }
}