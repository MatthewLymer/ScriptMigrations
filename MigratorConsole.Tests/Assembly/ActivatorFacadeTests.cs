using System;
using MigratorConsole.Assembly;
using NUnit.Framework;

namespace MigratorConsole.Tests.Assembly
{
    class ActivatorFacadeTests
    {
        [TestFixture]
        public class WhenTellingActivatorFacadeToCreateAnInstance
        {
            private readonly ActivatorFacade _activator = new ActivatorFacade();

            private static string CreateQualifiedName(Type type)
            {
                var assemblyName = type.Assembly.FullName.Split(',')[0];
                return string.Format("{0}, {1}", assemblyName, type.FullName);
            }

            [Test]
            [TestCase(null)]
            [TestCase("")]
            [TestCase("    ")]
            [TestCase("a,")]
            [TestCase(",b")]
            public void ShouldThrowExceptionIfQualifiedNameIsNotValid(string qualifiedName)
            {
                var exception = Assert.Catch<ArgumentException>(() => _activator.CreateInstance<object>(qualifiedName));

                Assert.AreEqual("qualifiedName", exception.ParamName);
            }

            [Test]
            public void ShouldReturnFailureResultIfAssemblyCannotBeResolved()
            {
                const string assemblyName = "somebogusassemblyname";

                var result = _activator.CreateInstance<object>(string.Format("{0}, sometypename", assemblyName));

                Assert.IsFalse(result.HasInstance);

                Assert.AreEqual(ActivatorResultCode.UnableToResolveAssembly, result.ResultCode);
            }

            [Test]
            public void ShouldReturnFailureResultIfTypeCannotBeResolved()
            {
                const string qualifiedName = "MigratorConsole.Tests, notatype";

                var result = _activator.CreateInstance<object>(qualifiedName);

                Assert.IsFalse(result.HasInstance);

                Assert.AreEqual(ActivatorResultCode.UnableToResolveType, result.ResultCode);
            }

            [Test]
            [TestCase("hello", 5, true)]
            [TestCase("world", 10, false)]
            public void ShouldCreateTypeWithProperConstructorArgumentsPassed(string arg1, int arg2, bool arg3)
            {
                var qualifedName = CreateQualifiedName(typeof (ConstructorInspector));

                var result = _activator.CreateInstance<ConstructorInspector>(qualifedName, new object[]{arg1, arg2, arg3});

                Assert.IsTrue(result.HasInstance);
                Assert.AreEqual(ActivatorResultCode.Successful, result.ResultCode);

                var instance = result.Instance;

                Assert.AreEqual(arg1, instance.Arg1);
                Assert.AreEqual(arg2, instance.Arg2);
                Assert.AreEqual(arg3, instance.Arg3);
            }

            public class ConstructorInspector
            {
                public string Arg1 { get; private set; }
                public int Arg2 { get; private set; }
                public bool Arg3 { get; private set; }

                public ConstructorInspector(string arg1, int arg2, bool arg3)
                {
                    Arg1 = arg1;
                    Arg2 = arg2;
                    Arg3 = arg3;
                }
            }
        }
    }
}
