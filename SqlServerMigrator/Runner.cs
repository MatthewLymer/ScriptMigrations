using System.Collections.Generic;
using System.Data.SqlClient;
using Migrator;

namespace SqlServerMigrator
{
    internal sealed class Runner : HistoryTableRunnerTemplate
    {
        private const string HistoryTableName = "_MigrationHistory";
        private const string InsertHistoryRecordFormat = "INSERT INTO [dbo].[{0}](Version, ScriptName) VALUES(@version, @scriptName)";
        private const string DeleteHistoryRecordFormat = "DELETE FROM [dbo].[{0}] WHERE Version=@version";
        private const string GetHistoryFormat = "SELECT Version FROM [dbo].[{0}]";
        private const string CreateTableHistoryFormat = @"CREATE TABLE [dbo].[{0}]([Version] BIGINT NOT NULL PRIMARY KEY, [ScriptName] NVARCHAR(255) NOT NULL)";
        private const string IsHistoryTableInSchemaFormat = "SELECT COUNT(*) FROM [sys].[tables] WHERE Name = @tableName";

        private readonly SqlConnection _connection;
        private readonly SqlTransaction _transaction;

        public Runner(string connectionString)
        {
            _connection = new SqlConnection(connectionString);
            _connection.Open();

            _transaction = _connection.BeginTransaction();
        }

        public override void Dispose()
        {
            _transaction.Dispose();
            _connection.Dispose();
        }

        public override void Commit()
        {
            _transaction.Commit();
        }

        protected override void ExecuteScript(string content)
        {
            using (var command = CreateCommandInTransaction())
            {
                command.CommandText = content;

                command.ExecuteNonQuery();
            }
        }

        protected override void InsertHistoryRecord(long version, string name)
        {
            using (var command = CreateCommandInTransaction())
            {
                command.CommandText = string.Format(InsertHistoryRecordFormat, HistoryTableName);

                command.Parameters.AddWithValue("@version", version);
                command.Parameters.AddWithValue("@scriptName", name);

                command.ExecuteNonQuery();
            }
        }

        protected override void DeleteHistoryRecord(long version)
        {
            using (var command = CreateCommandInTransaction())
            {
                command.CommandText = string.Format(DeleteHistoryRecordFormat, HistoryTableName);

                command.Parameters.AddWithValue("@version", version);

                command.ExecuteNonQuery();
            }
        }

        protected override IEnumerable<long> GetHistory()
        {
            using (var command = CreateCommandInTransaction())
            {
                command.CommandText = string.Format(GetHistoryFormat, HistoryTableName);

                using (var reader = command.ExecuteReader())
                {
                    var history = new List<long>();

                    while (reader.Read())
                    {
                        history.Add((long)reader["Version"]);
                    }

                    return history;
                }
            }
        }

        protected override bool IsHistoryTableInSchema()
        {
            using (var command = CreateCommandInTransaction())
            {
                command.CommandText = IsHistoryTableInSchemaFormat;
                command.Parameters.AddWithValue("@tableName", HistoryTableName);

                return (int)command.ExecuteScalar() > 0;
            }            
        }

        protected override void CreateHistoryTable()
        {
            using (var command = CreateCommandInTransaction())
            {
                command.CommandText = string.Format(CreateTableHistoryFormat, HistoryTableName);

                command.ExecuteNonQuery();
            }
        }

        private SqlCommand CreateCommandInTransaction()
        {
            var command = _connection.CreateCommand();

            command.Transaction = _transaction;

            return command;
        }
    }
}
