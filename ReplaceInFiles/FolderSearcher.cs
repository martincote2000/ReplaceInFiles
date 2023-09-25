using System.IO.Abstractions;
using System.Xml.Linq;

namespace ReplaceInFiles
{
    public class FolderSearcher
    {
        private string _rootDirectory;
        private bool _searchSubfolders = true;
        private readonly List<string> _ignoredFolderNames;
        private readonly IFileSystem _fileSystem;

        public FolderSearcher(IFileSystem fileSystem)
        {
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

        public FolderSearcher IncludeSubfolders(bool searchSubfolders)
        {
            _searchSubfolders = searchSubfolders;
            return this;
        }

        public FolderSearcher IgnoreFolderNames(params string[] folderNames)
        {
            foreach(var folderName in folderNames)
            {
                if(!string.IsNullOrWhiteSpace(folderName))
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

            List<string> filtredDirectories = new List<string>();
            filtredDirectories.Add(_rootDirectory);

            SearchOption searchOption = _searchSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            var directories = _fileSystem.Directory.GetDirectories(_rootDirectory, "*", searchOption);

            foreach (var folder in directories)
            {
                IDirectoryInfo directoryInfo = _fileSystem.DirectoryInfo.New(folder);
                if (!_ignoredFolderNames.Exists(folder => directoryInfo.Name.Equals(folder, StringComparison.OrdinalIgnoreCase)))
                    filtredDirectories.Add(folder);
            }

            return filtredDirectories;
        }
    }
}
