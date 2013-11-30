using System;
using System.Collections.Generic;

namespace Migrator
{
    public interface IRunner : IDisposable
    {
        void Commit();
        void ExecuteUpMigration(UpMigration migration);
        void ExecuteDownMigration(DownMigration migration);
        IEnumerable<long> GetExecutedMigrations();
    }
}