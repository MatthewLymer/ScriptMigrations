using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Moq;
using NUnit.Framework;

namespace Migrator.Tests
{
    class GivenAnEmptyDirectory
    {
        private FileSystemScriptFinder _scriptFinder;

        [SetUp]
        public void BeforeEachTest()
        {
            const string path = ".";

            var mockFileSystemFacade = new Mock<IFileSystemFacade>();

            mockFileSystemFacade.Setup(x => x.GetFiles(path, FileSystemScriptFinder.SqlFileSearchPattern, SearchOption.AllDirectories))
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

    class GivenADirectoryWithASingleUpScript
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

            _mockFileSystemFacade.Setup(x => x.GetFiles(path, FileSystemScriptFinder.SqlFileSearchPattern, SearchOption.AllDirectories))
                .Returns(new[] { filePath });

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

    class GivenADirectoryWithMultipleUpScripts
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

            _upScriptPaths = new[]
            {
                @".\20121212000000_MyScript_up.sql",
                @".\20121212000001_MyScript_up.sql",
                @".\20121212000002_MyScript_up.sql",
            };

            _mockFileSystemFacade.Setup(x => x.GetFiles(TestPath, FileSystemScriptFinder.SqlFileSearchPattern, SearchOption.AllDirectories))
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

            [Test]
            public void ShouldThrowExceptionIfMultipleUpScriptsHaveTheSameVersion()
            {
                // arrange
                _upScriptPaths[0] = @".\20000000000000_MyUpScriptA_up.sql";
                _upScriptPaths[1] = @".\20000000000000_MyUpScriptB_up.sql";

                try
                {
                    // act
                    _scriptFinder.GetUpScripts();
                    Assert.Fail();
                }
                catch (DuplicateScriptVersionException)
                {
                    // assert
                    Assert.Pass();
                }
            }
        }
    }

    public class DuplicateScriptVersionException : Exception
    {
    }

    public class FileSystemScriptFinder : IScriptFinder
    {
        public const string SqlFileSearchPattern = "*.sql";

        private readonly IFileSystemFacade _fileSystemFacade;
        private readonly string _path;

        public FileSystemScriptFinder(IFileSystemFacade fileSystemFacade, string path)
        {
            _fileSystemFacade = fileSystemFacade;
            _path = path;
        }

        public IEnumerable<UpScript> GetUpScripts()
        {
            var files = _fileSystemFacade.GetFiles(_path, SqlFileSearchPattern, SearchOption.AllDirectories);

            var upScripts = new List<UpScript>();

            foreach (var file in files)
            {
                var fileSegments = Path.GetFileNameWithoutExtension(file).Split('_');

                var upScript = new UpScript(long.Parse(fileSegments[0]), fileSegments[1], _fileSystemFacade.ReadAllText(file));

                upScripts.Add(upScript);
            }

            if (upScripts.GroupBy(x => x.Version).Any(g => g.Count() > 1))
            {
                throw new DuplicateScriptVersionException();
            }

            return upScripts;
        }

        public IEnumerable<DownScript> GetDownScripts()
        {
            throw new NotImplementedException();
        }
    }

    public interface IFileSystemFacade
    {
        string[] GetFiles(string path, string searchPattern, SearchOption searchOption);
        string ReadAllText(string path);
    }
}
