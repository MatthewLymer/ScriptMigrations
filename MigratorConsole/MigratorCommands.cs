using System;
using System.Collections.Generic;
using Migrator;
using Migrator.Runners;
using MigratorConsole.Properties;
using MigratorConsole.Wrappers;

namespace MigratorConsole
{
    public class MigratorCommands : IMigratorCommands
    {
        private readonly IConsoleWrapper _consoleWrapper;
        private readonly IMigrationServiceFactory _migrationServiceFactory;
        private readonly IActivatorFacade _activatorFacade = new ActivatorFacade();

        public MigratorCommands(IConsoleWrapper consoleWrapper, IMigrationServiceFactory migrationServiceFactory)
        {
            _consoleWrapper = consoleWrapper;
            _migrationServiceFactory = migrationServiceFactory;
        }

        public void ShowHelp()
        {
            _consoleWrapper.WriteLine(Resources.HelpUsage);
        }

        public void ShowErrors(IEnumerable<string> errors)
        {
            _consoleWrapper.WriteErrorLine(Resources.ErrorHeading);

            foreach (var error in errors)
            {
                _consoleWrapper.WriteErrorLine("> {0}", error);
            }

            Environment.ExitCode = 1;
        }

        public void MigrateUp(string runnerQualifiedName, string connectionString, string scriptsPath)
        {
            var result = _activatorFacade.CreateInstance<IRunnerFactory>(runnerQualifiedName, new object[] {connectionString});

            if (!result.HasInstance)
            {
                WriteResultCodeErrorLine(result.ResultCode, runnerQualifiedName);
                Environment.ExitCode = 1;
                return;
            }

            var service = _migrationServiceFactory.Create(scriptsPath, result.Instance);

            service.OnUpScriptStartedEvent += OnUpScriptStartedEvent;

            service.Up();
        }

        private void OnUpScriptStartedEvent(object o, UpScriptStartedEventArgs args)
        {
            var upScript = args.UpScript;

            _consoleWrapper.Write(Resources.StartingMigrationMessageFormat, upScript.Version, upScript.Name);
        }

        public void MigrateDown(string runnerQualifiedName, string connectionString, string scriptsPath, long version)
        {
            var result = _activatorFacade.CreateInstance<IRunnerFactory>(runnerQualifiedName, new object[] { connectionString });

            if (!result.HasInstance)
            {
                WriteResultCodeErrorLine(result.ResultCode, runnerQualifiedName);
                Environment.ExitCode = 1;
                return;
            }

            var service = _migrationServiceFactory.Create(scriptsPath, result.Instance);

            if (version > 0)
            {
                service.DownToVersion(version);
            }
            else
            {
                service.DownToZero();
            }
        }

        private void WriteResultCodeErrorLine(ActivatorResultCode resultCode, string runnerQualifiedName)
        {
            switch (resultCode)
            {
                case ActivatorResultCode.UnableToResolveType:
                    _consoleWrapper.WriteErrorLine(Resources.CouldNotCreateRunnerFactoryType, runnerQualifiedName);
                    break;

                case ActivatorResultCode.UnableToResolveAssembly:
                    _consoleWrapper.WriteErrorLine(Resources.CouldNotLoadRunnerAssemblyFormat, runnerQualifiedName);
                    break;
            }
        }
    }
}