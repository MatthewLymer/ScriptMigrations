using System;
using System.Collections.Generic;

namespace Migrator
{
    public interface IRunner : IDisposable
    {
        void Commit();
        void ExecuteUpScript(UpScript script);
        void ExecuteDownScript(DownScript script);
        IEnumerable<long> GetExecutedMigrations();
    }
}