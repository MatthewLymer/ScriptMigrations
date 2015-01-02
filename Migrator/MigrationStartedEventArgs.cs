using System;

namespace Migrator
{
    public class MigrationStartedEventArgs : EventArgs
    {
        public long Version { get; private set; }
        public string Name { get; private set; }

        public MigrationStartedEventArgs(long version, string name)
        {
            Version = version;
            Name = name;
        }
    }
}