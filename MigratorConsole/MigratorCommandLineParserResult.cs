namespace MigratorConsole
{
    public class MigratorCommandLineParserResult
    {
        public bool ShowHelp { get; set; }
        public bool MigrateUp { get; set; }
        public bool MigrateDown { get; set; }
        public long? Version { get; set; }
        public string RunnerQualifiedName { get; set; }
        public string ScriptsPath { get; set; }
    }
}