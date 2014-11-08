using System.Collections.Generic;
using System.Data.SqlClient;
using Migrator.Runners;
using Migrator.Scripts;

namespace SqlServerRunner
{
    internal class Runner : IRunner
    {
        private const string HistoryTableName = "_MigrationHistory";

        private const string CreateTableHistoryFormat = @"
CREATE TABLE [dbo].[{0}](
    [Version] BIGINT NOT NULL PRIMARY KEY,
    [ScriptName] NVARCHAR(255) NOT NULL
)";

        private readonly SqlConnection _connection;
        private readonly SqlTransaction _transaction;

        public Runner(string connectionString)
        {
            _connection = new SqlConnection(connectionString);
            _connection.Open();

            _transaction = _connection.BeginTransaction();

            if (!IsHistoryTableExistant())
            {
                CreateHistoryTable();
            }
        }

        public void Dispose()
        {
            _transaction.Dispose();
            _connection.Dispose();
        }

        public void Commit()
        {
            _transaction.Commit();
        }

        public void ExecuteUpScript(UpScript script)
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = script.Content;
                command.ExecuteNonQuery();
            }

            using (var command = _connection.CreateCommand())
            {
                const string insertFormat = "INSERT INTO [dbo].[{0}](Version, ScriptName) VALUES(@version, @scriptName)";
                command.CommandText = string.Format(insertFormat, HistoryTableName);

                command.Parameters.AddWithValue("@version", script.Version);
                command.Parameters.AddWithValue("@scriptName", script.Name);

                command.ExecuteNonQuery();
            }
        }

        public void ExecuteDownScript(DownScript script)
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = script.Content;
                command.ExecuteNonQuery();
            }

            using (var command = _connection.CreateCommand())
            {
                const string deleteFormat = "DELETE FROM [dbo].[{0}] WHERE Version=@version";
                command.CommandText = string.Format(deleteFormat, HistoryTableName);

                command.Parameters.AddWithValue("@version", script.Version);

                command.ExecuteNonQuery();
            }
        }

        public IEnumerable<long> GetExecutedMigrations()
        {
            var versions = new List<long>();

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = string.Format("SELECT [Version] FROM [dbo].[{0}]", HistoryTableName);
                
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        versions.Add((long) reader["Version"]);
                    }
                }
            }

            return versions;
        }

        private void CreateHistoryTable()
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = string.Format(CreateTableHistoryFormat, HistoryTableName);
                command.ExecuteNonQuery();
            }
        }

        private bool IsHistoryTableExistant()
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = "select count(*) from [sys].[tables] where name = @tableName";
                command.Parameters.AddWithValue("@tableName", HistoryTableName);

                return (long)command.ExecuteScalar() > 0;
            }
        }
    }
}
