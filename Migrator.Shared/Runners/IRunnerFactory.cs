namespace Migrator.Shared.Runners
{
    public interface IRunnerFactory
    {
        IRunner Create();
    }
}