using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Migrator.Scripts;
using NUnit.Framework;

namespace Migrator.Tests
{
    class HistoryPersistingRunnerTemplateTests
    {
        internal class GivenAHistoryTableRunnerTemplateSpy
        {
            protected const string CreateHistoryTableMethodName = "CreateHistoryTable";
            protected const string DoesHistoryTableExistMethodName = "IsHistoryTableInSchema";
            protected const string ExecuteScriptMethodName = "ExecuteScript";

            public HistoryTableRunnerTemplateSpy RunnerTemplate { get; private set; }

            [SetUp]
            public void BeforeEachTest()
            {
                RunnerTemplate = new HistoryTableRunnerTemplateSpy();
            }

            protected static void AssertHistoryTableCreated(IEnumerable<Invocation> invocations)
            {
                CollectionAssert.Contains(invocations.Select(x => x.MethodOrPropertyName), CreateHistoryTableMethodName);
            }

            protected static void AssertHistoryTableNotCreated(IEnumerable<Invocation> invocations)
            {
                CollectionAssert.DoesNotContain(invocations.Select(x => x.MethodOrPropertyName), CreateHistoryTableMethodName);
            }

            protected static void AssertHistoryTableCreatedBeforeScriptExecution(List<Invocation> invocations)
            {
                var createHistoryTableIndex = invocations.FindIndex(i => i.MethodOrPropertyName == CreateHistoryTableMethodName);
                var executeScriptIndex = invocations.FindIndex(i => i.MethodOrPropertyName == ExecuteScriptMethodName);
                Assert.Less(createHistoryTableIndex, executeScriptIndex);
            }
        }

        [TestFixture]
        public class WhenExecutingUpScripts : GivenAHistoryTableRunnerTemplateSpy
        {
            private const string InsertHistoryRecordMethodName = "InsertHistoryRecord";

            private static void AssertScriptExecutedBeforeInsertingIntoHistory(List<Invocation> invocations)
            {
                var executeScriptIndex = invocations.FindIndex(i => i.MethodOrPropertyName == ExecuteScriptMethodName);
                var insertHistoryRecordIndex = invocations.FindIndex(i => i.MethodOrPropertyName == InsertHistoryRecordMethodName);

                Assert.Less(executeScriptIndex, insertHistoryRecordIndex);
            }

            [Test]
            public void ShouldCreateHistoryTableIfTableDoesNotExist()
            {
                RunnerTemplate.ExecuteUpScript(new UpScript(0, string.Empty, string.Empty));

                AssertHistoryTableCreated(RunnerTemplate.Invocations);
            }

            [Test]
            public void ShouldNotCreateHistoryTableIfTableDoesExist()
            {
                RunnerTemplate.DoesHistoryTableExistDefault = true;

                RunnerTemplate.ExecuteUpScript(new UpScript(0, string.Empty, string.Empty));

                AssertHistoryTableNotCreated(RunnerTemplate.Invocations);
            }

            [Test]
            [TestCase("select * from foo")]
            [TestCase("delete from bar")]
            public void ShouldExecuteScriptAfterHistoryTableHasBeenEnsured(string scriptContent)
            {
                RunnerTemplate.ExecuteUpScript(new UpScript(0, string.Empty, scriptContent));

                var invocations = RunnerTemplate.Invocations.ToList();

                AssertHistoryTableCreatedBeforeScriptExecution(invocations);
                
                var executeScriptInvocation = invocations.Single(i => i.MethodOrPropertyName == ExecuteScriptMethodName);

                Assert.AreEqual(scriptContent, executeScriptInvocation.Args[0]);
            }

            [Test]
            [TestCase(1, "my-script")]
            [TestCase(2, "your-script")]
            public void ShouldInsertHistoryRecordAfterScriptHasBeenExecuted(long version, string name)
            {
                RunnerTemplate.ExecuteUpScript(new UpScript(version, name, string.Empty));

                var invocations = RunnerTemplate.Invocations.ToList();

                AssertScriptExecutedBeforeInsertingIntoHistory(invocations);

                var args = invocations.Single(i => i.MethodOrPropertyName == InsertHistoryRecordMethodName).Args;

                Assert.AreEqual(version, args[0]);
                Assert.AreEqual(name, args[1]);
            }

