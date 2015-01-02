using System.Collections.Generic;
using System.IO;
using System.Linq;
using SystemWrappers.Interfaces.IO;
using Migrator.Migrations;
using Moq;
using NUnit.Framework;

namespace Migrator.Tests.Migrations
{
    internal class FileSystemMigrationFinderTests
    {
        private class GivenADirectory
        {
            private const string Path = ".";

            private Mock<IFileSystem> _mockFileSystem;
            private FileSystemMigrationFinder _migrationFinder;

            [SetUp]
            public void BeforeEachTest()
            {
                _mockFileSystem = new Mock<IFileSystem>();

                _migrationFinder = new FileSystemMigrationFinder(_mockFileSystem.Object, Path);
            }

            private class GivenTheDirectoryIsEmpty : GivenADirectory
            {
                [SetUp]
                public new void BeforeEachTest()
                {
                    _mockFileSystem.Setup(
                        x => x.GetFiles(Path, FileSystemMigrationFinder.SqlFileSearchPattern, SearchOption.AllDirectories))
                        .Returns(new string[0]);
                }

                [TestFixture]
                public class WhenTellingTheFileSystemMigrationFinderToFindUpMigrations : GivenTheDirectoryIsEmpty
                {
                    [Test]
                    public void ShouldReturnAnEmptyEnumeration()
                    {
                        // act
                        var migrations = _migrationFinder.GetUpMigrations();

                        // assert
                        Assert.IsEmpty(migrations);
                    }
                }

                [TestFixture]
                public class WhenTellingTheFileSystemMigrationFinderToFindDownMigrations : GivenTheDirectoryIsEmpty
                {
                    [Test]
                    public void ShouldReturnAnEmptyEnumeration()
                    {
                        // act
                        var migrations = _migrationFinder.GetDownMigrations();

                        // assert
                        Assert.IsEmpty(migrations);
                    }
                }
            }

            private class GivenADirectoryWithASingleUpScript : GivenADirectory
            {
                private const long Version = 20150105235959;
                private const string Name = "MyMigration";
                private const string Content = "-- This is a migration";

                [SetUp]
                public new void BeforeEachTest()
                {
                    string scriptPath = string.Format(@".\{0}_{1}_up.sql", Version, Name);

                    _mockFileSystem.Setup(
                        x => x.GetFiles(Path, FileSystemMigrationFinder.SqlFileSearchPattern, SearchOption.AllDirectories))
                        .Returns(new[] {scriptPath});

                    _mockFileSystem.Setup(x => x.ReadAllText(scriptPath)).Returns(Content);
                }

                [TestFixture]
                public class WhenTellingTheFileSystemMigrationFinderToFindUpMigrations : GivenADirectoryWithASingleUpScript
                {
                    [Test]
                    public void ShouldReturnASinglePopulatedUpScript()
                    {
                        // act
                        var scripts = _migrationFinder.GetUpMigrations();

                        // assert
                        var script = scripts.Single();

                        Assert.AreEqual(Version, script.Version);
                        Assert.AreEqual(Name, script.Name);
                        Assert.AreEqual(Content, script.Content);
                    }

                    [Test]
                    public void ShouldNotAccessFilesystemIfContentPropertyIsNotRead()
                    {
                        var migrations = _migrationFinder.GetUpMigrations();

                        Assert.IsNotEmpty(migrations);

                        _mockFileSystem.Verify(x => x.ReadAllText(It.IsAny<string>()), Times.Never);
                    }
                }
            }

            private class GivenADirectoryWithMultipleUpScripts : GivenADirectory
            {
                private readonly string[] _scriptPaths = {
                    @".\20121212000000_MyScript_up.sql",
                    @".\20121212000001_MyScript_up.sql",
                    @".\subdirectory\20121212000002_MyScript_up.sql"
                };
                
                [SetUp]
                public new void BeforeEachTest()
                {
                    _mockFileSystem.Setup(x => x.ReadAllText(It.IsAny<string>())).Returns("");
                    
                    _mockFileSystem.Setup(x => x.GetFiles(Path, FileSystemMigrationFinder.SqlFileSearchPattern, SearchOption.AllDirectories))
                                         .Returns(_scriptPaths);
                }

