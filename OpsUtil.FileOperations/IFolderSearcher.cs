namespace OpsUtil.FileOperations
{
    public interface IFolderSearcher
    {
        bool HasDirectoryDefined();
        FolderSearcher IgnoreFolderNames(params string[] folderNames);
        FolderSearcher IncludeSubfolders(bool searchSubfolders);
        FolderSearcher InDirectory(string rootDirectory);
        List<string> Search();
    }
}