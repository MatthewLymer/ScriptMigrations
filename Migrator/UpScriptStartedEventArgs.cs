using System;
using Migrator.Scripts;

namespace Migrator
{
    public class UpScriptStartedEventArgs : EventArgs
    {
        public UpScriptStartedEventArgs(UpScript upScript)
        {
            UpScript = upScript;
        }

        public UpScript UpScript { get; private set; }
    }
}