using System;
using SystemWrappers.Interfaces;

namespace SystemWrappers.Wrappers
{
    public sealed class ConsoleWrapper : IConsole
    {
        public void WriteLine(string format, params object[] arg)
        {
            Console.WriteLine(format, arg);
        }

        public void WriteErrorLine(string format, params object[] arg)
        {
            Console.Error.WriteLine(format, arg);
        }
        
        public void Write(string format, params object[] arg)
        {
            Console.Write(format, arg);
        }

        public void WriteError(string format, params object[] arg)
        {
            Console.Error.Write(format, arg);
        }
    }
}