using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Moq;
using NUnit.Framework;

namespace Migrator.Tests
{
    class GivenADirectoryFacade
    {
        protected Mock<IDirectoryFacade> _mockDirectoryFacade;

        [SetUp]
        public void BeforeEachTest()
        {
            _mockDirectoryFacade = new Mock<IDirectoryFacade>();
        }

        [TestFixture]
        public class WhenTellingTheFileSystemScriptFinderToFindUpScripts : GivenADirectoryFacade
        {
            [Test]
            public void ShouldThrowExceptionIfPathDoesNotExist()
            {
                // arrange
                _mockDirectoryFacade.Setup(x => x.GetFiles("", "", SearchOption.AllDirectories)).Throws<DirectoryNotFoundException>();

                var scriptFinder = new FileSystemScriptFinder(_mockDirectoryFacade.Object, "");

                try
                {
                    // act
                    scriptFinder.GetUpScripts();
                    Assert.Fail();
                }
                catch (DirectoryNotFoundException)
                {
                    // assert
                    Assert.Pass();
                }
            }

            [Test]
            public void ShouldReturnEmptyEnumerationIfNoFilesExist()
            {
                // arrange
                const string basePath = "mybasepath";

                _mockDirectoryFacade.Setup(x => x.GetFiles(basePath, "", SearchOption.AllDirectories)).Returns(new string[0]);

                var scriptFinder = new FileSystemScriptFinder(_mockDirectoryFacade.Object, basePath);

                // act
                var upScripts = scriptFinder.GetUpScripts();

                // assert
                Assert.IsEmpty(upScripts);
            }
        }
    }

    public class FileSystemScriptFinder : IScriptFinder
    {
        private readonly IDirectoryFacade _directoryFacade;
        private readonly string _basePath;

        public FileSystemScriptFinder(IDirectoryFacade directoryFacade, string basePath)
        {
            _directoryFacade = directoryFacade;
            _basePath = basePath;
        }

        public IEnumerable<UpScript> GetUpScripts()
        {
            _directoryFacade.GetFiles(_basePath, "", SearchOption.AllDirectories);

            return Enumerable.Empty<UpScript>();
        }

        public IEnumerable<DownScript> GetDownScripts()
        {
            throw new NotImplementedException();
        }
    }

    public interface IDirectoryFacade
    {
        string[] GetFiles(string path, string searchPattern, SearchOption searchOption);
    }
}
