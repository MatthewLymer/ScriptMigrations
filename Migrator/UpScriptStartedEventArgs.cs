using System;

namespace Migrator
{
    public class UpScriptStartedEventArgs : EventArgs
    {
        public long Version { get; private set; }
        public string ScriptName { get; private set; }

        public UpScriptStartedEventArgs(long version, string scriptName)
        {
            Version = version;
            ScriptName = scriptName;
        }
    }
}