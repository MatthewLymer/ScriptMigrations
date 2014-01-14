using System.IO;
using System.Linq;
using Moq;
using NUnit.Framework;

namespace Migrator.Tests
{
    internal class FileSystemScriptFinderTests
    {
        private class GivenAnEmptyDirectory
        {
            private FileSystemScriptFinder _scriptFinder;

            [SetUp]
            public void BeforeEachTest()
            {
                const string path = ".";

                var mockFileSystemFacade = new Mock<IFileSystemFacade>();

                mockFileSystemFacade.Setup(
                    x => x.GetFiles(path, FileSystemScriptFinder.SqlFileSearchPattern, SearchOption.AllDirectories))
                    .Returns(new string[0]);

                _scriptFinder = new FileSystemScriptFinder(mockFileSystemFacade.Object, path);
            }

            [TestFixture]
            public class WhenTellingTheFileSystemScriptFinderToFindUpScripts : GivenAnEmptyDirectory
            {
                [Test]
                public void ShouldReturnAnEmptyEnumeration()
                {
                    // act
                    var upScripts = _scriptFinder.GetUpScripts();

                    // assert
                    Assert.IsEmpty(upScripts);
                }
            }
        }

        private class GivenADirectoryWithASingleUpScript
        {
            private FileSystemScriptFinder _scriptFinder;

            private Mock<IFileSystemFacade> _mockFileSystemFacade;

            private const long Version = 20150105235959;
            private const string Name = "MyMigration";
            private const string Content = "-- This is a migration";

            [SetUp]
            public void BeforeEachTest()
            {
                const string path = ".";

                _mockFileSystemFacade = new Mock<IFileSystemFacade>();

                var filePath = string.Format(@".\{0}_{1}_up.sql", Version, Name);

                _mockFileSystemFacade.Setup(
                    x => x.GetFiles(path, FileSystemScriptFinder.SqlFileSearchPattern, SearchOption.AllDirectories))
                    .Returns(new[] {filePath});

                _mockFileSystemFacade.Setup(x => x.ReadAllText(filePath)).Returns(Content);

                _scriptFinder = new FileSystemScriptFinder(_mockFileSystemFacade.Object, path);
            }

            [TestFixture]
            public class WhenTellingTheFileSystemScriptFinderToFindUpScripts : GivenADirectoryWithASingleUpScript
            {
                [Test]
                public void ShouldReturnASinglePopulatedUpMigration()
                {
                    // act
                    var upScripts = _scriptFinder.GetUpScripts();

                    // assert
                    var upScript = upScripts.Single();

                    Assert.AreEqual(Version, upScript.Version);
                    Assert.AreEqual(Name, upScript.Name);
                    Assert.AreEqual(Content, upScript.Content);
                }
            }
        }

        private class GivenADirectoryWithMultipleUpScripts
        {
            private const string TestPath = ".";

            private string[] _upScriptPaths;

            private FileSystemScriptFinder _scriptFinder;

            private Mock<IFileSystemFacade> _mockFileSystemFacade;

            [SetUp]
            public void BeforeEachTest()
            {
                _mockFileSystemFacade = new Mock<IFileSystemFacade>();

                _mockFileSystemFacade.Setup(x => x.ReadAllText(It.IsAny<string>())).Returns("");

                _upScriptPaths = new[] {
                    @".\20121212000000_MyScript_up.sql",
                    @".\20121212000001_MyScript_up.sql",
                    @".\20121212000002_MyScript_up.sql",
                };

                _mockFileSystemFacade.Setup(
                    x => x.GetFiles(TestPath, FileSystemScriptFinder.SqlFileSearchPattern, SearchOption.AllDirectories))
                    .Returns(_upScriptPaths);

                _scriptFinder = new FileSystemScriptFinder(_mockFileSystemFacade.Object, TestPath);
            }

            [TestFixture]
            public class WhenTellingTheFileSystemScriptFinderToFindUpScripts : GivenADirectoryWithMultipleUpScripts
            {
                [Test]
                public void ShouldReturnMultipleUpScripts()
                {
                    // act
                    var upScripts = _scriptFinder.GetUpScripts();

                    // assert
                    Assert.AreEqual(3, upScripts.Count());
                }
            }
        }
    }
}
