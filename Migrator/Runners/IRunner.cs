using System;
using System.Collections.Generic;
using Migrator.Migrations;

namespace Migrator.Runners
{
    public interface IRunner : IDisposable
    {
        void Commit();
        void ExecuteUpMigration(UpMigration migration);
        void ExecuteDownMigration(DownMigration migration);
        IEnumerable<long> GetExecutedMigrations();
    }
}