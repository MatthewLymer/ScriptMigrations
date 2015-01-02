using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Migrator.Facades;

namespace Migrator.Migrations
{
    public sealed class FileSystemMigrationFinder : IMigrationFinder
    {
        public const string SqlFileSearchPattern = "*.sql";

        private static readonly Regex UpScriptRegex = new Regex(@"^\d{14}_(.+)_up$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex DownScriptRegex = new Regex(@"^\d{14}_(.+)_down$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly IFileSystemFacade _fileSystemFacade;
        private readonly string _path;

        public FileSystemMigrationFinder(IFileSystemFacade fileSystemFacade, string path)
        {
            _fileSystemFacade = fileSystemFacade;
            _path = path;
        }

        public IEnumerable<UpMigration> GetUpScripts()
        {
            return GetScripts(UpScriptRegex, CreateUpScript);
        }

        public IEnumerable<DownMigration> GetDownScripts()
        {
            return GetScripts(DownScriptRegex, CreateDownScript);
        }

        private IEnumerable<TMigration> GetScripts<TMigration>(Regex regex, Func<IList<string>, string, TMigration> createScript)
        {
            var files = _fileSystemFacade.GetFiles(_path, SqlFileSearchPattern, SearchOption.AllDirectories);

            var scripts = new List<TMigration>();

            foreach (var file in files)
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);

                Debug.Assert(fileNameWithoutExtension != null);

                if (!regex.IsMatch(fileNameWithoutExtension))
                {
                    continue;
                }

                var fileSegments = fileNameWithoutExtension.Split('_');

                scripts.Add(createScript(fileSegments, file));
            }

            return scripts;
        }

        private UpMigration CreateUpScript(IList<string> fileSegments, string file)
        {
            return new UpMigration(long.Parse(fileSegments[0]), fileSegments[1], () => _fileSystemFacade.ReadAllText(file));
        }

        private DownMigration CreateDownScript(IList<string> fileSegments, string file)
        {
            return new DownMigration(long.Parse(fileSegments[0]), fileSegments[1], () => _fileSystemFacade.ReadAllText(file));
        }
    }
}