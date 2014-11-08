using System;

namespace Migrator
{
    public class ScriptStartedEventArgs : EventArgs
    {
        public long Version { get; private set; }
        public string ScriptName { get; private set; }

        public ScriptStartedEventArgs(long version, string scriptName)
        {
            Version = version;
            ScriptName = scriptName;
        }
    }
}