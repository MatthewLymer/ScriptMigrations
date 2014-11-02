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
        private readonly IActivatorFacade _activatorFacade;

        public MigratorCommands(IConsoleWrapper consoleWrapper, IMigrationServiceFactory migrationServiceFactory, IActivatorFacade activatorFacade)
        {
            _consoleWrapper = consoleWrapper;
            _migrationServiceFactory = migrationServiceFactory;
            _activatorFacade = activatorFacade;
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
            InvokeServiceIfConstructable(
                runnerQualifiedName,
                connectionString,
                scriptsPath,
                ExecuteUp);
        }

        public void MigrateDown(string runnerQualifiedName, string connectionString, string scriptsPath, long version)
        {
            InvokeServiceIfConstructable(
                runnerQualifiedName, 
                connectionString, 
                scriptsPath,
                service => ExecuteDown(service, version));
        }

        private void InvokeServiceIfConstructable(string runnerQualifiedName, string connectionString, string scriptsPath, Action<IMigrationService> action)
        {
            var result = _activatorFacade.CreateInstance<IRunnerFactory>(runnerQualifiedName, connectionString);

            if (!result.HasInstance)
            {
                WriteResultCodeErrorLine(result.ResultCode, runnerQualifiedName);
                Environment.ExitCode = 1;
                return;
            }

            var service = _migrationServiceFactory.Create(scriptsPath, result.Instance);

            action(service);
        }

        private void ExecuteUp(IMigrationService service)
        {
            service.OnUpScriptStartedEvent += OnUpScriptStartedEvent;

            service.Up();
        }

        private static void ExecuteDown(IMigrationService service, long version)
        {
            if (version > 0)
            {
                service.DownToVersion(version);
            }
            else
            {
                service.DownToZero();
            }
        }

        private void OnUpScriptStartedEvent(object source, UpScriptStartedEventArgs args)
        {
            var upScript = args.UpScript;

            _consoleWrapper.Write(Resources.StartingMigrationMessageFormat, upScript.Version, upScript.Name);
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