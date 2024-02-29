using EnsureThat;
using System.Collections.Concurrent;
using System.IO.Abstractions;
using System.Text;
using System.Text.RegularExpressions;

namespace OpsUtil.FileOperations
{
    public class FileReplacer : IFileReplacer
    {
        private readonly ConcurrentQueue<string> _filesQueue;
        private int _parallelsExecution;
        private string _startPattern;
        private string _endPattern;
        private RegexOptions _regexOption;
        private Action<string, ReplacementVariable>? _reportFileChangeAction;
        private readonly List<ReplacementVariable> _replacementVariables;
        private readonly IFileSystem _fileSystem;

        public FileReplacer(IFileSystem fileSystem)
        {
            _parallelsExecution = 5;
            _regexOption = RegexOptions.None;
            _filesQueue = new ConcurrentQueue<string>();
            _replacementVariables = new List<ReplacementVariable>();
            _fileSystem = fileSystem;

            _startPattern = "${";
            _endPattern = "}";
        }

        public IFileReplacer ParallelsExecution(int parallelsExecution)
        {
            Ensure.That(parallelsExecution).IsInRange(1, 10);

            _parallelsExecution = parallelsExecution;
            return this;
        }

        public IFileReplacer ForFile(string filePath)
        {
            Ensure.That(filePath).IsNotNullOrWhiteSpace();

            if (!_fileSystem.File.Exists(filePath))
                throw new FileNotFoundException("File doesn't exists", filePath);

            _filesQueue.Enqueue(filePath);
            return this;
        }

        public IFileReplacer ForFiles(List<string> files)
        {
            Ensure.That(files).IsNotNull();

            files.ForEach(f => ForFile(f));
            return this;
        }

        public IFileReplacer IgnoreCase(bool ignorecase)
        {
            if (ignorecase)
                _regexOption = RegexOptions.IgnoreCase;
            else
                _regexOption = RegexOptions.None;

            return this;
        }

        public IFileReplacer MatchPattern(string startPattern, string endPattern)
        {
            Ensure.That(startPattern).IsNotNull();
            Ensure.That(endPattern).IsNotNull();

            _startPattern = startPattern;
            _endPattern = endPattern;

            return this;
        }

        public IFileReplacer ReportFileChange(Action<string, ReplacementVariable> reportFileChange)
        {
            Ensure.That(reportFileChange).IsNotNull();

            _reportFileChangeAction = reportFileChange;
            return this;
        }

        public IFileReplacer ReplaceVariable(string variableName, string replacementValue)
        {
            Ensure.That(variableName).IsNotNullOrWhiteSpace();
            Ensure.That(replacementValue).IsNotNull();

            if (MatchPatternDefined())
            {
                if (!variableName.StartsWith(_startPattern))
                {
                    variableName = _startPattern + variableName;
                }
                if (!variableName.EndsWith(_endPattern))
                {
                    variableName += _endPattern;
                }
            }

            ReplacementVariable replacementVariable = new()
            {
                Name = variableName.Trim(),
                Value = replacementValue.Trim()
            };

            _replacementVariables.Add(replacementVariable);
            return this;
        }

        public IFileReplacer ReplaceVariable(params string[] rawParameters)
        {
            Ensure.That(rawParameters).IsNotNull();

            foreach (var rawValue in rawParameters)
            {
                var splitedBySemicolons = rawValue.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();

                splitedBySemicolons.ForEach(param =>
                {
                    var splitedValues = param.Split('=', StringSplitOptions.RemoveEmptyEntries);

                    if (splitedValues.Length != 2)
                        throw new ArgumentException($"Replace parameter {param} doesn't respect the expected structure (example: ParameterName1=MyValue1)");

                    ReplaceVariable(splitedValues[0], splitedValues[1]);
                });
            }
            return this;
        }

