namespace OpsUtil.FileOperationsTests
{
    using System;
    using System.IO.Abstractions.TestingHelpers;
    using FluentAssertions;
    using OpsUtil.FileOperations;
    using OpsUtil.FileOperationsTests.Extensions;
    using Xunit;

    public class FileSearcherTests
    {
        private readonly MockFileSystem _fileSystem;
        private readonly FileSearcher _fileSearcher;

        public FileSearcherTests()
        {
            _fileSystem = new MockFileSystem();            
            _fileSearcher = new FileSearcher(_fileSystem);
        }

        [Fact]
        public void Search_WithNoExtensionsDefined_ShouldReturnAllFiles()
        {
            // Arrange
            var rootFolder = "/root";
            _fileSystem.AddDirectory(rootFolder);
            CreateGenericFiles(rootFolder, 2, "txt");
            CreateGenericFiles(rootFolder, 1, "csv");

            _fileSearcher.InDirectory(rootFolder);

            // Act
            var result = _fileSearcher.Search();

            // Assert
            result.Should().HaveCount(3);
        }

        [Fact]
        public void Search_WithNoDirectoryDefined_ShouldThrowException()
        {
            // Act & assert
            _fileSearcher.Invoking(x => x.Search()).Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void WithExtensions_WithNullOrEmptyExtension_ShouldNotAddExtension(string extension)
        {
            // Arrange
            _fileSearcher.WithExtensions(extension);

            // Act & assert
            _fileSearcher.HasExtensionDefined().Should().BeFalse();
        }

        [Fact]
        public void Search_WithExtensionsInOneDirectory_ShouldReturnMatchingFiles()
        {
            // Arrange
            var rootFolder = "/root";
            var extensions = new[] { "txt", "csv" };

            _fileSystem.AddDirectory(rootFolder);
            _fileSystem.AddDirectory($"{rootFolder}/Test");

            CreateGenericFiles(rootFolder, 2, "txt");
            CreateGenericFiles(rootFolder, 1, "csv");
            CreateGenericFiles(rootFolder, 1, "gif");
            CreateGenericFiles(rootFolder, 1, "bmp");
            CreateGenericFiles(rootFolder, 1, "pdf");

            _fileSearcher
                .InDirectory(rootFolder)
                .WithExtensions(extensions);

            // Act
            var result = _fileSearcher.Search();

            // Assert
            result.Should().HaveCount(3);
            result.Exists(x => x.EndsWith($"file0.txt")).Should().BeTrue();
            result.Exists(x => x.EndsWith("file1.txt")).Should().BeTrue();
            result.Exists(x => x.EndsWith("file0.csv")).Should().BeTrue();
        }

        [Fact]
        public void Search_WithFolderContainsDifferentFileExtensions_ShouldOnlyReturnTheSpecifiedExtensions()
        {
            // Arrange
            var rootFolder = "/root";
            _fileSystem.AddDirectory(rootFolder);
            _fileSystem.AddDirectory($"{rootFolder}/Test");
                        
            var extensions = new[] { "txt", "csv" };

            CreateGenericFiles(rootFolder, 1, "gif");
            CreateGenericFiles(rootFolder, 1, "doc");
            CreateGenericFiles(rootFolder, 1, "txt");
            CreateGenericFiles(rootFolder, 1, "csv");

            _fileSearcher
                .InDirectory(rootFolder)
                .WithExtensions(extensions);

            // Act
            var result = _fileSearcher.Search();

            // Assert
            result.Should().HaveCount(2);
            result.Where(file => extensions.Any(ext => ext.Equals(_fileSystem.FileInfo.New(file).ExtensionWithoutDot(), StringComparison.CurrentCultureIgnoreCase)))
                .Should().HaveCount(2, "Only have the expected file extensions");
        }


        private void CreateGenericFiles(string folder, int fileCount, string extension)
        {
            for (int index = 0; index < fileCount; index++)
            {
                _fileSystem.AddFile(Path.Combine(folder, $"file{index}.{extension}"), new MockFileData($"file content of file{index}"));
            }
        }
    }

}