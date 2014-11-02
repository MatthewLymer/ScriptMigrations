using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Migrator.Facades;

namespace Migrator.Scripts
{
    public sealed class FileSystemScriptFinder : IScriptFinder
    {
        public const string SqlFileSearchPattern = "*.sql";

        private static readonly Regex UpScriptRegex = new Regex(@"^\d{14}_(.+)_up$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex DownScriptRegex = new Regex(@"^\d{14}_(.+)_down$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly IFileSystemFacade _fileSystemFacade;
        private readonly string _path;

        public FileSystemScriptFinder(IFileSystemFacade fileSystemFacade, string path)
        {
            _fileSystemFacade = fileSystemFacade;
            _path = path;
        }

        public IEnumerable<UpScript> GetUpScripts()
        {
            return GetScripts(UpScriptRegex, CreateUpScript);
        }

        public IEnumerable<DownScript> GetDownScripts()
        {
            return GetScripts(DownScriptRegex, CreateDownScript);
        }

        private IEnumerable<TScript> GetScripts<TScript>(Regex regex, Func<IList<string>, string, TScript> createScript)
        {
            var files = _fileSystemFacade.GetFiles(_path, SqlFileSearchPattern, SearchOption.AllDirectories);

            var scripts = new List<TScript>();

            foreach (var file in files)
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);

                if (fileNameWithoutExtension == null)
                {
                    continue;
                }

                if (!regex.IsMatch(fileNameWithoutExtension))
                {
                    continue;
                }

                var fileSegments = fileNameWithoutExtension.Split('_');

                scripts.Add(createScript(fileSegments, file));
            }

            return scripts;
        }

        private UpScript CreateUpScript(IList<string> fileSegments, string file)
        {
            return new UpScript(long.Parse(fileSegments[0]), fileSegments[1], _fileSystemFacade.ReadAllText(file));
        }

        private DownScript CreateDownScript(IList<string> fileSegments, string file)
        {
            return new DownScript(long.Parse(fileSegments[0]), fileSegments[1], _fileSystemFacade.ReadAllText(file));
        }
    }
}