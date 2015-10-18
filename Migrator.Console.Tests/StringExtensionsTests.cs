using System;
using NUnit.Framework;

namespace Migrator.Console.Tests
{
    class StringExtensionsTests
    {
        [TestFixture]
        public class WhenTellingStringExtensionsTestsToTruncateAString
        {
            [Test]
            public void ShouldThrowExceptionIfStringIsNull()
            {
                const string subject = null;

                var exception = Assert.Throws<ArgumentNullException>(() => subject.Truncate(0));

                Assert.AreEqual("subject", exception.ParamName);
            }

            [Test]
            public void ShouldThrowExceptionIfMaxLengthIsLessThanZero()
            {
                const string subject = "foo";

                var exception = Assert.Throws<ArgumentOutOfRangeException>(() => subject.Truncate(-1));

                Assert.AreEqual("maxLength", exception.ParamName);
            }

            [Test]
            [TestCase("", 0)]
            [TestCase("a", 1)]
            [TestCase("abc", 5)]
            public void ShouldReturnExactStringIfStringIsShorterOrEqualToMaximumLength(string subject, int maxLength)
            {
                var result = subject.Truncate(maxLength);

                Assert.AreEqual(subject, result);
            }

            [Test]
            [TestCase("Hello World", 5, "Hello")]
            [TestCase("Goodbye World", 0, "")]
            public void ShouldReturnTruncatedStringIfStringIsLongerThanMaximumLength(string subject, int maxLength, string expectedResult)
            {
                var actualResult = subject.Truncate(maxLength);

                Assert.AreEqual(expectedResult, actualResult);
            }
        }

        [TestFixture]
        public class WhenTellingStringExtensionsTestsToFormatAString
        {
            [Test]
            public void ShouldThrowExceptionIfFormatIsNull()
            {
                const string format = null;

                var exception = Assert.Throws<ArgumentNullException>(() => format.FormatWith());

                Assert.AreEqual("format", exception.ParamName);
            }

            [Test]
            [TestCase("Hello {0}", "World", 0, 25.0d)]
            [TestCase("{0}{1}{2}", "Zorp", 5, -25.5d)]
            public void ShouldFormatString(string format, string arg1, int arg2, double arg3)
            {
                var args = new object[] {arg1, arg2, arg3};

                var expectedResult = string.Format(format, args);

                var actualResult = format.FormatWith(args);

                Assert.AreEqual(expectedResult, actualResult);
            }
        }
    }
}
