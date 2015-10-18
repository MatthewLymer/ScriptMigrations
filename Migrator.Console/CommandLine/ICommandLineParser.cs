using System.Collections.Generic;

namespace Migrator.Console.CommandLine
{
    public interface ICommandLineParser<out TModel> where TModel : new()
    {
        TModel Parse(ICollection<string> args);
    }
}