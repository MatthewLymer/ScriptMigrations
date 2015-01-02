using System;
using System.Collections.Generic;
using SystemWrappers.Interfaces;
using Migrator;
using Migrator.Runners;
using MigratorConsole.Properties;

namespace MigratorConsole
{
    public class MigratorCommands : IMigratorCommands
    {
        private readonly IConsole _console;
        private readonly IMigrationServiceFactory _migrationServiceFactory;
        private readonly IActivatorFacade _activatorFacade;

        public MigratorCommands(IConsole console, IMigrationServiceFactory migrationServiceFactory, IActivatorFacade activatorFacade)
        {
            _console = console;
            _migrationServiceFactory = migrationServiceFactory;
            _activatorFacade = activatorFacade;
        }

        public void ShowHelp()
        {
            _console.WriteLine(Resources.HelpUsage);
        }

        public void ShowErrors(IEnumerable<string> errors)
        {
            _console.WriteErrorLine(Resources.ErrorHeading);

            foreach (var error in errors)
            {
                _console.WriteErrorLine("> {0}", error);
            }

            Environment.ExitCode = 1;
        }

        public void MigrateUp(string runnerQualifiedName, string connectionString, string scriptsPath)
        {
            InvokeServiceIfConstructable(
                runnerQualifiedName,
                connectionString,
                scriptsPath,
                service => service.Up());
        }

        public void MigrateDown(string runnerQualifiedName, string connectionString, string scriptsPath, long version)
        {
            InvokeServiceIfConstructable(
                runnerQualifiedName, 
                connectionString, 
                scriptsPath,
                service => service.Down(version));
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

            service.OnMigrationStarted += OnMigrationStarted;
            service.OnMigrationCompleted += OnMigrationCompleted;

            action(service);
        }

        private void OnMigrationStarted(object sender, MigrationStartedEventArgs args)
        {
            _console.Write(Resources.StartingMigrationMessageFormat, args.Version, args.Name);
        }

        private void OnMigrationCompleted(object sender, EventArgs args)
        {
            _console.WriteLine(Resources.CompletedMigrationMessage);
        }

        private void WriteResultCodeErrorLine(ActivatorResultCode resultCode, string runnerQualifiedName)
        {
            switch (resultCode)
            {
                case ActivatorResultCode.UnableToResolveType:
                    _console.WriteErrorLine(Resources.CouldNotCreateRunnerFactoryType, runnerQualifiedName);
                    break;

                case ActivatorResultCode.UnableToResolveAssembly:
                    _console.WriteErrorLine(Resources.CouldNotLoadRunnerAssemblyFormat, runnerQualifiedName);
                    break;
            }
        }
    }
}