                [TestFixture]
                public class WhenTellingTheFileSystemMigrationFinderToFindUpMigrations : GivenADirectoryWithMultipleUpScripts
                {
                    [Test]
                    public void ShouldReturnMultipleUpMigrations()
                    {
                        // act
                        var migrations = _migrationFinder.GetUpMigrations();

                        // assert
                        Assert.AreEqual(3, migrations.Count());
                    }
                }
            }

            private class GivenADirectoryWithASingleDownScript : GivenADirectory
            {
                private const long Version = 20150105235959;
                private const string Name = "MyMigration";
                private const string Content = "-- This is a migration";

                [SetUp]
                public new void BeforeEachTest()
                {
                    string filePath = string.Format(@".\{0}_{1}_down.sql", Version, Name);

                    _mockFileSystem.Setup(
                        x => x.GetFiles(Path, FileSystemMigrationFinder.SqlFileSearchPattern, SearchOption.AllDirectories))
                        .Returns(new[] { filePath });

                    _mockFileSystem.Setup(x => x.ReadAllText(filePath)).Returns(Content);                    
                }

                [TestFixture]
                public class WhenTellingTheFileSystemMigrationFinderToFindDownMigrations : GivenADirectoryWithASingleDownScript
                {
                    [Test]
                    public void ShouldReturnASinglePopulatedDownMigration()
                    {
                        // act
                        var migrations = _migrationFinder.GetDownMigrations();

                        // assert
                        var migration = migrations.Single();

                        Assert.AreEqual(Version, migration.Version);
                        Assert.AreEqual(Name, migration.Name);
                        Assert.AreEqual(Content, migration.Content);                        
                    }

                    [Test]
                    public void ShouldNotAccessFilesystemIfContentPropertyIsNotRead()
                    {
                        var migrations = _migrationFinder.GetDownMigrations();

                        Assert.IsNotEmpty(migrations);

                        _mockFileSystem.Verify(x => x.ReadAllText(It.IsAny<string>()), Times.Never);
                    }
                }
            }

            private class GivenADirectoryWithMultipleDownScripts : GivenADirectory
            {
                private readonly string[] _files = {
                    @".\20121212000000_MyScript_down.sql",
                    @".\subdirectory\20121212000001_MyScript_down.sql",
                    @".\20121212000002_MyScript_down.sql"
                };

                [SetUp]
                public new void BeforeEachTest()
                {
                    _mockFileSystem.Setup(x => x.ReadAllText(It.IsAny<string>())).Returns("");

                    _mockFileSystem.Setup(x => x.GetFiles(Path, FileSystemMigrationFinder.SqlFileSearchPattern, SearchOption.AllDirectories))
                                         .Returns(_files);
                }

                [TestFixture]
                public class WhenTellingTheFileSystemMigrationFinderToFindDownMigrations : GivenADirectoryWithMultipleDownScripts
                {
                    [Test]
                    public void ShouldReturnMultipleDownMigrations()
                    {
                        // act
                        var migrations = _migrationFinder.GetDownMigrations();

                        // assert
                        Assert.AreEqual(3, migrations.Count());
                    }
                }                
            }

            private class GivenADirectoryWithManyDifferentFiles : GivenADirectory
            {
                private readonly string[] _files = {
                    @".\20121212000000_MyScript_up.sql",
                    @".\readme.txt",
                    @".\subdirectory\20121212000003_MyScript_sideways.sql",
                    @".\dbo.MyStoredProcedure.sql",
                    @".\20121212000000_MyScript_down.sql"
                };

                [SetUp]
                public new void BeforeEachTest()
                {
                    _mockFileSystem.Setup(x => x.ReadAllText(It.IsAny<string>())).Returns("");

                    _mockFileSystem.Setup(x => x.GetFiles(Path, FileSystemMigrationFinder.SqlFileSearchPattern, SearchOption.AllDirectories))
                                         .Returns(_files);
                }

                [TestFixture]
                public class WhenTellingTheFileSystemMigrationFinderToFindMigrations : GivenADirectoryWithManyDifferentFiles
                {
                    [Test]
                    public void ShouldOnlyReturnUpMigrations()
                    {
                        // act
                        var migrations = _migrationFinder.GetUpMigrations();

                        // assert
                        Assert.AreEqual(1, migrations.Count());
                    }

                    [Test]
                    public void ShouldOnlyReturnDownMigrations()
                    {
                        // act
                        var migrations = _migrationFinder.GetDownMigrations();

                        // assert
                        Assert.AreEqual(1, migrations.Count());
                    }
                }                  
            }
        }
    }
}
