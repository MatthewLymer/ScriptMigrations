using System;
using System.Collections.Generic;
using System.IO;
using Migrator.Runners;
using MigratorConsole.Properties;
using MigratorConsole.Wrappers;

namespace MigratorConsole
{
    public class MigratorCommands : IMigratorCommands
    {
        private readonly IConsoleWrapper _consoleWrapper;

        public MigratorCommands(IConsoleWrapper consoleWrapper)
        {
            _consoleWrapper = consoleWrapper;
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
            try
            {
                var runnerFactory = LoadRunnerFactory(runnerQualifiedName, connectionString);
            }
            catch (MigrationAssemblyNotFoundException e)
            {
                _consoleWrapper.WriteErrorLine(Resources.CouldNotLoadRunnerAssemblyFormat,
                    ((FileNotFoundException) e.InnerException).FileName);
                Environment.ExitCode = 1;
            }
            catch (MigrationRunnerFactoryNotFoundException e)
            {
                _consoleWrapper.WriteErrorLine(Resources.CouldNotCreateRunnerFactoryType,
                    ((TypeLoadException)e.InnerException).TypeName);
                Environment.ExitCode = 1;                
            }
        }

        private static IRunnerFactory LoadRunnerFactory(string runnerQualifiedName, string connectionString)
        {
            var qualifiedName = ParseQualifiedName(runnerQualifiedName);

            var constructorArgs = new object[] { connectionString };

            try
            {
                return (IRunnerFactory) Activator.CreateInstance(
                    qualifiedName.AssemblyName,
                    qualifiedName.TypeName,
                    constructorArgs).Unwrap();
            }
            catch (FileNotFoundException e)
            {
                throw new MigrationAssemblyNotFoundException(e);
            }
            catch (TypeLoadException e)
            {
                throw new MigrationRunnerFactoryNotFoundException(e);
            }
        }

        public void MigrateDown(string runnerQualifiedName, string connectionString, string scriptsPath, long version)
        {
            throw new NotImplementedException();
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

    internal class MigrationRunnerFactoryNotFoundException : Exception
    {
        public MigrationRunnerFactoryNotFoundException(TypeLoadException typeLoadException)
            : base(typeLoadException.Message, typeLoadException)
        {
        }
    }

    internal class MigrationAssemblyNotFoundException : Exception
    {
        public MigrationAssemblyNotFoundException(FileNotFoundException innerException) 
            : base(innerException.Message, innerException)
        {
        }
    }
}