using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MigratorConsole
{
    public class MigratorCommandLineParser
    {
        private static readonly string[] HelpFlags = {
            "/help",
            "/h",
            "/?"
        };

        private static readonly Regex VersionRegex = 
            new Regex(@"^/version=([0-9]+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex RunnerQualifiedNameRegex = 
            new Regex("^/runner=(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex ScriptsPathRegex =
            new Regex("^/scripts=(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public MigratorCommandLineParserResult Parse(ICollection<string> args)
        {
            return new MigratorCommandLineParserResult
            {
                ShowHelp = ShouldShowHelp(args),
                MigrateUp = ShouldMigrateUp(args),
                MigrateDown = ShouldMigrateDown(args),
                Version = ReadVersion(args),
                RunnerQualifiedName = ReadRunnerQualifiedName(args),
                ScriptsPath = ReadScriptsPath(args)
            };
        }

        private static bool ShouldShowHelp(IEnumerable<string> args)
        {
            return HelpFlags.Any(hf => args.Any(a => hf.Equals(a, StringComparison.OrdinalIgnoreCase)));
        }

        private static bool ShouldMigrateUp(IEnumerable<string> args)
        {
            return args.Any(arg => "/command=up".Equals(arg, StringComparison.OrdinalIgnoreCase));
        }

        private static bool ShouldMigrateDown(IEnumerable<string> args)
        {
            return args.Any(arg => "/command=down".Equals(arg, StringComparison.OrdinalIgnoreCase));
        }

        private static long? ReadVersion(IEnumerable<string> args)
        {
            var match = args.Select(arg => VersionRegex.Match(arg)).FirstOrDefault(m => m.Success);

            if (match == null)
            {
                return null;
            }

            var captureValue = match.Groups[1].Value;

            long version;
            if (long.TryParse(captureValue, out version))
            {
                return version;
            }

            return null;
        }

        private static string ReadRunnerQualifiedName(IEnumerable<string> args)
        {
            var match = args.Select(arg => RunnerQualifiedNameRegex.Match(arg)).FirstOrDefault(m => m.Success);

            return match == null ? null : match.Groups[1].Value;
        }

        private static string ReadScriptsPath(IEnumerable<string> args)
        {
            var match = args.Select(arg => ScriptsPathRegex.Match(arg)).FirstOrDefault(m => m.Success);

            return match == null ? null : match.Groups[1].Value;
        }
    }
}