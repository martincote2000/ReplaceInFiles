namespace OpsUtil.FileOperations
{
    public interface IFileSearcher
    {
        bool HasExtensionDefined();
        FileSearcher IgnoreFolderNames(params string[] folderNames);
        FileSearcher IncludeSubfolders(bool searchSubfolders);
        FileSearcher InDirectory(string directory);
        FileSearcher ParallelsExecution(int parallelsExecution);
        List<string> Search();
        FileSearcher WithExtensions(params string[] extensions);
    }
}