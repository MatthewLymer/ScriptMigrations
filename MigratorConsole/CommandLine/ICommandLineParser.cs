using System.Collections.Generic;

namespace MigratorConsole.CommandLine
{
    public interface ICommandLineParser<out TModel> where TModel : new()
    {
        TModel Parse(ICollection<string> args);
    }
}