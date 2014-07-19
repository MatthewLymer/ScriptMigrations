using System;
using System.Collections.Generic;
using Migrator.Scripts;

namespace Migrator.Runners
{
    public interface IRunner : IDisposable
    {
        void Commit();
        void ExecuteUpScript(UpScript script);
        void ExecuteDownScript(DownScript script);
        IEnumerable<long> GetExecutedMigrations();
    }
}