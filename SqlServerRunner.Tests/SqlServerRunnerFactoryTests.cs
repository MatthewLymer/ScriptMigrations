using System;
using NUnit.Framework;

namespace SqlServerRunner.Tests
{
    internal class SqlServerRunnerFactoryTests
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
                Assert.IsNotNull(new SqlServerRunnerFactory(connectionString));
            }
        }
    }
}
