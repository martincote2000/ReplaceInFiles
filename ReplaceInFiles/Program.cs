// See https://aka.ms/new-console-template for more information
using FluentArgs;
using ReplaceInFiles;
using Serilog;
using System.IO.Abstractions;

Log.Logger = new LoggerConfiguration()                    
                    .WriteTo.Console()
                    .CreateLogger();

FluentArgsBuilder.New()    
    .Parameter<bool>("-sf", "--subfolder")
        .WithDescription("Folder to search")
        .WithExamples("C:\\MyFolder")
        .IsOptionalWithDefault(true)
    .Parameter<int>("-rep", "--parallelsreplacement")
        .WithDescription("Parallels replacement")        
        .WithValidation(n => n >= 1 && n <= 10, "Should be between 1 & 10")
        .IsOptionalWithDefault(5)
    .ListParameter("--ignorefolderNames")
        .WithDescription("A list of folder names to ignore (ex: bin, obj, .git).")
        .WithValidation(n => !string.IsNullOrWhiteSpace(n), "A name must not only contain whitespace.")
        .IsRequired()    
    .ListParameter("-p", "--replaceparameters")
        .WithDescription("List of parameter to replace in files. Parameter in the file is ${...variable name...}.")
        .WithExamples("--replaceparameters \"ParameterName1=MyValue1;ParameterName2=MyValue2;\"")
        .WithValidation(n => !string.IsNullOrWhiteSpace(n), "A name must not only contain whitespace.")
        .IsRequired()
    .ListParameter("-e", "--extensions")
        .WithDescription("A list of file extensions.")
        .WithValidation(n => !string.IsNullOrWhiteSpace(n), "A name must not only contain whitespace.")
        .IsRequired()
    .Parameter("-f", "--folder")
        .WithDescription("Folder to search files")
        .WithExamples("C:\\MyFolder")
        .IsRequired()
    .Call(folder => extensions => replaceparameters => ignorefolderNames => parallelsreplacement => searchInsubfolder =>
    {
        IFileSystem fileSystem = new FileSystem();

        var filesFound = new FileSearcher(fileSystem)
            .InDirectory(folder)
            .WithExtensions(extensions?.ToArray())
            .IncludeSubfolders(searchInsubfolder)
            .IgnoreFolderNames(ignorefolderNames?.ToArray())
            .Search();

        new FileReplacer(Log.Logger, fileSystem)
            .ForFiles(filesFound)
            .ParallelsReplacement(parallelsreplacement)
            .ReplaceVariable(replaceparameters?.ToArray())
            .Replace();
    })
    .Parse(args);