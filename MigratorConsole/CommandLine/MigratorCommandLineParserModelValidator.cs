using System.Text.RegularExpressions;
using FluentValidation;
using FluentValidation.Results;
using MigratorConsole.Properties;

namespace MigratorConsole.CommandLine
{
    public class MigratorCommandLineParserModelValidator : AbstractValidator<MigratorCommandLineParserModel>
    {
        public MigratorCommandLineParserModelValidator()
        {
            RuleFor(x => x.RunnerQualifiedName)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotEmpty()
                .When(IsMigrating)
                .WithLocalizedMessage(() => Resources.RunnerQualifiedNameIsRequired)
                .Must(MatchQualifiedName)
                .WithLocalizedMessage(() => Resources.RunnerQualifiedNameBadFormat);

            RuleFor(x => x.ConnectionString)
                .NotEmpty()
                .When(IsMigrating)
                .WithLocalizedMessage(() => Resources.ConnectionStringIsRequired);

            RuleFor(x => x.ScriptsPath)
                .NotEmpty()
                .When(IsMigrating)
                .WithLocalizedMessage(() => Resources.ScriptsPathIsRequired);

            RuleFor(x => x.Version)
                .NotNull()
                .When(IsMigratingDown)
                .WithLocalizedMessage(() => Resources.VersionIsRequired)
                .GreaterThanOrEqualTo(0L)
                .WithLocalizedMessage(() => Resources.VersionMustBeZeroOrMore);

            Custom(ValidateMigrateUpDownIsExclusive);

            Custom(ValidateShowHelpAndMigrateAreExclusive);
        }

        private static bool IsMigrating(MigratorCommandLineParserModel model)
        {
            return model.MigrateUp || model.MigrateDown;
        }

        private static bool MatchQualifiedName(string qualifiedName)
        {
            return string.IsNullOrWhiteSpace(qualifiedName) || Regex.IsMatch(qualifiedName, @"^\w+,\s*[\w.+]+$");
        }

        private static bool IsMigratingDown(MigratorCommandLineParserModel model)
        {
            return model.MigrateDown;
        }

        private static ValidationFailure ValidateMigrateUpDownIsExclusive(MigratorCommandLineParserModel model)
        {
            if (model.MigrateDown && model.MigrateUp)
            {
                return new ValidationFailure("", Resources.MigrateUpDownMutuallyExclusive);
            }

            return null;
        }

        private static ValidationFailure ValidateShowHelpAndMigrateAreExclusive(MigratorCommandLineParserModel model)
        {
            if (model.ShowHelp && IsMigrating(model))
            {
                return new ValidationFailure("", Resources.ShowHelpMigrateMutuallyExclusive);
            }

            return null;
        }
    }
}