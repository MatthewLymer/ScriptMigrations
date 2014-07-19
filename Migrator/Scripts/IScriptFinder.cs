using System.Collections.Generic;

namespace Migrator.Scripts
{
    public interface IScriptFinder
    {
        IEnumerable<UpScript> GetUpScripts();
        IEnumerable<DownScript> GetDownScripts();
    }
}