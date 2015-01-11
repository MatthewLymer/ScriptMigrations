using System.Diagnostics;
using SystemWrappers.Wrappers;
using SystemWrappers.Wrappers.Diagnostics;
using SystemWrappers.Wrappers.IO;
using Migrator;
using Migrator.Migrations;
using Migrator.Runners;
using MigratorConsole.Assembly;
using MigratorConsole.CommandLine;

namespace MigratorConsole
{
    public class Program
    {
        static void Main(string[] args)
        {
            var consoleWrapper = new ConsoleWrapper();

            var migrationServiceFactory = new MigrationServiceFactory();

            var activatorFacade = new ActivatorFacade();

            var stopwatch = new StopwatchWrapper(new Stopwatch());

            var migratorCommands = new MigratorCommands(consoleWrapper, migrationServiceFactory, activatorFacade, stopwatch);

            var migratorCommandLineParser = new MigratorCommandLineParser<MigratorCommandLineParserModel>();

            var migratorCommandLineParserModelValidator = new MigratorCommandLineParserModelValidator();

            var commandLineBinder = new CommandLineBinder<MigratorCommandLineParserModel>(migratorCommandLineParser, migratorCommandLineParserModelValidator);

            var bootstrapper = new Bootstrapper(migratorCommands, commandLineBinder);

            bootstrapper.Start(args);
        }

        private class MigrationServiceFactory : IMigrationServiceFactory
        {
            public IMigrationService Create(string scriptsPath, IRunnerFactory runnerFactory)
            {
                var fileSystem = new FileSystemWrapper();

                var scriptFinder = new FileSystemMigrationFinder(fileSystem, scriptsPath);

                return new MigrationService(scriptFinder, runnerFactory);
            }
        }
    }
}
