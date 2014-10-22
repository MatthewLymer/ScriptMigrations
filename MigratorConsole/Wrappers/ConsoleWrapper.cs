using System;

namespace MigratorConsole.Wrappers
{
    class ConsoleWrapper : IConsoleWrapper
    {
        public void WriteLine(string format, params object[] arg)
        {
            Console.WriteLine(format, arg);
        }

        public void WriteErrorLine(string format, params object[] arg)
        {
            Console.Error.WriteLine(format, arg);
        }
    }
}