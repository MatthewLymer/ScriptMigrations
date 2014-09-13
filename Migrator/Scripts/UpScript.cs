namespace Migrator.Scripts
{
    public sealed class UpScript
    {
        public UpScript(long version, string name, string content)
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