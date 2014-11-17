using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace SqlServerMigrator.Tests
{
    class SqlBatchSplitterTests
    {
        [TestFixture]
        public class WhenTellingSqlBatchSplitterToSplitAScriptIntoBatches
        {
            private readonly SqlBatchSplitter _splitter = new SqlBatchSplitter();

            [Test]
            public void ShouldThrowExceptionIfArgumentIsNull()
            {
                var e = Assert.Throws<ArgumentNullException>(() => _splitter.Split(null));

                Assert.AreEqual("script", e.ParamName);
            }

            [Test]
            [TestCase("")]
            [TestCase("select * from foo")]
            [TestCase("select * from bar\nselect * from baz")]
            public void ShouldReturnGivenStringIfSeparationNotNeeded(string script)
            {
                var batches = _splitter.Split(script).ToList();

                Assert.AreEqual(1, batches.Count);
                Assert.AreEqual(script, batches[0]);
            }

            [Test]
            [TestCase("\ngo   ")]
            [TestCase("\r\n   GO\r")]
            public void ShouldSplitIntoTwoBatches(string glue)
            {
                const string firstStatement = "select * from foo";
                const string secondStatement = "select * from bar";
                var script = firstStatement + glue + secondStatement;

                var batches = _splitter.Split(script).ToList();
                
                Assert.AreEqual(2, batches.Count);
                Assert.AreEqual(firstStatement, batches[0].TrimEnd());
                Assert.AreEqual(secondStatement, batches[1].TrimEnd());
            }

            [Test]
            [Ignore]
            [TestCase("set @var = '\nGO\n'")]
            [TestCase("set @var = '''\nGO\n'")]
            public void ShouldIgnoreGoStatementsInMultilineStrings(string script)
            {
                var batch = _splitter.Split(script).Single();

                Assert.AreEqual(script, batch);
            }
        }
    }

    internal class SqlBatchSplitter
    {
        private static readonly Regex GoOnNewLineRegex = new Regex(@"^\s*GO\s+", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

        public IEnumerable<string> Split(string script)
        {
            if (script == null)
            {
                throw new ArgumentNullException("script");
            }

            return SplitImpl(script);
        }

        private static IEnumerable<string> SplitImpl(string script)
        {
            var start = 0;

            var matches = GoOnNewLineRegex.Matches(script);

            foreach (Match match in matches)
            {
                yield return script.Substring(start, match.Index - start);

                start = match.Index + match.Length;
            }

            yield return script.Substring(start);
        }
    }
}
