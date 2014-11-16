using System;
using System.Collections.Generic;
using System.Linq;
using Migrator.Runners;
using Migrator.Scripts;

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

        public void ExecuteUpScript(UpScript script)
        {
            CreateHistoryTableIfNonExistant();

            ExecuteScript(script.Content);

            InsertHistoryRecord(script.Version, script.Name);
        }

        public void ExecuteDownScript(DownScript script)
        {
            CreateHistoryTableIfNonExistant();

            ExecuteScript(script.Content);

            DeleteHistoryRecord(script.Version);
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

        protected abstract void ExecuteScript(string content);

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