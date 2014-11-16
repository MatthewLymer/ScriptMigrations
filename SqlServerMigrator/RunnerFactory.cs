using System;
using Migrator.Runners;

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
    }
}