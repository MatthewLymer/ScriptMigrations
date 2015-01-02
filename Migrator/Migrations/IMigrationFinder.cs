using System.Collections.Generic;

namespace Migrator.Migrations
{
    public interface IMigrationFinder
    {
        IEnumerable<UpMigration> GetUpMigrations();
        IEnumerable<DownMigration> GetDownMigrations();
    }
}