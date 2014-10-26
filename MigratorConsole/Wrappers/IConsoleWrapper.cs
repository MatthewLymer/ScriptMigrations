namespace MigratorConsole.Wrappers
{
    public interface IConsoleWrapper
    {
        void WriteLine(string format, params object[] arg);
        void WriteErrorLine(string format, params object[] arg);
        void Write(string format, params object[] arg);
        void WriteError(string format, params object[] arg);
    }
}