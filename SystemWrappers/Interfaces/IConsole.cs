namespace SystemWrappers.Interfaces
{
    public interface IConsole
    {
        void WriteLine(string format, params object[] arg);
        void WriteErrorLine(string format, params object[] arg);
        void Write(string format, params object[] arg);
        void WriteError(string format, params object[] arg);
    }
}