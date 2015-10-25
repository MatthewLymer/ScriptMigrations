using System;
using System.Data.SqlClient;
using Migrator.Shared.Runners;

namespace SqlServerMigrator
{
    public class RunnerFactory : IRunnerFactory
    {
        private readonly string _connectionString;

        public RunnerFactory(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("connection string must not be empty", "connectionString");
            }

            _connectionString = connectionString;
        }

        public IRunner Create()
        {
            return new Runner(_connectionString);
        }

        public bool CanExecuteTestQuery()
        {
            try
            {
                using (var connection = new SqlConnection())
                {
                    connection.Open();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT 2 + 3";
                        return (int) command.ExecuteScalar() == 5;
                    }
                }
            }
            catch (SqlException)
            {
                return false;
            }
        }
    }
}