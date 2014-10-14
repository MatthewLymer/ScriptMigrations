using System;
using System.Linq;
using FluentValidation;
using FluentValidation.Results;
using MigratorConsole.CommandLine;
using Moq;
using NUnit.Framework;

namespace MigratorConsole.Tests.CommandLine
{
    class CommandLineBinderTests
    {
        [TestFixture]
        public class WhenCreatingACommandLineBuilderInstance
        {
            [Test]
            [ExpectedException(typeof(ArgumentNullException))]
            public void ShouldThrowExceptionIfParserIsNull()
            {
                Assert.IsNotNull(new CommandLineBinder<int>(null, null));
            }

            [Test]
            [ExpectedException(typeof(ArgumentNullException))]
            public void ShouldThrowExceptionIfValidatorIsNull()
            {
                var mockParser = new Mock<ICommandLineParser<int>>();
                Assert.IsNotNull(new CommandLineBinder<int>(mockParser.Object, null));
            }
        }

        [TestFixture]
        public class WhenTellingCommandLineBinderToBindAnArray
        {
            private Mock<ICommandLineParser<int>> _mockParser;
            private Mock<AbstractValidator<int>> _mockValidator;
            private CommandLineBinder<int> _binder;

            private CommandLineBinderResult<int> BindModel(int model, ValidationResult validationResult)
            {
                var args = new string[0];

                _mockParser.Setup(x => x.Parse(args)).Returns(model);
                _mockValidator.Setup(x => x.Validate(model)).Returns(validationResult);

                return _binder.Bind(args);
            }

            [SetUp]
            public void BeforeEachTest()
            {
                _mockParser = new Mock<ICommandLineParser<int>>();
                _mockValidator = new Mock<AbstractValidator<int>>();
                _binder = new CommandLineBinder<int>(_mockParser.Object, _mockValidator.Object);
            }

            [Test]
            [ExpectedException(typeof(ArgumentNullException))]
            public void ShouldThrowIfArgsIsNull()
            {
                _binder.Bind(null);
            }

            [Test]
            public void ShouldParseCommandLineArgumentsWithoutErrors()
            {
                const int expectedModel = 5;

                var result = BindModel(expectedModel, new ValidationResult());

                Assert.AreEqual(expectedModel, result.Model);
                Assert.IsTrue(result.IsValid);
                Assert.IsEmpty(result.Errors);
            }

            [Test]
            public void ShouldNotBeValidIfValidatorGivesErrors()
            {
                const int expectedModel = 6;

                var validationResult = new ValidationResult(new[] {new ValidationFailure("prop", "error")});

                var result = BindModel(expectedModel, validationResult);

                Assert.IsFalse(result.IsValid);
            }

            [Test]
            public void ShouldGiveEnumerationOfErrorMessages()
            {
                const int expectedModel = 7;

                var errors = new[]
                {
                    "not good",
                    "very bad",
                    "the worst"
                };

                var validationResult = new ValidationResult(errors.Select(e => new ValidationFailure("prop", e)).ToArray());

                var result = BindModel(expectedModel, validationResult);

                foreach (var error in errors)
                {
                    CollectionAssert.Contains(result.Errors, error);
                }
            }
        }
    }
}