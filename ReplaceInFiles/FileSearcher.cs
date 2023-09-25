using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplaceInFiles
{
    public class FileSearcher
    {
        private readonly IFileSystem _fileSystem;
        private readonly List<string> _extensions;
        private readonly FolderSearcher _folderSearcher;

        public FileSearcher(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
            _folderSearcher = new FolderSearcher(_fileSystem);
            _extensions = new List<string>();
        }
        public FileSearcher InDirectory(string directory)
        {
            _folderSearcher.InDirectory(directory);
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

            var cleanedExtensions = new List<string>();

            foreach (string extension in extensions)
            {
                if (!string.IsNullOrWhiteSpace(extension))
                {
                    var cleanedExtension = extension.Replace("*.", "").Replace(".", "");
                    cleanedExtensions.Add(cleanedExtension);
                }
            }

            _extensions.AddRange(cleanedExtensions);
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
            if(!_folderSearcher.HasDirectoryDefined())
#pragma warning disable S3928 // Parameter names used into ArgumentException constructors should match an existing one 
                throw new ArgumentException($"You should determine the directory before searching.", nameof(InDirectory));
#pragma warning restore S3928 // Parameter names used into ArgumentException constructors should match an existing one 


            var folders = _folderSearcher.Search();
            var foundFiles = new List<string>();

            foreach (var folder in folders)
            {
                foreach (var extension in _extensions)
                {
                    var foundFilesInFolder = _fileSystem.Directory.GetFiles(folder, $"*.{extension}", SearchOption.TopDirectoryOnly);
                    foundFiles.AddRange(foundFilesInFolder);
                }
            }

            return foundFiles;
        }

    }

}
