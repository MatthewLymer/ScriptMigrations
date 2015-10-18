using System.Collections.Generic;

namespace Migrator.Shared.Migrations
{
    public interface IMigrationFinder
    {
        IEnumerable<UpMigration> GetUpMigrations();
        IEnumerable<DownMigration> GetDownMigrations();
    }
}