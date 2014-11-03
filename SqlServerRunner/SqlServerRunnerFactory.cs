using System;
using Migrator.Runners;

namespace SqlServerRunner
{
    public class SqlServerRunnerFactory : IRunnerFactory
    {
        public SqlServerRunnerFactory(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("connection string must not be empty", "connectionString");
            }
        }

        public IRunner Create()
        {
            throw new NotImplementedException();
        }
    }
}