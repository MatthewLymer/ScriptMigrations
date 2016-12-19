using System;
using NUnit.Framework;

namespace SqlServerMigrator.Tests
{
    internal class RunnerFactoryTests
    {
        [TestFixture]
        public class WhenCreatingAnSqlServerRunnerFactoryInstance
        {
            [Test]
            [TestCase(null)]
            [TestCase("")]
            [TestCase("    ")]
            public void ShouldThrowExceptionIfNoConnectionStringValueIsSet(string connectionString)
            {
                // ReSharper disable once ObjectCreationAsStatement
                Assert.Throws<ArgumentException>(() => new RunnerFactory(connectionString));
            }
        }
    }
}