            [Test]
            [TestCase(false)]
            [TestCase(true)]
            public void ShouldOnlyTryAndCreateHistoryTableOnce(bool doesHistoryTableExistDefault)
            {
                RunnerTemplate.DoesHistoryTableExistDefault = doesHistoryTableExistDefault;

                var upScript = new UpScript(0, string.Empty, string.Empty);

                RunnerTemplate.ExecuteUpScript(upScript);
                RunnerTemplate.ExecuteUpScript(upScript);

                Assert.AreEqual(1, RunnerTemplate.Invocations.Count(x => x.MethodOrPropertyName == DoesHistoryTableExistMethodName));
                Assert.AreEqual(doesHistoryTableExistDefault ? 0 : 1, RunnerTemplate.Invocations.Count(x => x.MethodOrPropertyName == CreateHistoryTableMethodName));
            }
        }

        [TestFixture]
        public class WhenExecutingDownScripts : GivenAHistoryTableRunnerTemplateSpy
        {
            private const string DeleteHistoryRecordMethodName = "DeleteHistoryRecord";

            private static void AssertScriptExecutedBeforeDeletingFromHistory(List<Invocation> invocations)
            {
                var executeScriptIndex = invocations.FindIndex(i => i.MethodOrPropertyName == ExecuteScriptMethodName);
                var insertHistoryRecordIndex = invocations.FindIndex(i => i.MethodOrPropertyName == DeleteHistoryRecordMethodName);

                Assert.Less(executeScriptIndex, insertHistoryRecordIndex);
            }

            [Test]
            public void ShouldCreateHistoryTableIfTableDoesNotExist()
            {
                RunnerTemplate.ExecuteDownScript(new DownScript(0, string.Empty, string.Empty));

                AssertHistoryTableCreated(RunnerTemplate.Invocations);
            }

            [Test]
            public void ShouldNotCreateHistoryTableIfTableDoesExist()
            {
                RunnerTemplate.DoesHistoryTableExistDefault = true;

                RunnerTemplate.ExecuteDownScript(new DownScript(0, string.Empty, string.Empty));

                AssertHistoryTableNotCreated(RunnerTemplate.Invocations);
            }

            [Test]
            [TestCase("select * from foo")]
            [TestCase("delete from bar")]
            public void ShouldExecuteScriptAfterHistoryTableHasBeenEnsured(string scriptContent)
            {
                RunnerTemplate.ExecuteDownScript(new DownScript(0, string.Empty, scriptContent));

                var invocations = RunnerTemplate.Invocations.ToList();

                AssertHistoryTableCreatedBeforeScriptExecution(invocations);

                var executeScriptInvocation = invocations.Single(i => i.MethodOrPropertyName == ExecuteScriptMethodName);

                Assert.AreEqual(scriptContent, executeScriptInvocation.Args[0]);
            }

            [Test]
            [TestCase(1)]
            [TestCase(2)]
            public void ShouldInsertHistoryRecordAfterScriptHasBeenExecuted(long version)
            {
                RunnerTemplate.ExecuteDownScript(new DownScript(version, string.Empty, string.Empty));

                var invocations = RunnerTemplate.Invocations.ToList();

                AssertScriptExecutedBeforeDeletingFromHistory(invocations);

                var args = invocations.Single(i => i.MethodOrPropertyName == DeleteHistoryRecordMethodName).Args;

                Assert.AreEqual(version, args[0]);
            }

