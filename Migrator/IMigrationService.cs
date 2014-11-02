using System;

namespace Migrator
{
    public interface IMigrationService
    {
        void Up();
        void DownToZero();
        void DownToVersion(long version);

        event Action<object, UpScriptStartedEventArgs> OnUpScriptStarted;
        event Action<object, EventArgs> OnUpScriptCompleted;
    }
}