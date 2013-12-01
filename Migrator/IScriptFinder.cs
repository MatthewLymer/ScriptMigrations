using System.Collections.Generic;

namespace Migrator
{
    public interface IScriptFinder
    {
        IEnumerable<UpMigration> GetUpMigrations();
        IEnumerable<DownMigration> GetDownMigrations();
    }
}