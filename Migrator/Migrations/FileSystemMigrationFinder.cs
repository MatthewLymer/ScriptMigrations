using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using SystemWrappers.Interfaces.IO;
using Migrator.Shared.Migrations;

namespace Migrator.Migrations
{
    public sealed class FileSystemMigrationFinder : IMigrationFinder
    {
        public const string SqlFileSearchPattern = "*.sql";

        private static readonly Regex UpScriptRegex = new Regex(@"^\d{14}_(.+)_up$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex DownScriptRegex = new Regex(@"^\d{14}_(.+)_down$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly IFileSystem _fileSystem;
        private readonly string _path;

        public FileSystemMigrationFinder(IFileSystem fileSystem, string path)
        {
            _fileSystem = fileSystem;
            _path = path;
        }

        public IEnumerable<UpMigration> GetUpMigrations()
        {
            return GetMigrations(UpScriptRegex, CreateUpMigration);
        }

        public IEnumerable<DownMigration> GetDownMigrations()
        {
            return GetMigrations(DownScriptRegex, CreateDownMigration);
        }

        private IEnumerable<TMigration> GetMigrations<TMigration>(Regex regex, Func<IList<string>, string, TMigration> createScript)
        {
            var files = _fileSystem.GetFiles(_path, SqlFileSearchPattern, SearchOption.AllDirectories);

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

        private UpMigration CreateUpMigration(IList<string> fileSegments, string file)
        {
            return new UpMigration(long.Parse(fileSegments[0]), fileSegments[1], () => _fileSystem.ReadAllText(file));
        }

        private DownMigration CreateDownMigration(IList<string> fileSegments, string file)
        {
            return new DownMigration(long.Parse(fileSegments[0]), fileSegments[1], () => _fileSystem.ReadAllText(file));
        }
    }
}