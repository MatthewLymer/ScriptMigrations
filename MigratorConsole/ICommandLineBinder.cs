using System.Collections.Generic;

namespace MigratorConsole
{
    public interface ICommandLineBinder<TModel> where TModel : new()
    {
        CommandLineBinderResult<TModel> Bind(ICollection<string> args);
    }
}