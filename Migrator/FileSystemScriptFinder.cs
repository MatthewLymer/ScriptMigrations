using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace Migrator
{
    public class FileSystemScriptFinder : IScriptFinder
    {
        public const string SqlFileSearchPattern = "*.sql";

        private readonly IFileSystemFacade _fileSystemFacade;
        private readonly string _path;

        public FileSystemScriptFinder(IFileSystemFacade fileSystemFacade, string path)
        {
            _fileSystemFacade = fileSystemFacade;
            _path = path;
        }

        public IEnumerable<UpScript> GetUpScripts()
        {
            var files = _fileSystemFacade.GetFiles(_path, SqlFileSearchPattern, SearchOption.AllDirectories);

            var scripts = new List<UpScript>();
            
            foreach (var file in files)
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);

                if (fileNameWithoutExtension == null)
                {
                    continue;
                }

                if (!Regex.IsMatch(fileNameWithoutExtension, @"^\d{14}_(.+)_up$", RegexOptions.IgnoreCase))
                {
                    continue;
                }

                var fileSegments = fileNameWithoutExtension.Split('_');
                
                var script = new UpScript(long.Parse(fileSegments[0]), fileSegments[1], _fileSystemFacade.ReadAllText(file));

                scripts.Add(script);
            }

            return scripts;
        }

        public IEnumerable<DownScript> GetDownScripts()
        {
            var files = _fileSystemFacade.GetFiles(_path, SqlFileSearchPattern, SearchOption.AllDirectories);

            var scripts = new List<DownScript>();

            foreach (var file in files)
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);

                if (fileNameWithoutExtension == null)
                {
                    continue;
                }

                if (!Regex.IsMatch(fileNameWithoutExtension, @"^\d{14}_(.+)_down$", RegexOptions.IgnoreCase))
                {
                    continue;
                }

                var fileSegments = fileNameWithoutExtension.Split('_');

                var script = new DownScript(long.Parse(fileSegments[0]), fileSegments[1], _fileSystemFacade.ReadAllText(file));

                scripts.Add(script);
            }

            return scripts;
        }
    }
}