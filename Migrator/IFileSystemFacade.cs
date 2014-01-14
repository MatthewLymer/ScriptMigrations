﻿using System.IO;

namespace Migrator
{
    public interface IFileSystemFacade
    {
        string[] GetFiles(string path, string searchPattern, SearchOption searchOption);
        string ReadAllText(string path);
    }
}