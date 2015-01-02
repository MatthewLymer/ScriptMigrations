using System.Collections.Generic;

namespace Migrator.Migrations
{
    public interface IMigrationFinder
    {
        IEnumerable<UpMigration> GetUpScripts();
        IEnumerable<DownMigration> GetDownScripts();
    }
}