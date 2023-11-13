using FluentAssertions;
using Moq;
using OpsUtil.FileOperations;
using Serilog;
using System.IO.Abstractions.TestingHelpers;

namespace OpsUtil.FileOperationsTests
{
    public class FileReplacerTests
    {
        private readonly MockFileSystem _fileSystem;
        private readonly FileReplacer _replacer;

        public FileReplacerTests()
        {
            _fileSystem = new MockFileSystem();
            _replacer = new FileReplacer(_fileSystem);
        }

        [Fact]
        public void Search_WithNoParameterDefined_ShouldThrowException()
        {
            // Arrange
            var filePath = "C:\\file1.txt";
            var mockFile = new MockFileData("Sample text without variable to replace");
            var initialLastWriteTime = mockFile.LastWriteTime;
            _fileSystem.AddFile(filePath, mockFile);

            _replacer.ForFile("C:\\file1.txt");

            // Act & Assert
            _replacer.Invoking(x => x.Replace()).Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Search_WithNoFileToReplace_ShouldNotThrowException()
        {
            // Arrange
            _replacer.ReplaceVariable("ParameterName1=MyValue1");


            // Act & Assert
            _replacer.Invoking(x => x.Replace()).Should().NotThrow();
        }

        [Theory]
        [InlineData(1, false)]
        [InlineData(10, false)]
        [InlineData(0, true)]
        [InlineData(11, true)]
        [InlineData(-1, true)]
        public void Search_WhenDefinedParallelReplacementValue_ShouldBeAlignedWithLimitSpecified(int parallelReplacement, bool shouldThrowException)
        {
            // Act & Assert
            if (shouldThrowException)
                _replacer.Invoking(x => x.ParallelsExecution(parallelReplacement)).Should().Throw<Exception>();
            else
                _replacer.Invoking(x => x.ParallelsExecution(parallelReplacement)).Should().NotThrow();
        }

        [Fact]
        public void Search_WhenFileDoesntExists_ShouldThrowException()
        {
            // Arrange
            var filePath = "C:\\file1.txt";

            // Act & assert
            _replacer.Invoking(x => x.ForFile(filePath)).Should().Throw<FileNotFoundException>();
        }

        [Fact]
        public void Search_WhenFileDontHaveParameterToReplace_ShouldReplaceFile()
        {
            // Arrange
            var filePath = "C:\\file1.txt";
            var mockFile = new MockFileData("Sample text without variable to replace");
            _fileSystem.AddFile(filePath, mockFile);
            var fileInfo = _fileSystem.FileInfo.New(filePath);
            var initialLastWriteTime = fileInfo.LastWriteTime;

            _replacer
                .ReplaceVariable("ParameterName1=MyValue1")
                .ForFile(filePath);

            // Act 
            _replacer.Replace();

            // Assert             
            _fileSystem.FileInfo.New(filePath).LastWriteTime.Should().Be(initialLastWriteTime);
        }

        [Theory]
        [InlineData("ParameterName1;MyValue1;Test123")]
        [InlineData("ParameterName1;MyValue1=1")]
        [InlineData("ParameterName1")]
        public void Search_WhenParameterDoesntRespectTheStandard_ShouldThrowException(string wrongParameterPattern)
        {
            // Act & Assert
            _replacer.Invoking(x => x.ReplaceVariable(wrongParameterPattern)).Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Search_WhenFileHaveParameterToReplace_ShouldReplaceHasExpected()
        {
            var filePath = "C:\\file1.txt";
            var fileContent = "Sample text without ${ParameterName1} to replace";
            var mockFile = new MockFileData(fileContent);

            _fileSystem.AddFile(filePath, mockFile);
            var initialLastWriteTime = _fileSystem.FileInfo.New(filePath).LastWriteTime;

            _replacer
                .ReplaceVariable("ParameterName1=MyValue1")
                .ParallelsExecution(1)
                .ForFile(filePath);

            // Act 
            _replacer.Replace();

            // Assert 
            _fileSystem.File.ReadAllText(filePath).Should().NotBe(fileContent);
            _fileSystem.FileInfo.New(filePath).LastWriteTime.Should().NotBe(initialLastWriteTime);
        }

        [Fact]
        public void Search_WhenFileContainsMultipleParameters_ShouldReplaceAll()
        {
            var filePath = "C:\\file1.txt";
            var value1 = "John";
            var value2 = "Terrible";
            var value3 = "Thanks";

            var fileContent = "Hello ${ParameterName1}. My name is ${ParameterName2}. ${ParameterName1} ${ParameterName3}";
            var expectedFileContent = $"Hello {value1}. My name is {value2}. {value1} {value3}";
            var mockFile = new MockFileData(fileContent);

            _fileSystem.AddFile(filePath, mockFile);
            var initialLastWriteTime = _fileSystem.FileInfo.New(filePath).LastWriteTime;

            var parameters = $"ParameterName1={value1};ParameterName2={value2};ParameterName3={value3}";

            _replacer
                .ReplaceVariable(parameters)
                .ParallelsExecution(1)
                .ForFile(filePath);

            // Act 
            _replacer.Replace();

            // Assert 
            _fileSystem.File.ReadAllText(filePath).Should().Be(expectedFileContent);
            _fileSystem.FileInfo.New(filePath).LastWriteTime.Should().NotBe(initialLastWriteTime);
        }
    }
}
