using System;
using System.Collections.Generic;
using Migrator.Shared.Migrations;

namespace Migrator.Shared.Runners
{
    public interface IRunner : IDisposable
    {
        void Commit();
        void ExecuteUpMigration(UpMigration migration);
        void ExecuteDownMigration(DownMigration migration);
        IEnumerable<long> GetExecutedMigrations();
    }
}