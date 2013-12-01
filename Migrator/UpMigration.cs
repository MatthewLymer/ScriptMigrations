namespace Migrator
{
    public class UpMigration
    {
        public UpMigration(long version)
        {
            Version = version;
        }

        public long Version { get; private set; }
    }
}