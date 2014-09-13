using System;
using System.Collections.Generic;
using Migrator.Runners;
using NUnit.Framework;

namespace SqlServerRunner.Tests
{
    internal class SqlServerRunnerFactoryTests
    {
        [TestFixture]
        public class WhenCreatingAnSqlServerRunnerFactoryInstance
        {
            [Test]
            [ExpectedException(typeof(ArgumentNullException))]
            public void ShouldThrowExceptionIfNoConfigurationArgumentsPassed()
            {
                Assert.IsNotNull(new SqlServerRunnerFactory(null));
            }

            [Test]
            [TestCase(null)]
            [TestCase("")]
            [TestCase("    ")]
            public void ShouldThrowExceptionIfNoConnectionStringValueIsSet(string connectionString)
            {
                throw new NotImplementedException();
            }
        }
    }

    internal class SqlServerRunnerFactory : IRunnerFactory
    {
        public SqlServerRunnerFactory(IEnumerable<KeyValuePair<string, string>> configuration)
        {
            throw new ArgumentNullException("configuration");
        }

        public IRunner Create()
        {
            throw new NotImplementedException();
        }
    }
}
