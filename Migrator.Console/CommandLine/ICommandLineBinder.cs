using System.Collections.Generic;

namespace Migrator.Console.CommandLine
{
    public interface ICommandLineBinder<TModel> where TModel : new()
    {
        CommandLineBinderResult<TModel> Bind(ICollection<string> args);
    }
}