using FluentAssertions;
using OpsUtil.FileOperations;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpsUtil.FileOperationsTests
{
    public class FolderSearcherTests
    {
        private MockFileSystem _fileSystem;
        private FolderSearcher _folderSearcher;
        private string _rootDirectory;

        public FolderSearcherTests()
        {
            _rootDirectory = "C:\\MyProject\\";
            _rootDirectory = "C:\\Projects\\FlowFitTMS\\FlowFit.Web.FlowFit";
            _fileSystem = new MockFileSystem();

            _folderSearcher = new FolderSearcher(_fileSystem);
            _folderSearcher.InDirectory(_rootDirectory);
        }


        [Fact]
        public void Search_WhenIgnoreFolderAreSpecified_ShouldNotBePresentIntoFolderList()
        {
            // Arrange            
            string ignoreFolder1 = "node_modules";
            string ignoreFolder2 = ".git";

            _fileSystem.AddDirectory($"{_rootDirectory}\\Folder1");
            _fileSystem.AddDirectory($"{_rootDirectory}\\node_modules");
            _fileSystem.AddDirectory($"{_rootDirectory}\\.git");
            _fileSystem.AddDirectory($"{_rootDirectory}\\Folder2");
            _fileSystem.AddDirectory($"{_rootDirectory}\\Folder2\\NODE_MODULES\\Test");
            _fileSystem.AddDirectory($"{_rootDirectory}\\Folder3");
            _fileSystem.AddDirectory($"{_rootDirectory}\\Folder3\\.git\\OtherFolder");
            _fileSystem.AddDirectory($"{_rootDirectory}\\Folder4");
            _fileSystem.AddDirectory($"{_rootDirectory}\\Folder4\\SubFolder4");

            _folderSearcher.IgnoreFolderNames(ignoreFolder1, ignoreFolder2);

            // Act
            var folders = _folderSearcher.Search();

            // Assert
            folders.Should().HaveCount(6);

            foreach (var folder in folders)
            {
                folder.Contains(ignoreFolder1, StringComparison.InvariantCultureIgnoreCase).Should().BeFalse("");
                folder.Contains(ignoreFolder2, StringComparison.InvariantCultureIgnoreCase).Should().BeFalse();
            }
        }


    }
}
