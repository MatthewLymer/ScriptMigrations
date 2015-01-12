namespace MigratorConsole.CommandLine
{
    public class MigratorCommandLineParserModel
    {
        [CommandLineAlias("?")]
        public bool ShowHelp { get; set; }

        [CommandLineAlias("up")]
        public bool MigrateUp { get; set; }

        [CommandLineAlias("down")]
        public bool MigrateDown { get; set; }

        [CommandLineAlias("Version")]
        public long? Version { get; set; }

        [CommandLineAlias("Runner")]
        public string RunnerQualifiedName { get; set; }

        [CommandLineAlias("Scripts")]
        public string ScriptsPath { get; set; }

        [CommandLineAlias("ConnectionString")]
        public string ConnectionString { get; set; }
    }
}