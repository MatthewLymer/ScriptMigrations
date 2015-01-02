using System.IO;

namespace SystemWrappers.Interfaces.IO
{
    public interface IFileSystem
    {
        string[] GetFiles(string path, string searchPattern, SearchOption searchOption);
        string ReadAllText(string path);
    }
}