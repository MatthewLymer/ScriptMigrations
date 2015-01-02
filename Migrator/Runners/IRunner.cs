using System;
using System.Collections.Generic;
using Migrator.Migrations;

namespace Migrator.Runners
{
    public interface IRunner : IDisposable
    {
        void Commit();
        void ExecuteUpScript(UpMigration migration);
        void ExecuteDownScript(DownMigration migration);
        IEnumerable<long> GetExecutedMigrations();
    }
}