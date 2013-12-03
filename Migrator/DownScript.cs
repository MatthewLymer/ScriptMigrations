namespace Migrator
{
    public class DownScript
    {
        public DownScript(long version)
        {
            Version = version;
        }

        public long Version { get; private set; }
    }
}