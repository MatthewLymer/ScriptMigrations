using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;

namespace MigratorConsole
{
    public class CommandLineBinder<TModel> : ICommandLineBinder<TModel> where TModel : new()
    {
        private readonly ICommandLineParser<TModel> _parser;
        private readonly AbstractValidator<TModel> _validator;

        public CommandLineBinder(ICommandLineParser<TModel> parser, AbstractValidator<TModel> validator)
        {
            if (parser == null)
            {
                throw new ArgumentNullException("parser");                
            }

            if (validator == null)
            {
                throw new ArgumentNullException("validator");                
            }

            _parser = parser;
            _validator = validator;
        }

        public CommandLineBinderResult<TModel> Bind(ICollection<string> args)
        {
            if (args == null)
            {
                throw new ArgumentNullException("args");
            }

            var model = _parser.Parse(args);

            var validationResult = _validator.Validate(model);

            return new CommandLineBinderResult<TModel>
            {
                Model = model,
                IsValid = validationResult.IsValid,
                Errors = validationResult.Errors.Select(e => e.ErrorMessage)
            };
        }
    }
}