using SystemWrappers.Interfaces;
using SystemWrappers.Interfaces.Diagnostics;
using SystemWrappers.Wrappers;
using SystemWrappers.Wrappers.Diagnostics;
using FluentValidation;
using MigratorConsole.Assembly;
using MigratorConsole.CommandLine;
using Ninject;
using Ninject.Syntax;

namespace MigratorConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var kernel = new StandardKernel())
            {
                RegisterServices(kernel);

                kernel.Get<CommandLineLauncher>().Start(args);
            }
        }

        private static void RegisterServices(IBindingRoot bindingRoot)
        {
            bindingRoot.Bind<IConsole>().To<ConsoleWrapper>();
            
            bindingRoot.Bind<IMigrationServiceFactory>().To<MigrationServiceFactory>();
            
            bindingRoot.Bind<IActivatorFacade>().To<ActivatorFacade>();
            
            bindingRoot.Bind<IStopwatch>().To<StopwatchWrapper>();
            
            bindingRoot.Bind<IMigratorCommands>().To<MigratorCommands>();
            
            bindingRoot.Bind<ICommandLineParser<MigratorCommandLineParserModel>>()
                .To<MigratorCommandLineParser<MigratorCommandLineParserModel>>();
            
            bindingRoot.Bind<ICommandLineBinder<MigratorCommandLineParserModel>>()
                .To<CommandLineBinder<MigratorCommandLineParserModel>>();

            bindingRoot.Bind<AbstractValidator<MigratorCommandLineParserModel>>()
                .To<MigratorCommandLineParserModelValidator>();

        }
    }
}
