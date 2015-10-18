using System;
using System.Diagnostics;
using Migrator.Console.CommandLine;

namespace Migrator.Console
{
    public class CommandLineLauncher
    {
        private readonly IMigratorCommands _migratorCommands;
        private readonly ICommandLineBinder<MigratorCommandLineParserModel> _commandLineBinder;

        public CommandLineLauncher(IMigratorCommands migratorCommands, ICommandLineBinder<MigratorCommandLineParserModel> commandLineBinder)
        {
            if (migratorCommands == null)
            {
                throw new ArgumentNullException("migratorCommands");
            }

            if (commandLineBinder == null)
            {
                throw new ArgumentNullException("commandLineBinder");
            }

            _migratorCommands = migratorCommands;
            _commandLineBinder = commandLineBinder;
        }

        public void Start(string[] args)
        {
            var bindingResult = _commandLineBinder.Bind(args);

            if (!bindingResult.IsValid)
            {
                _migratorCommands.ShowErrors(bindingResult.Errors);
                return;
            }

            ExecuteSuccessfulCommand(bindingResult.Model);
        }

        private void ExecuteSuccessfulCommand(MigratorCommandLineParserModel model)
        {
            if (model.MigrateUp)
            {
                _migratorCommands.MigrateUp(
                    model.RunnerQualifiedName,
                    model.ConnectionString,
                    model.ScriptsPath);

                return;
            }

            if (model.MigrateDown)
            {
                Debug.Assert(model.Version != null);

                _migratorCommands.MigrateDown(
                    model.RunnerQualifiedName,
                    model.ConnectionString,
                    model.ScriptsPath,
                    model.Version.Value);

                return;
            }

            _migratorCommands.ShowHelp();
        }
    }
}