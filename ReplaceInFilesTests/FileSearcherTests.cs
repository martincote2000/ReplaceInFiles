namespace FinInFilesTests
{
    using System;
    using System.IO.Abstractions.TestingHelpers;    
    using FinInFilesTests.Extensions;
    using FluentAssertions;
    using ReplaceInFiles;
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
        public void Search_WithNoExtensions_ShouldReturnEmptyList()
        {
            // Arrange
            
            _fileSearcher.InDirectory("C:\\");

            // Act
            var result = _fileSearcher.Search();

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void Search_WithNoDirectoryDefined_ShouldThrowException()
        {
            // Act & assert
            var result = _fileSearcher.Invoking(x => x.Search()).Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void WithExtensions_WithNullOrEmptyExtension_ShouldIgnoreAddExtensions(string extension)
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
            var directory = "C:\\ExampleDirectory";
            var extensions = new[] { "txt", "csv" };

            _fileSystem.AddFile(Path.Combine(directory, "file1.txt"), new MockFileData("file content"));
            _fileSystem.AddFile(Path.Combine(directory, "file2.txt"), new MockFileData("file content"));
            _fileSystem.AddFile(Path.Combine(directory, "data.csv"), new MockFileData("csv data"));
            _fileSystem.AddDirectory($"{directory}\\Test");

            _fileSearcher
                .InDirectory(directory)
                .WithExtensions(extensions);

            // Act
            var result = _fileSearcher.Search();

            // Assert
            result.Should().HaveCount(3);
            result.Should().Contain(new[] { Path.Combine(directory, "file1.txt"), Path.Combine(directory, "file2.txt"), Path.Combine(directory, "data.csv") });
        }

        [Fact]
        public void Search_WithFolderContainsDifferentFileExtensions_ShouldOnlyReturnTheSpecifiedExtensions()
        {
            // Arrange
            var directory = "C:\\ExampleDirectory";
            var extensions = new[] { "txt", "csv" };

            _fileSystem.AddFile(Path.Combine(directory, "file1.gif"), new MockFileData("file content"));
            _fileSystem.AddFile(Path.Combine(directory, "file2.doc"), new MockFileData("file content"));
            _fileSystem.AddFile(Path.Combine(directory, "file3.txt"), new MockFileData("file content"));
            _fileSystem.AddFile(Path.Combine(directory, "file4.csv"), new MockFileData("csv data"));

            _fileSystem.AddDirectory($"{directory}\\Test");

            _fileSearcher
                .InDirectory(directory)
                .WithExtensions(extensions);

            // Act
            var result = _fileSearcher.Search();

            // Assert
            result.Should().HaveCount(2);
            result.Where(file => extensions.Any(ext => ext.Equals(_fileSystem.FileInfo.New(file).ExtensionWithoutDot(), StringComparison.CurrentCultureIgnoreCase)))
                .Should().HaveCount(2, "Only have the expected file extensions");
        }
    }

}