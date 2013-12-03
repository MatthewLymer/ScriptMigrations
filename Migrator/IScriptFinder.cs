using System.Collections.Generic;

namespace Migrator
{
    public interface IScriptFinder
    {
        IEnumerable<UpScript> GetUpScripts();
        IEnumerable<DownScript> GetDownScripts();
    }
}