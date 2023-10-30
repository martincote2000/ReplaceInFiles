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
        private int _parallelsReplacement;
        private string _startPattern;
        private string _endPattern;
        private bool _debug;
        private readonly List<ReplacementVariable> _replacementVariables;
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;

        public FileReplacer(ILogger logger, IFileSystem fileSystem)
        {
            _filesQueue = new ConcurrentQueue<string>();
            _parallelsReplacement = 5;
            _filesQueue = new ConcurrentQueue<string>();
            _replacementVariables = new List<ReplacementVariable>();
            _logger = logger;
            _fileSystem = fileSystem;

            _startPattern = "${";
            _endPattern = "}";
        }

        public FileReplacer ParallelsReplacement(int parallelsReplacement)
        {
            EnsureThat.Ensure.That(parallelsReplacement).IsInRange(1, 10);

            _parallelsReplacement = parallelsReplacement;
            return this;
        }

        public FileReplacer ForFile(string filePath)
        {
            Ensure.That(filePath).IsNotEmptyOrWhiteSpace();

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

        public FileReplacer DebugMode(bool? debug)
        {
            _debug = debug == true;
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
            Ensure.That(variableName).IsNotEmptyOrWhiteSpace();
            Ensure.That(replacementValue).IsNotNull();

            ReplacementVariable replacementVariable = new()
            {
                Name = variableName,
                Value = replacementValue
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

            Parallel.ForEach(
                _filesQueue,
                new ParallelOptions { MaxDegreeOfParallelism = _parallelsReplacement },
                filePath =>
                {
                    ReplaceInFile(filePath);
                    _filesQueue.TryDequeue(out string dequeueValue);
                });
        }

        private void ReplaceInFile(string filePath)
        {
            var content = _fileSystem.File.ReadAllText(filePath);
            int variableFound = 0;

            if (string.IsNullOrEmpty(_startPattern) && string.IsNullOrEmpty(_endPattern))
            {
                foreach (var variable in _replacementVariables)
                {
                    content = Regex.Replace(content, variable.Name, match =>
                    {   
                        _logger.Information("Changed | {filePath} | {variableName} for {replacement}", filePath, variable.Name, variable.Value);
                        variableFound++;
                        return variable.Value;
                    });
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
                        _logger.Information("Changed | {filePath} | {variableName} for {replacement}", filePath, variableName, variable.Value);
                        variableFound++;
                        return variable.Value;
                    }
                    else
                    {
                        if (_debug)
                            _logger.Information("Match not found | {filePath} | {variableName}", filePath, variableName);

                        return match.Value;
                    }
                });
            }

            if (variableFound > 0)
                _fileSystem.File.WriteAllText(filePath, content);
            else if(_debug)
                _logger.Information("No Changed | {filePath}", filePath);

        }
    }
}
