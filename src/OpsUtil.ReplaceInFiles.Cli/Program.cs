// See https://aka.ms/new-console-template for more information
using FluentArgs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpsUtil.FileOperations;
using OpsUtil.FileOperations.DependencyInjection;
using Serilog;
using Spinnerino;
using System.Diagnostics;


IConfiguration configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

ILogger logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

var service = CreateServiceCollection(configuration, logger);
var builder = service.BuildServiceProvider();

Stopwatch stopwatch = Stopwatch.StartNew();

//Global catch all error
AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;



Log.Logger.Information("Replace in files starting .....");

FluentArgsBuilder.New()
    .DefaultConfigsWithAppDescription("Replace values or parameter ${} in files.")
    .Parameter<bool>("--verbose")
        .WithDescription("Verbose mode")
        .IsOptional()
    .Parameter<bool>("--ignorecase")
        .WithDescription("Indicate to find parameters using case-insensitive")
        .IsOptionalWithDefault(false)
    .Parameter<bool>("--nopattern")
        .WithDescription("Specify to search the exact string of the parameter name specified.")
        .WithExamples("http://localhost/api/myspecialApi=MyValue1    ")
        .IsOptional()
    .Parameter<bool>("--includesubfolder")
        .WithDescription("Include sub folder in the search")
        .IsOptionalWithDefault(true)
    .Parameter<int>("--parallelexecution")
        .WithDescription("Parallels replacement")
        .WithValidation(n => n >= 1 && n <= 10, "Should be between 1 & 10")
        .IsOptionalWithDefault(5)
    .ListParameter("--ignorefoldernames")
        .WithDescription("A list of folder names to ignore (ex: bin, obj, .git).")
        .WithValidation(n => !string.IsNullOrWhiteSpace(n), "A name must not only contain whitespace.")
        .IsOptional()
    .ListParameter("-p", "--parameters")
        .WithDescription("List of parameters to find and replace in files. Parameter in the file is ${...variable name...}.")
        .WithExamples("--parameters \"ParameterName1=MyValue1;ParameterName2=MyValue2;\"")
        .WithValidation(n => !string.IsNullOrWhiteSpace(n), "Parameter should not be empty")
        .IsRequired()
    .ListParameter("-e", "--extensions")
        .WithDescription("A list of file extensions.")
        .WithValidation(n => !string.IsNullOrWhiteSpace(n), "Name should not be empty")
        .IsRequired()
    .Parameter("-f", "--folder")
        .WithDescription("Folder to search files")
        .WithExamples("C:\\MyFolder")
        .IsRequired()
    .Call(folder => extensions => replaceparameters => ignorefolderNames => parallelexecution => searchInsubfolder => nopattern => ignorecase => verbose =>
    {
        logger.Information("Searching files ...");

        var fileSearcher = builder.GetRequiredService<IFileSearcher>();

        List<string> filesFound = fileSearcher
            .InDirectory(folder)
            .WithExtensions(extensions?.ToArray())
            .ParallelsExecution(parallelexecution)
            .IncludeSubfolders(searchInsubfolder)
            .IgnoreFolderNames(ignorefolderNames?.ToArray())
            .Search();

        logger.Information("Number of file founds: {0}", filesFound.Count);
        

        if (filesFound.Any())
        {
            logger.Information("Replacement starting ... ");

            using (var bar = new InlineProgressBar())
            {
                var reportEvery = ComputeReportEvery(filesFound.Count); 
                var replacer = builder.GetService<IFileReplacer>();

                replacer = replacer
                    .ForFiles(filesFound)
                    .ParallelsExecution(parallelexecution)
                    .IgnoreCase(ignorecase)
                    .ReportFileChange((filePath, replacementVariable) =>
                    {
                        if(verbose && replacementVariable != null)
                            logger.Information("Changed | {filePath} | {variableName} for {replacement}", filePath, replacementVariable.Name, replacementVariable.Value);
                    })
                    .ReportProgress(reportEvery, (fileCount, fileProcessed) =>
                    {
                        var progressPercentage = Math.Round((fileProcessed / fileCount) * 100);                    
                        bar.SetProgress(progressPercentage);
                    })
                    .ReplaceVariable(replaceparameters?.ToArray());

                if (nopattern)
                {
                    replacer.MatchPattern(string.Empty, string.Empty);
                }

                replacer.Replace();
            }
        }

    })
    .Parse(args);

logger.Information("Execution time {0}s", stopwatch.Elapsed.TotalSeconds);


int ComputeReportEvery(int fileCount)
{
    // Report every 5% of file count found.
    double reportEvery = Math.Ceiling(fileCount * 0.05);
    if (reportEvery <= 0)
        reportEvery = 1;

    return Convert.ToInt32(reportEvery);
}

void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
{
    var exception = e.ExceptionObject as Exception;
    logger.Error(exception, exception.Message);
}


IServiceCollection CreateServiceCollection(IConfiguration configuration, ILogger logger)
{
    var builder = new ServiceCollection()
        .AddSingleton(configuration)
        .AddSingleton(logger)
        .RegisterReplaceInFiles();

    return builder;
}