            [Test]
            [TestCase(false)]
            [TestCase(true)]
            public void ShouldOnlyTryAndCreateHistoryTableOnce(bool doesHistoryTableExistDefault)
            {
                RunnerTemplate.DoesHistoryTableExistDefault = doesHistoryTableExistDefault;

                var script = new DownScript(0, string.Empty, string.Empty);

                RunnerTemplate.ExecuteDownScript(script);
                RunnerTemplate.ExecuteDownScript(script);

                Assert.AreEqual(1, RunnerTemplate.Invocations.Count(x => x.MethodOrPropertyName == DoesHistoryTableExistMethodName));
                Assert.AreEqual(doesHistoryTableExistDefault ? 0 : 1, RunnerTemplate.Invocations.Count(x => x.MethodOrPropertyName == CreateHistoryTableMethodName));
            }
        }

        [TestFixture]
        public class WhenGettingAExecutedMigrationVersions : GivenAHistoryTableRunnerTemplateSpy
        {
            [Test]
            public void ShouldReturnEmptyEnumerationIfHistoryTableDoesNotExist()
            {
                var executedMigrations = RunnerTemplate.GetExecutedMigrations();

                Assert.IsEmpty(executedMigrations);
            }

            [Test]
            [TestCase(1,2,3)]
            [TestCase(4,5,6)]
            public void ShouldReturnVersionsFromHistoryIfHistoryTableExists(long a, long b, long c)
            {
                RunnerTemplate.DoesHistoryTableExistDefault = true;

                RunnerTemplate.History = new List<long>{a,b,c};

                var executedMigrations = RunnerTemplate.GetExecutedMigrations();

                Assert.AreEqual(RunnerTemplate.History, executedMigrations);
            }

            [Test]
            public void ShouldOnlyCheckIfHistoryTableExistsOnce()
            {
                RunnerTemplate.GetExecutedMigrations();
                RunnerTemplate.GetExecutedMigrations();

                var invocationCount = RunnerTemplate.Invocations.Count(i => i.MethodOrPropertyName == DoesHistoryTableExistMethodName);

                Assert.AreEqual(1, invocationCount);
            }

            [Test]
            public void ShouldFindHistoryIfUpMigrationExecuted()
            {
                RunnerTemplate.ExecuteUpScript(new UpScript(0, string.Empty, string.Empty));

                RunnerTemplate.History = new List<long> {1,2,3};

                var executedMigrations = RunnerTemplate.GetExecutedMigrations();

                Assert.IsNotEmpty(executedMigrations);
            }
        }

        internal class HistoryTableRunnerTemplateSpy : HistoryTableRunnerTemplate
        {
            private readonly List<Invocation> _invocations = new List<Invocation>();

            public IEnumerable<Invocation> Invocations
            {
                get { return _invocations; }
            }

            public IEnumerable<long> History { get; set; }

            public bool DoesHistoryTableExistDefault { get; set; }

            public override void Dispose()
            {
                _invocations.Add(new Invocation(MethodBase.GetCurrentMethod().Name));
            }

            public override void Commit()
            {
                _invocations.Add(new Invocation(MethodBase.GetCurrentMethod().Name));
            }

            protected override void ExecuteScript(string script)
            {
                _invocations.Add(new Invocation(MethodBase.GetCurrentMethod().Name, script));
            }

            protected override IEnumerable<long> GetHistory()
            {
                return History;
            }

            protected override bool IsHistoryTableInSchema()
            {
                _invocations.Add(new Invocation(MethodBase.GetCurrentMethod().Name));

                return DoesHistoryTableExistDefault;
            }

            protected override void CreateHistoryTable()
            {
                _invocations.Add(new Invocation(MethodBase.GetCurrentMethod().Name));
            }

            protected override void InsertHistoryRecord(long version, string name)
            {
                _invocations.Add(new Invocation(MethodBase.GetCurrentMethod().Name, version, name));
            }

            protected override void DeleteHistoryRecord(long version)
            {
                _invocations.Add(new Invocation(MethodBase.GetCurrentMethod().Name, version));
            }
        }

        internal class Invocation
        {
            public string MethodOrPropertyName { get; private set; }
            public object[] Args { get; private set; }

            public Invocation(string methodOrPropertyName, params object[] args)
            {
                MethodOrPropertyName = methodOrPropertyName;
                Args = args;
            }
        }
    }
}
