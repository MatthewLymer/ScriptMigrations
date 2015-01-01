using System.Collections.Generic;
using System.IO;
using System.Linq;
using Migrator.Facades;
using Migrator.Scripts;
using Moq;
using NUnit.Framework;

namespace Migrator.Tests.Scripts
{
    internal class FileSystemScriptFinderTests
    {
        private class GivenADirectory
        {
            private const string Path = ".";

            private Mock<IFileSystemFacade> _mockFileSystemFacade;
            private FileSystemScriptFinder _scriptFinder;

            [SetUp]
            public void BeforeEachTest()
            {
                _mockFileSystemFacade = new Mock<IFileSystemFacade>();

                _scriptFinder = new FileSystemScriptFinder(_mockFileSystemFacade.Object, Path);
            }

            private class GivenTheDirectoryIsEmpty : GivenADirectory
            {
                [SetUp]
                public new void BeforeEachTest()
                {
                    _mockFileSystemFacade.Setup(
                        x => x.GetFiles(Path, FileSystemScriptFinder.SqlFileSearchPattern, SearchOption.AllDirectories))
                        .Returns(new string[0]);
                }

                [TestFixture]
                public class WhenTellingTheFileSystemScriptFinderToFindUpScripts : GivenTheDirectoryIsEmpty
                {
                    [Test]
                    public void ShouldReturnAnEmptyEnumeration()
                    {
                        // act
                        IEnumerable<UpScript> upScripts = _scriptFinder.GetUpScripts();

                        // assert
                        Assert.IsEmpty(upScripts);
                    }
                }

                [TestFixture]
                public class WhenTellingTheFileSystemScriptFinderToFindDownScripts : GivenTheDirectoryIsEmpty
                {
                    [Test]
                    public void ShouldReturnAnEmptyEnumeration()
                    {
                        // act
                        var downScripts = _scriptFinder.GetDownScripts();

                        // assert
                        Assert.IsEmpty(downScripts);
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

                    _mockFileSystemFacade.Setup(
                        x => x.GetFiles(Path, FileSystemScriptFinder.SqlFileSearchPattern, SearchOption.AllDirectories))
                        .Returns(new[] {scriptPath});

                    _mockFileSystemFacade.Setup(x => x.ReadAllText(scriptPath)).Returns(Content);
                }

                [TestFixture]
                public class WhenTellingTheFileSystemScriptFinderToFindUpScripts : GivenADirectoryWithASingleUpScript
                {
                    [Test]
                    public void ShouldReturnASinglePopulatedUpScript()
                    {
                        // act
                        var scripts = _scriptFinder.GetUpScripts();

                        // assert
                        var script = scripts.Single();

                        Assert.AreEqual(Version, script.Version);
                        Assert.AreEqual(Name, script.Name);
                        Assert.AreEqual(Content, script.Content);
                    }

                    [Test]
                    public void ShouldNotAccessFilesystemIfContentPropertyIsNotRead()
                    {
                        var scripts = _scriptFinder.GetUpScripts();

                        Assert.IsNotEmpty(scripts);

                        _mockFileSystemFacade.Verify(x => x.ReadAllText(It.IsAny<string>()), Times.Never);
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
                    _mockFileSystemFacade.Setup(x => x.ReadAllText(It.IsAny<string>())).Returns("");
                    
                    _mockFileSystemFacade.Setup(x => x.GetFiles(Path, FileSystemScriptFinder.SqlFileSearchPattern, SearchOption.AllDirectories))
                                         .Returns(_scriptPaths);
                }

                [TestFixture]
                public class WhenTellingTheFileSystemScriptFinderToFindUpScripts : GivenADirectoryWithMultipleUpScripts
                {
                    [Test]
                    public void ShouldReturnMultipleUpScripts()
                    {
                        // act
                        var scripts = _scriptFinder.GetUpScripts();

                        // assert
                        Assert.AreEqual(3, scripts.Count());
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

                    _mockFileSystemFacade.Setup(
                        x => x.GetFiles(Path, FileSystemScriptFinder.SqlFileSearchPattern, SearchOption.AllDirectories))
                        .Returns(new[] { filePath });

                    _mockFileSystemFacade.Setup(x => x.ReadAllText(filePath)).Returns(Content);                    
                }

                [TestFixture]
                public class WhenTellingTheFileSystemScriptFinderToFindDownScripts : GivenADirectoryWithASingleDownScript
                {
                    [Test]
                    public void ShouldReturnASinglePopulatedDownScript()
                    {
                        // act
                        var scripts = _scriptFinder.GetDownScripts();

                        // assert
                        var script = scripts.Single();

                        Assert.AreEqual(Version, script.Version);
                        Assert.AreEqual(Name, script.Name);
                        Assert.AreEqual(Content, script.Content);                        
                    }

                    [Test]
                    public void ShouldNotAccessFilesystemIfContentPropertyIsNotRead()
                    {
                        var scripts = _scriptFinder.GetDownScripts();

                        Assert.IsNotEmpty(scripts);

                        _mockFileSystemFacade.Verify(x => x.ReadAllText(It.IsAny<string>()), Times.Never);
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
                    _mockFileSystemFacade.Setup(x => x.ReadAllText(It.IsAny<string>())).Returns("");

                    _mockFileSystemFacade.Setup(x => x.GetFiles(Path, FileSystemScriptFinder.SqlFileSearchPattern, SearchOption.AllDirectories))
                                         .Returns(_files);
                }

                [TestFixture]
                public class WhenTellingTheFileSystemScriptFinderToFindDownScripts : GivenADirectoryWithMultipleDownScripts
                {
                    [Test]
                    public void ShouldReturnMultipleDownScripts()
                    {
                        // act
                        var scripts = _scriptFinder.GetDownScripts();

                        // assert
                        Assert.AreEqual(3, scripts.Count());
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
                    _mockFileSystemFacade.Setup(x => x.ReadAllText(It.IsAny<string>())).Returns("");

                    _mockFileSystemFacade.Setup(x => x.GetFiles(Path, FileSystemScriptFinder.SqlFileSearchPattern, SearchOption.AllDirectories))
                                         .Returns(_files);
                }

                [TestFixture]
                public class WhenTellingTheFileSystemScriptFinderToFindScripts : GivenADirectoryWithManyDifferentFiles
                {
                    [Test]
                    public void ShouldOnlyReturnUpScripts()
                    {
                        // act
                        var scripts = _scriptFinder.GetUpScripts();

                        // assert
                        Assert.AreEqual(1, scripts.Count());
                    }

                    [Test]
                    public void ShouldOnlyReturnDownScripts()
                    {
                        // act
                        var scripts = _scriptFinder.GetDownScripts();

                        // assert
                        Assert.AreEqual(1, scripts.Count());
                    }
                }                  
            }
        }
    }
}
