using EnsureThat;
using System.Collections.Concurrent;
using System.IO.Abstractions;

namespace OpsUtil.FileOperations
{
    public class FileSearcher : IFileSearcher
    {
        private readonly IFileSystem _fileSystem;
        private readonly List<string> _extensions;
        private readonly FolderSearcher _folderSearcher;
        private int _parallelsExecution;
        private readonly ConcurrentQueue<string> _folderQueue;

        public FileSearcher(IFileSystem fileSystem)
        {
            _parallelsExecution = 5;
            _fileSystem = fileSystem;
            _folderSearcher = new FolderSearcher(_fileSystem);
            _extensions = new List<string>();
            _folderQueue = new ConcurrentQueue<string>();
        }

        public FileSearcher InDirectory(string directory)
        {
            _folderSearcher.InDirectory(directory);
            return this;
        }

        public FileSearcher ParallelsExecution(int parallelsExecution)
        {
            Ensure.That(parallelsExecution).IsInRange(1, 10);
            _folderSearcher.ParallelsExecution(parallelsExecution);

            _parallelsExecution = parallelsExecution;
            return this;
        }

        public bool HasExtensionDefined()
        {
            return _extensions.Any();
        }

        public FileSearcher WithExtensions(params string[] extensions)
        {
            if (extensions == null || extensions.Length == 0)
                return this;

            foreach (string extension in extensions)
            {
                if (!string.IsNullOrWhiteSpace(extension))
                {
                    var cleanedExtension = CleanExtension(extension);
                    
                    if(!_extensions.Contains(cleanedExtension))
                        _extensions.Add(cleanedExtension);
                }
            }
            return this;
        }

        public FileSearcher IncludeSubfolders(bool searchSubfolders)
        {
            _folderSearcher.IncludeSubfolders(searchSubfolders);
            return this;
        }

        public FileSearcher IgnoreFolderNames(params string[] folderNames)
        {
            if (folderNames == null || folderNames.Length == 0)
                return this;

            _folderSearcher.IgnoreFolderNames(folderNames);
            return this;
        }

        public List<string> Search()
        {
            if (!_folderSearcher.HasDirectoryDefined())
#pragma warning disable S3928 // Parameter names used into ArgumentException constructors should match an existing one 
                throw new ArgumentException($"You should determine the directory before searching.", nameof(InDirectory));
#pragma warning restore S3928 // Parameter names used into ArgumentException constructors should match an existing one 

            var folders = _folderSearcher.Search();
            folders.ForEach(d => _folderQueue.Enqueue(d));

            var foundFiles = new List<string>();

            Parallel.ForEach(
                _folderQueue,
                new ParallelOptions { MaxDegreeOfParallelism = _parallelsExecution },
                folder =>
                {
                    var internalFileExtensions = _extensions;
                    
                    if (!internalFileExtensions.Any())
                        internalFileExtensions.Add(CleanExtension("*"));

                    foreach (var extension in internalFileExtensions)
                    {
                        var foundFilesInFolder = _fileSystem.Directory.GetFiles(folder, $"*.{extension}", SearchOption.TopDirectoryOnly).ToList();
                        foundFilesInFolder.ForEach((f) =>
                        {
                            if (!string.IsNullOrWhiteSpace(f))
                                foundFiles.Add(f);
                        });
                    }
                    _folderQueue.TryDequeue(out string? dequeueValue);
                });

            return foundFiles.Distinct().ToList();
        }


        private string CleanExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
                return extension;

            return extension.Replace("*.", "").Replace(".", "");
        }

    }

}
