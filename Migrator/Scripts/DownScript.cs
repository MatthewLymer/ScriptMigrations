using System;

namespace Migrator.Scripts
{
    public sealed class DownScript
    {
        private readonly Func<string> _getContent;

        public DownScript(long version, string name, Func<string> getContent)
        {
            Version = version;
            Name = name;
            _getContent = getContent;
        }

        public long Version { get; private set; }
        public string Name { get; private set; }
        public string Content { get { return _getContent(); } }
    }
}