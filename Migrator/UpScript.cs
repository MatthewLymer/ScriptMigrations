namespace Migrator
{
    public class UpScript
    {
        public UpScript(long version)
        {
            Version = version;
        }

        public long Version { get; private set; }
    }
}