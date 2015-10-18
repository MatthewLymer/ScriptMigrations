using System.Collections.Generic;
using System.Linq;

namespace Migrator.Console.CommandLine
{
    public class CommandLineBinderResult<TModel>
    {
        private readonly TModel _model;
        private readonly IEnumerable<string> _errors;

        public CommandLineBinderResult(TModel model, IEnumerable<string> errors)
        {
            _model = model;
            _errors = errors;
        }

        public TModel Model
        {
            get { return _model; }
        }

        public bool IsValid
        {
            get { return !_errors.Any(); }
        }

        public IEnumerable<string> Errors
        {
            get { return _errors; }
        }
    }
}