namespace Migrator.Console.Assembly
{
    public interface IActivatorFacade
    {
        ActivatorResult<T> CreateInstance<T>(string qualifiedName, params object[] constructorArgs) where T : class;
    }
}