using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Migrator.Runners;
using MigratorConsole.Properties;
using MigratorConsole.Wrappers;

namespace MigratorConsole
{
    public class MigratorCommands : IMigratorCommands
    {
        private readonly IConsoleWrapper _consoleWrapper;
        private readonly IMigrationServiceFactory _migrationServiceFactory;

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
            var runnerFactory = LoadRunnerFactory(runnerQualifiedName, connectionString);

            if (runnerFactory == null)
            {
                Environment.ExitCode = 1;
                return;
            }

            var service = _migrationServiceFactory.Create(scriptsPath, runnerFactory);

            service.Up();
        }

        public void MigrateDown(string runnerQualifiedName, string connectionString, string scriptsPath, long version)
        {
            throw new NotImplementedException();
        }

        private IRunnerFactory LoadRunnerFactory(string runnerQualifiedName, string connectionString)
        {
            var qualifiedName = ParseQualifiedName(runnerQualifiedName);

            try
            {
                var assembly = Assembly.Load(qualifiedName.AssemblyName);
                var type = assembly.GetType(qualifiedName.TypeName);

                if (type == null)
                {
                    _consoleWrapper.WriteErrorLine(Resources.CouldNotCreateRunnerFactoryType, qualifiedName.TypeName);
                    return null;
                }
                
                var constructorArgs = new object[] {connectionString};

                return (IRunnerFactory)Activator.CreateInstance(type, constructorArgs);
            }
            catch (FileNotFoundException e)
            {
                _consoleWrapper.WriteErrorLine(Resources.CouldNotLoadRunnerAssemblyFormat, e.FileName);
            }

            return null;
        }

        private static QualifiedName ParseQualifiedName(string qualifiedName)
        {
            var segments = qualifiedName.Split(',');

            return new QualifiedName(segments[0].Trim(), segments[1].Trim());
        }

        private class QualifiedName
        {
            public QualifiedName(string assemblyName, string typeName)
            {
                AssemblyName = assemblyName;
                TypeName = typeName;
            }

            public string AssemblyName { get; private set; }

            public string TypeName { get; private set; }
        }
    }
}