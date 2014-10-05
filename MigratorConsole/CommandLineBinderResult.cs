using System.Collections.Generic;

namespace MigratorConsole
{
    public class CommandLineBinderResult<TModel>
    {
        public TModel Model { get; set; }
        public bool IsValid { get; set; }
        public IEnumerable<string> Errors { get; set; }
    }
}