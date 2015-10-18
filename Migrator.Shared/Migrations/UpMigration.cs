using System;

namespace Migrator.Shared.Migrations
{
    public sealed class UpMigration
    {
        private readonly Func<string> _getContent;

        public UpMigration(long version, string name, Func<string> getContent)
        {
            _getContent = getContent;
            Version = version;
            Name = name;
        }

        public long Version { get; private set; }
        public string Name { get; private set; }
        public string Content { get { return _getContent(); } }
    }
}