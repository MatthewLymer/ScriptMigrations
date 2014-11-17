using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SqlServerMigrator
{
    internal class SqlBatchSplitter
    {
        private const char StringDelimiter = '\'';

        private static readonly Regex GoRegex = 
            new Regex(@"^\s*GO(\s+|$)", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

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

            var matches = GoRegex.Matches(script);

            var literals = GetStringLiteralSections(script);
            
            foreach (var match in GetMatchesOutsideStringLiterals(matches, literals))
            {
                yield return script.Substring(start, match.Index - start);

                start = match.Index + match.Length;
            }

            yield return script.Substring(start);
        }

        private static IEnumerable<Match> GetMatchesOutsideStringLiterals(IEnumerable matches, IEnumerable<StringLiteralSection> literals)
        {
            return matches.Cast<Match>().Where(match => !literals.Any(x => x.StartIndex <= match.Index && x.EndIndex >= match.Index));
        }

        private static IEnumerable<StringLiteralSection> GetStringLiteralSections(string script)
        {
            var startIndex = 0;

            while ((startIndex = script.IndexOf(StringDelimiter, startIndex)) != -1)
            {
                var endIndex = GetStringDelimiterEndIndex(script, startIndex);

                yield return new StringLiteralSection(startIndex, endIndex);

                startIndex = endIndex + 1;
            }
        }

        private static int GetStringDelimiterEndIndex(string script, int startIndex)
        {
            var endIndex = script.IndexOf(StringDelimiter, startIndex + 1);

            if (endIndex == -1)
            {
                return script.Length - 1;
            }

            return endIndex;
        }

        private class StringLiteralSection
        {
            public StringLiteralSection(int startIndex, int endIndex)
            {
                StartIndex = startIndex;
                EndIndex = endIndex;
            }

            public int StartIndex { get; private set; }
            public int EndIndex { get; private set; }
        }
    }
}
