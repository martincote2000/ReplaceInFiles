using EnsureThat;
using System.Collections.Concurrent;
using System.IO.Abstractions;
using System.Xml.Linq;

namespace OpsUtil.FileOperations
{
    public class FolderSearcher : IFolderSearcher
    {
        private string _rootDirectory;
        private bool _searchSubfolders = true;
        private readonly List<string> _ignoredFolderNames;
        private readonly IFileSystem _fileSystem;
        private readonly ConcurrentQueue<string> _folderQueue;
        private int _parallelsExecution;

        public FolderSearcher(IFileSystem fileSystem)
        {
            _parallelsExecution = 5;
            _folderQueue = new ConcurrentQueue<string>();
            _rootDirectory = string.Empty;
            _fileSystem = fileSystem;
            _ignoredFolderNames = new List<string>();
        }

        public FolderSearcher InDirectory(string rootDirectory)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory))
                throw new ArgumentException($"Folder couldn't be null or empty", nameof(rootDirectory));

            _rootDirectory = rootDirectory;
            return this;
        }

        public FolderSearcher ParallelsExecution(int parallelsExecution)
        {
            Ensure.That(parallelsExecution).IsInRange(1, 10);
            _parallelsExecution = parallelsExecution;
            return this;
        }

        public FolderSearcher IncludeSubfolders(bool searchSubfolders)
        {
            _searchSubfolders = searchSubfolders;
            return this;
        }

        public FolderSearcher IgnoreFolderNames(params string[] folderNames)
        {
            foreach (var folderName in folderNames)
            {
                if (!string.IsNullOrWhiteSpace(folderName))
                    _ignoredFolderNames.Add(folderName);
            }
            return this;
        }
        public bool HasDirectoryDefined()
        {
            return !string.IsNullOrWhiteSpace(_rootDirectory);
        }

        private void EnsureRootFolderExists()
        {
            if (!_fileSystem.Directory.Exists(_rootDirectory))
                throw new DirectoryNotFoundException($"The {_rootDirectory} doesn't exists");
        }

        public List<string> Search()
        {
            EnsureRootFolderExists();

            List<string> filtredDirectories = new List<string>
            {
                _rootDirectory
            };

            SearchOption searchOption = _searchSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            var directories = _fileSystem.Directory.GetDirectories(_rootDirectory, "*", searchOption).ToList();
            directories.ForEach(d => _folderQueue.Enqueue(d));


            Parallel.ForEach(
                _folderQueue,
                new ParallelOptions { MaxDegreeOfParallelism = _parallelsExecution },
                directory =>
                {
                    IDirectoryInfo directoryInfo = _fileSystem.DirectoryInfo.New(directory);
                    if (!IsIgnoredFolder(directoryInfo) && !IsParentFolderContainsIgnoredFolders(directoryInfo))
                    {
                        filtredDirectories.Add(directory);
                    }

                    _folderQueue.TryDequeue(out string? dequeueValue);
                });
            return filtredDirectories;
        }

        private bool IsIgnoredFolder(IDirectoryInfo directoryInfo)
        {
            return _ignoredFolderNames.Any() &&
                _ignoredFolderNames.Exists(ignoreName => ignoreName.Equals(directoryInfo.Name, StringComparison.CurrentCultureIgnoreCase));
        }

        private bool IsParentFolderContainsIgnoredFolders(IDirectoryInfo directoryInfo)
        {
            if (!_ignoredFolderNames.Any() || directoryInfo == null)
                return false;

            IDirectoryInfo? parentDirectoryInfo = directoryInfo.Parent;

            while (parentDirectoryInfo != null)
            {
                if (IsIgnoredFolder(parentDirectoryInfo))
                {
                    return true;
                }
                parentDirectoryInfo = parentDirectoryInfo.Parent;
            }

            return false;
        }


    }
}
