using System;
using System.Collections.Generic;
using System.IO;

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

            var upScripts = new List<UpScript>();

            foreach (var file in files)
            {
                var fileSegments = Path.GetFileNameWithoutExtension(file).Split('_');

                var upScript = new UpScript(long.Parse(fileSegments[0]), fileSegments[1], _fileSystemFacade.ReadAllText(file));

                upScripts.Add(upScript);
            }

            return upScripts;
        }

        public IEnumerable<DownScript> GetDownScripts()
        {
            throw new NotImplementedException();
        }
    }
}