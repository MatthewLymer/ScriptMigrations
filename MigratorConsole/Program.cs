using System.IO;
using Migrator;
using Migrator.Facades;
using Migrator.Runners;
using Migrator.Scripts;
using MigratorConsole.CommandLine;
using MigratorConsole.Wrappers;

namespace MigratorConsole
{
    public class Program
    {
        static void Main(string[] args)
        {
            var consoleWrapper = new ConsoleWrapper();

            var migrationServiceFactory = new MigrationServiceFactory();

            var activatorFacade = new ActivatorFacade();

            var migratorCommands = new MigratorCommands(consoleWrapper, migrationServiceFactory, activatorFacade);

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
                var fileSystemFacade = new FileSystemFacade();

                var scriptFinder = new FileSystemScriptFinder(fileSystemFacade, scriptsPath);

                return new MigrationService(scriptFinder, runnerFactory);
            }
        }

        private class FileSystemFacade : IFileSystemFacade
        {
            public string[] GetFiles(string path, string searchPattern, SearchOption searchOption)
            {
                return Directory.GetFiles(path, searchPattern, searchOption);
            }

            public string ReadAllText(string path)
            {
                return File.ReadAllText(path);
            }
        }
    }
}
