using EnsureThat;
using System.Collections.Concurrent;
using System.IO.Abstractions;
using System.Text.RegularExpressions;

namespace OpsUtil.FileOperations
{
    public class FileReplacer : IFileReplacer
    {
        private readonly ConcurrentQueue<string> _filesQueue;
        private int _parallelsExecution;
        private string _startPattern;
        private string _endPattern;
        private int _reportEveryIteration;
        private Action<double, double> _reportProgressAction;
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

        public FileReplacer ParallelsExecution(int parallelsExecution)
        {
            Ensure.That(parallelsExecution).IsInRange(1, 10);

            _parallelsExecution = parallelsExecution;
            return this;
        }

        public FileReplacer ForFile(string filePath)
        {
            Ensure.That(filePath).IsNotNullOrWhiteSpace();

            if (!_fileSystem.File.Exists(filePath))
                throw new FileNotFoundException("File doesn't exists", filePath);

            _filesQueue.Enqueue(filePath);
            return this;
        }

        public FileReplacer ForFiles(List<string> files)
        {
            Ensure.That(files).IsNotNull();

            files.ForEach(f => ForFile(f));
            return this;
        }

        public FileReplacer IgnoreCase(bool ignorecase)
        {
            if (ignorecase)
                _regexOption = RegexOptions.IgnoreCase;
            else
                _regexOption = RegexOptions.None;

            return this;
        }

        public FileReplacer MatchPattern(string startPattern, string endPattern)
        {
            Ensure.That(startPattern).IsNotNullOrWhiteSpace();
            Ensure.That(endPattern).IsNotNullOrWhiteSpace();

            _startPattern = startPattern;
            _endPattern = endPattern;

            return this;
        }

        public FileReplacer ReportFileChange(Action<string, ReplacementVariable> reportFileChange)
        {
            Ensure.That(reportFileChange).IsNotNull();

            _reportFileChangeAction = reportFileChange;
            return this;
        }

        public FileReplacer ReportProgress(int every, Action<double, double> reportProgress)
        {
            Ensure.That(every).IsGt(0);
            Ensure.That(reportProgress).IsNotNull();

            _reportEveryIteration = every;
            _reportProgressAction = reportProgress;
            return this;
        }

        public FileReplacer ReplaceVariable(string variableName, string replacementValue)
        {
            Ensure.That(variableName).IsNotNullOrWhiteSpace();
            Ensure.That(replacementValue).IsNotNull();

            ReplacementVariable replacementVariable = new()
            {
                Name = variableName.Trim(),
                Value = replacementValue.Trim()
            };

            _replacementVariables.Add(replacementVariable);
            return this;
        }

        public FileReplacer ReplaceVariable(params string[] rawValues)
        {
            Ensure.That(rawValues).IsNotNull();

            foreach (var rawValue in rawValues)
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

        public void Replace()
        {
            if (!_filesQueue.Any())
                return;

            if (!_replacementVariables.Any())
            {
                throw new InvalidOperationException("No variable replacements provided.");
            }

            long fileProcessed = 0;
            long fileCount = _filesQueue.Count;

            Parallel.ForEach(
                _filesQueue,
                new ParallelOptions { MaxDegreeOfParallelism = _parallelsExecution },
                filePath =>
                {
                    ReplaceInFile(filePath);

                    fileProcessed = Interlocked.Increment(ref fileProcessed);
                    TryReportProgress(fileCount, fileProcessed);

                    _filesQueue.TryDequeue(out string dequeueValue);
                });

            TryReportProgress(fileCount, fileProcessed);
        }

        private void ReplaceInFile(string filePath)
        {
            var fileContent = _fileSystem.File.ReadAllText(filePath);
            if (MatchPatternDefined())
            {
                ReplaceInFileWithMatchPattern(filePath, fileContent);
            }
            else
            {
                ReplaceInFileWithExactString(filePath, fileContent);
            }
        }

        private void ReplaceInFileWithMatchPattern(string filePath, string fileContent)
        {
            int variableFoundCount = 0;
            var regexPattern = @"\" + _startPattern + "(.*?)" + _endPattern;

            fileContent = Regex.Replace(fileContent, regexPattern, match =>
            {
                var variableName = match.Groups[1].Value;
                var variable = _replacementVariables.Find(x => x.Name == variableName);
                if (variable != null)
                {
                    TryReportFileChange(filePath, variable);

                    variableFoundCount++;
                    return variable.Value;
                }
                else
                {
                    return match.Value;
                }
            }, _regexOption);

            if (variableFoundCount > 0)
                _fileSystem.File.WriteAllText(filePath, fileContent);
        }

        private void ReplaceInFileWithExactString(string filePath, string fileContent)
        {
            int variableCount = 0;
            foreach (var variable in _replacementVariables)
            {
                fileContent = Regex.Replace(fileContent, variable.Name, match =>
                {
                    TryReportFileChange(filePath, variable);

                    variableCount++;
                    return variable.Value;
                }, _regexOption);
            }

            if (variableCount > 0)
                _fileSystem.File.WriteAllText(filePath, fileContent);
        }

        private void TryReportProgress(long fileCount, long fileProcessed)
        {
            if (_reportProgressAction != null && fileProcessed % _reportEveryIteration == 0)
            {
                _reportProgressAction(fileCount, fileProcessed);
            }
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
