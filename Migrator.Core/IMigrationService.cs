using System;

namespace Migrator.Core
{
    public interface IMigrationService
    {
        void Up();
        void Down(long version);

        event EventHandler<MigrationStartedEventArgs> OnMigrationStarted;
        event EventHandler<EventArgs> OnMigrationCompleted;
    }
}