        public bool Replace()
        {
            if (!_filesQueue.Any())
                return false;

            if (!_replacementVariables.Any())
            {
                throw new InvalidOperationException("No variable replacements provided.");
            }

            long fileReplaced = 0;
            long fileProcessed = 0;
            long fileCount = _filesQueue.Count;

            Parallel.ForEach(
                _filesQueue,
                new ParallelOptions { MaxDegreeOfParallelism = _parallelsExecution },
                filePath =>
                {
                    if (ReplaceInFile(filePath))
                        fileReplaced = Interlocked.Increment(ref fileReplaced);

                    fileProcessed = Interlocked.Increment(ref fileProcessed);

                    _filesQueue.TryDequeue(out string dequeueValue);
                });


            return fileReplaced > 0;
        }

        private bool ReplaceInFile(string filePath)
        {
            var fileContent = _fileSystem.File.ReadAllText(filePath);
            var regexPatternExactString = @"\b{0}\b";
            var regexPatternWithEscapeCharacter = @"{0}";
            int replacementCount = 0;

            foreach (var variable in _replacementVariables)
            {
                string matchPattern;
                
                var escapedVariableName = Regex.Escape(variable.Name);
                if (!MatchPatternDefined() && escapedVariableName == variable.Name)
                    matchPattern = string.Format(regexPatternExactString, escapedVariableName);
                else if (MatchPatternDefined() && escapedVariableName == variable.Name)
                    matchPattern = string.Format(regexPatternWithEscapeCharacter, escapedVariableName);
                else
                    matchPattern = string.Format(regexPatternWithEscapeCharacter, escapedVariableName);

                //if (escapedVariableName == variable.Name)
                //    matchPattern = string.Format(regexPatternExactString, escapedVariableName);
                //else
                //    matchPattern = string.Format(regexPatternWithEscapeCharacter, escapedVariableName);

                fileContent = Regex.Replace(fileContent, matchPattern, match =>
                {
                    TryReportFileChange(filePath, variable);

                    replacementCount++;
                    return variable.Value;
                }, _regexOption);
            }

            if (replacementCount > 0)
            {
                // Update files only when variables has been updated.
                _fileSystem.File.WriteAllText(filePath, fileContent);
                return true;
            }
            return false;
        }

        private bool ReplaceVariable(string text, string variableName, string variableValue)
        {
            var regexPatternExactString = @"\b{0}\b";
            var regexPatternWithEscapeCharacter = @"\{0}";
            
            string matchPattern;
            var escapedVariableName = Regex.Escape(variableName);
            if (escapedVariableName == variableName)
                matchPattern = string.Format(regexPatternExactString, escapedVariableName);
            else
                matchPattern = string.Format(regexPatternWithEscapeCharacter, escapedVariableName);

            Regex.Replace(text, matchPattern, match =>
            {
                return variableValue;
            }, _regexOption);

            return false;
        }

        private string ReplaceVariable2(string text, string variableName, string variableValue)
        {
            var regexPatternExactString = @"\b{0}\b";
            var regexPatternWithEscapeCharacter = @"\{0}";

            string matchPattern;
            var escapedVariableName = Regex.Escape(variableName);
            if (escapedVariableName == variableName)
                matchPattern = string.Format(regexPatternExactString, escapedVariableName);
            else
                matchPattern = string.Format(regexPatternWithEscapeCharacter, escapedVariableName);

            // Capture the result of Regex.Replace
            string replacedText = Regex.Replace(text, matchPattern, match =>
            {
                return variableValue;
            }, _regexOption);

            // Return the modified string
            return replacedText;
        }

        private void TryReportFileChange(string filePath, ReplacementVariable replacementVariable)
        {
            if (_reportFileChangeAction != null)
            {
                _reportFileChangeAction(filePath, replacementVariable);
            }
        }

        private bool MatchPatternDefined()
        {
            return !string.IsNullOrWhiteSpace(_startPattern) && !string.IsNullOrWhiteSpace(_endPattern);
        }

    }
}
