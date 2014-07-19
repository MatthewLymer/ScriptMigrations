namespace Migrator.Runners
{
    public interface IRunnerFactory
    {
        IRunner Create();
    }
}