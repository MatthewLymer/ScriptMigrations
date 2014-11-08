using System;

namespace Migrator
{
    public interface IMigrationService
    {
        void Up();
        void Down(long version);

        event EventHandler<ScriptStartedEventArgs> OnScriptStarted;
        event EventHandler<EventArgs> OnScriptCompleted;
    }
}