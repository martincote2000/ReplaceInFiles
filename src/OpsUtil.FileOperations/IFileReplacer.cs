namespace OpsUtil.FileOperations
{
    public interface IFileReplacer
    {
        IFileReplacer ForFile(string filePath);
        IFileReplacer ForFiles(List<string> files);
        IFileReplacer MatchPattern(string startPattern, string endPattern);
        IFileReplacer ParallelsExecution(int parallelsExecution);        
        IFileReplacer ReplaceVariable(params string[] rawValues);
        IFileReplacer ReplaceVariable(string variableName, string replacementValue);
        IFileReplacer ReportFileChange(Action<string, ReplacementVariable> reportFileChange);
        bool Replace();
        IFileReplacer IgnoreCase(bool ignorecase);
    }
}