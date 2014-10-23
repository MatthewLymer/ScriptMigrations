using System;
using System.IO;
using System.Reflection;

namespace MigratorConsole
{
    public sealed class ActivatorFacade
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
                    return new ActivatorResult<T>(ActivatorResultCode.TypeNotFound);
                }

                var instance = (T)Activator.CreateInstance(type, constructorArgs);

                return new ActivatorResult<T>(instance);
            }
            catch (FileNotFoundException)
            {
                return new ActivatorResult<T>(ActivatorResultCode.AssemblyNotFound);
            }
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