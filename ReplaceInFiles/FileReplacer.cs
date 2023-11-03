using EnsureThat;
using Serilog;
using System.Collections.Concurrent;
using System.IO.Abstractions;
using System.Text.RegularExpressions;

namespace ReplaceInFiles
{
    public class FileReplacer : IFileReplacer
    {
        private readonly ConcurrentQueue<string> _filesQueue;
        private int _parallelsExecution;
        private string _startPattern;
        private string _endPattern;
        private int _reportEveryIteration;
        private Action<double, double> _reportProgressAction;
        private bool _verbose;
        private RegexOptions _regexOption;
        private Action<string, string, string> _reportFileChangeAction;
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
            EnsureThat.Ensure.That(parallelsExecution).IsInRange(1, 10);

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

        public FileReplacer VerboseMode(bool verbose)
        {
            _verbose = verbose;
            return this;
        }

        public FileReplacer ReportFileChange(Action<string, string, string> reportFileChange)
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

        public FileReplacer MatchPattern(string startPattern, string endPattern)
        {
            Ensure.That(startPattern).IsNotNull();
            Ensure.That(endPattern).IsNotNull();

            _startPattern = startPattern;
            _endPattern = endPattern;

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

        private void TryReportProgress(long fileCount, long fileProcessed)
        {   
            if (_reportProgressAction != null && (fileProcessed % _reportEveryIteration) == 0)
            {
                _reportProgressAction(fileCount, fileProcessed);
            }
        }

        private void TryReportFileChange(string filePath, ReplacementVariable replacementVariable)
        {
            if(_reportFileChangeAction != null)
            {
                _reportFileChangeAction(filePath, replacementVariable.Name, replacementVariable.Value);
            }
        }

        private void ReplaceInFile(string filePath)
        {
            var content = _fileSystem.File.ReadAllText(filePath);
            int variableFound = 0;

            if (string.IsNullOrEmpty(_startPattern) && string.IsNullOrEmpty(_endPattern))
            {
                foreach (var variable in _replacementVariables)
                {
                    content = Regex.Replace(content, variable.Name,  match =>
                    {
                        TryReportFileChange(filePath, variable);

                        variableFound++;
                        return variable.Value;
                    }, _regexOption);
                }
            }
            else
            {
                var regexPattern = @"\" + _startPattern + "(.*?)" + _endPattern;
                content = Regex.Replace(content, regexPattern, match =>
                {
                    var variableName = match.Groups[1].Value;
                    var variable = _replacementVariables.Find(x => x.Name == variableName);
                    if (variable != null)
                    {
                        TryReportFileChange(filePath, variable);

                        variableFound++;
                        return variable.Value;
                    }
                    else
                    {
                        return match.Value;
                    }
                }, _regexOption);
            }

            if (variableFound > 0)
                _fileSystem.File.WriteAllText(filePath, content);
        }

        
    }
}
