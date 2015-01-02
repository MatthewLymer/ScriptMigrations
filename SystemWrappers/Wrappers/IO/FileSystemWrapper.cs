using System.IO;
using SystemWrappers.Interfaces.IO;

namespace SystemWrappers.Wrappers.IO
{
    public class FileSystemWrapper : IFileSystem
    {
        public string[] GetFiles(string path, string searchPattern, SearchOption searchOption)
        {
            return Directory.GetFiles(path, searchPattern, searchOption);
        }

        public string ReadAllText(string path)
        {
            return File.ReadAllText(path);
        }
    }
}