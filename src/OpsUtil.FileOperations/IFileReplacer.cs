namespace OpsUtil.FileOperations
{
    public interface IFileReplacer
    {
        FileReplacer ForFile(string filePath);
        FileReplacer ForFiles(List<string> files);
        FileReplacer MatchPattern(string startPattern, string endPattern);
        FileReplacer ParallelsExecution(int parallelsExecution);
        void Replace();
        FileReplacer ReplaceVariable(params string[] rawValues);
        FileReplacer ReplaceVariable(string variableName, string replacementValue);
        FileReplacer ReportFileChange(Action<string, ReplacementVariable> reportFileChange);
    }
}