using System;
using System.Collections.Generic;
using Migrator.Runners;
using MigratorConsole.Properties;
using MigratorConsole.Wrappers;

namespace MigratorConsole
{
    public class MigratorCommands : IMigratorCommands
    {
        private readonly IConsoleWrapper _consoleWrapper;
        private readonly IMigrationServiceFactory _migrationServiceFactory;
        private readonly ActivatorFacade _activatorFacade = new ActivatorFacade();

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

            service.Up();
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
                case ActivatorResultCode.TypeNotFound:
                    _consoleWrapper.WriteErrorLine(Resources.CouldNotCreateRunnerFactoryType, runnerQualifiedName);
                    break;

                case ActivatorResultCode.AssemblyNotFound:
                    _consoleWrapper.WriteErrorLine(Resources.CouldNotLoadRunnerAssemblyFormat, runnerQualifiedName);
                    break;
            }
        }
    }
}