using System;
using System.Collections.Generic;
using System.Linq;
using Migrator.Migrations;
using Migrator.Runners;

namespace Migrator
{
    public abstract class HistoryTableRunnerTemplate : IRunner
    {
        private readonly Lazy<bool> _lazyDoesHistoryTableExist;
        private bool _wasHistoryTableCreated;

        protected HistoryTableRunnerTemplate()
        {
            _lazyDoesHistoryTableExist = new Lazy<bool>(IsHistoryTableInSchema);
        }

        public void ExecuteUpMigration(UpMigration migration)
        {
            CreateHistoryTableIfNonExistant();

            ExecuteScript(migration.Content);

            InsertHistoryRecord(migration.Version, migration.Name);
        }

        public void ExecuteDownMigration(DownMigration migration)
        {
            CreateHistoryTableIfNonExistant();

            ExecuteScript(migration.Content);

            DeleteHistoryRecord(migration.Version);
        }

        public IEnumerable<long> GetExecutedMigrations()
        {
            if (_lazyDoesHistoryTableExist.Value || _wasHistoryTableCreated)
            {
                return GetHistory();
            }

            return Enumerable.Empty<long>();
        }

        public abstract void Dispose();

        public abstract void Commit();

        protected abstract void ExecuteScript(string script);

        protected abstract void InsertHistoryRecord(long version, string name);

        protected abstract void DeleteHistoryRecord(long version);

        protected abstract IEnumerable<long> GetHistory();

        protected abstract bool IsHistoryTableInSchema();

        protected abstract void CreateHistoryTable();

        private void CreateHistoryTableIfNonExistant()
        {
            if (_lazyDoesHistoryTableExist.Value || _wasHistoryTableCreated)
            {
                return;
            }

            CreateHistoryTable();

            _wasHistoryTableCreated = true;
        }
    }
}