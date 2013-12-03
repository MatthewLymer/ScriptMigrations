using System;
using System.Collections.Generic;

namespace Migrator
{
    public interface IRunner : IDisposable
    {
        void Commit();
        void ExecuteUpMigration(UpScript script);
        void ExecuteDownScript(DownScript script);
        IEnumerable<long> GetExecutedMigrations();
    }
}