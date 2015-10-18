using System;

namespace Migrator.Console.CommandLine
{
    [AttributeUsage(AttributeTargets.Property)]
    public class CommandLineAliasAttribute : Attribute
    {
        public CommandLineAliasAttribute(string alias)
        {
            Alias = alias;
        }

        public string Alias { get; private set; }
    }
}