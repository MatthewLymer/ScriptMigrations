using System;
using System.IO;
using System.Reflection;

namespace MigratorConsole
{
    public sealed class ActivatorFacade : IActivatorFacade
    {
        public ActivatorResult<T> CreateInstance<T>(string qualifiedName, params object[] constructorArgs) where T : class
        {
            var parsed = ParseQualifiedName(qualifiedName);

            try
            {
                var assembly = Assembly.Load(parsed.AssemblyName);
                var type = assembly.GetType(parsed.TypeName);

                if (type == null)
                {
                    return new ActivatorResult<T>(ActivatorResultCode.UnableToResolveType);
                }

                var instance = (T)Activator.CreateInstance(type, constructorArgs);

                return new ActivatorResult<T>(instance);
            }
            catch (FileNotFoundException)
            {
                return new ActivatorResult<T>(ActivatorResultCode.UnableToResolveAssembly);
            }
        }

        private static QualifiedName ParseQualifiedName(string qualifiedName)
        {
            if (string.IsNullOrEmpty(qualifiedName))
            {
                throw new ArgumentException(@"qualified name must not be empty", "qualifiedName");
            }

            var segments = qualifiedName.Split(',');

            if (segments.Length != 2)
            {
                throw new QualifiedNameBadFormatException("qualifiedName");
            }
            
            var assemblyName = segments[0].Trim();
            var typeName = segments[1].Trim();

            if (assemblyName == "" || typeName == "")
            {
                throw new QualifiedNameBadFormatException("qualifiedName");
            }

            return new QualifiedName(assemblyName, typeName);
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

    [Serializable]
    internal class QualifiedNameBadFormatException : ArgumentException
    {
        public QualifiedNameBadFormatException(string paramName)
            : base(@"qualified name must be in format 'AssemblyName, TypeName'", paramName)
        {

        }
    }
}