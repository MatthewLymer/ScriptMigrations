namespace Migrator.Scripts
{
    public class DownScript
    {
        public DownScript(long version, string name, string content)
        {
            Version = version;
            Name = name;
            Content = content;
        }

        public long Version { get; private set; }
        public string Name { get; private set; }
        public string Content { get; private set; }
    }
}