namespace Migrator
{
    public class DownMigration
    {
        public DownMigration(long version)
        {
            Version = version;
        }

        public long Version { get; private set; }
    }
}