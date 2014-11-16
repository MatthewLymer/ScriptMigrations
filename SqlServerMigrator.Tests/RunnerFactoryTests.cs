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
            [ExpectedException(typeof(ArgumentException))]
            public void ShouldThrowExceptionIfNoConnectionStringValueIsSet(string connectionString)
            {
                Assert.IsNotNull(new RunnerFactory(connectionString));
            }
        }
    }
}
