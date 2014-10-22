namespace MigratorConsole.Wrappers
{
    public interface IConsoleWrapper
    {
        void WriteLine(string format, params object[] arg);
        void WriteErrorLine(string format, params object[] arg);
    }
}