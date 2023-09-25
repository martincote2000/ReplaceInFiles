using EnsureThat;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ReplaceInFiles
{
    public class FileReplacer
    {
        private readonly ConcurrentQueue<string> _filesQueue;
        private int _parallelsReplacement;
        private string _startPattern;
        private string _endPattern;
        private readonly Dictionary<string, string> _variableDictionary;
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;

        public FileReplacer(ILogger logger, IFileSystem fileSystem)
        {
            _filesQueue = new ConcurrentQueue<string>();
            _parallelsReplacement = 5;
            _filesQueue = new ConcurrentQueue<string>();
            _variableDictionary = new Dictionary<string, string>();
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

        public FileReplacer MatchPattern(string startPattern, string endPattern)
        {
            Ensure.That(startPattern).IsNotEmptyOrWhiteSpace();
            Ensure.That(endPattern).IsNotEmptyOrWhiteSpace();

            _startPattern = startPattern;
            _endPattern = endPattern;

            return this;
        }

        public FileReplacer ReplaceVariable(string variableName, string replacementValue)
        {
            _variableDictionary.Add(variableName, replacementValue);
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

            if (_variableDictionary.Count == 0)
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

            var regexPattern = @"\" + _startPattern + "(.*?)" + _endPattern;
            //var regexPattern = @"\${(.*?)}";
            content = Regex.Replace(content, regexPattern, match =>
            {
                var variableName = match.Groups[1].Value;
                if (_variableDictionary.TryGetValue(variableName, out var replacement))
                {
                    _logger.Information("Changed | {filePath} | {variableName} for {replacement}", filePath, variableName, replacement);
                    variableFound++;
                    return replacement;
                }
                else
                {
                    return match.Value;
                }
            });

            if (variableFound > 0)
                _fileSystem.File.WriteAllText(filePath, content);
        }
    }
}
