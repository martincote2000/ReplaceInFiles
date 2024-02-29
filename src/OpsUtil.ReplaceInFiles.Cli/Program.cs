// See https://aka.ms/new-console-template for more information
using FluentArgs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpsUtil.FileOperations;
using OpsUtil.FileOperations.DependencyInjection;
using Serilog;
using System.Diagnostics;

IConfiguration configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

ILogger logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

PrintSplashScreen(logger);


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
        .WithValidation(n => n >= 1 && n <= 10, "--parallelexecution should be between 1 & 10")
        .IsOptionalWithDefault(5)
    .ListParameter("--ignorefoldernames")
        .WithDescription("A list of folder names to ignore (ex: bin, obj, .git).")
        .WithValidation(n => !string.IsNullOrWhiteSpace(n), "A name must not only contain whitespace.")
        .IsOptional()
    .Parameter<string>("-p", "--parameters")
        .WithDescription("List of parameters to find and replace in files. Parameter in the file is ${...variable name...}.")
        .WithExamples("--parameters \"ParameterName1=MyValue1;ParameterName2=MyValue2;\"")
        .WithValidation(n => !string.IsNullOrWhiteSpace(n), "\"--parameters should not be empty")
        .IsRequired()
    .ListParameter("-e", "--extensions")
        .WithDescription("A list of file extensions.")
        .WithValidation(n => !string.IsNullOrWhiteSpace(n), "--extensions should not be empty")
        .IsRequired()
    .Parameter("-f", "--folder")
        .WithDescription("Folder to search files")
        .WithExamples("C:\\MyFolder")
        .IsRequired()
    .Call(folder => extensions => parameters => ignorefolderNames => parallelexecution => includeSubFolder => nopattern => ignorecase => verbose =>
    {
        logger.Information("=============== Parameters ===============");
        PrintParameter(logger, nameof(folder), folder);
        PrintParameter(logger, nameof(extensions), extensions);
        PrintParameter(logger, nameof(parameters), parameters);
        PrintParameter(logger, nameof(ignorefolderNames), ignorefolderNames);
        PrintParameter(logger, nameof(parallelexecution), parallelexecution);
        PrintParameter(logger, nameof(includeSubFolder), includeSubFolder);
        PrintParameter(logger, nameof(nopattern), nopattern);
        PrintParameter(logger, nameof(ignorecase), ignorecase);
        PrintParameter(logger, nameof(verbose), verbose);
        logger.Information("==============================");

        logger.Information("Searching files ...");

        var fileSearcher = builder.GetRequiredService<IFileSearcher>();

        List<string> filesFound = fileSearcher
            .InDirectory(folder)
            .WithExtensions(extensions?.ToArray())
            .ParallelsExecution(parallelexecution)
            .IncludeSubfolders(includeSubFolder)
            .IgnoreFolderNames(ignorefolderNames?.ToArray())
            .Search();

        logger.Information("Number of file founds: {0}", filesFound.Count);


        if (filesFound.Any())
        {
            logger.Information("Replacement starting ... ");


            var replacer = builder.GetService<IFileReplacer>();

            // Make sure to update the match pattern before adding parameters.
            if (nopattern)
            {
                replacer = replacer.MatchPattern(string.Empty, string.Empty);
            }

            replacer = replacer
                .ForFiles(filesFound)
                .ParallelsExecution(parallelexecution)
                .IgnoreCase(ignorecase)
                .ReportFileChange((filePath, replacementVariable) =>
                {
                    if (replacementVariable != null)
                        logger.Information("Changed | {filePath} | {variableName} for {replacement}", filePath, replacementVariable.Name, replacementVariable.Value);
                })
                .ReplaceVariable(parameters);


            var fileReplaced = replacer.Replace();
            
            if(!fileReplaced)
                logger.Information(">>>>> No file changed >>>>>");
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


void PrintSplashScreen(ILogger logger)
{
    var splashScreen = @"
     ___            _   _ _   _ _                                  
  / _ \ _ __  ___| | | | |_(_) |                                 
 | | | | '_ \/ __| | | | __| | |                                 
 | |_| | |_) \__ \ |_| | |_| | |                                 
  \___/| .__/|___/\___/ \__|_|_|   ___       _____ _ _           
 |  _ \|_|_ _ __ | | __ _  ___ ___|_ _|_ __ |  ___(_) | ___  ___ 
 | |_) / _ \ '_ \| |/ _` |/ __/ _ \| || '_ \| |_  | | |/ _ \/ __|
 |  _ <  __/ |_) | | (_| | (_|  __/| || | | |  _| | | |  __/\__ \
 |_| \_\___| .__/|_|\__,_|\___\___|___|_| |_|_|   |_|_|\___||___/
           |_|                                                   
";

    logger.Information(splashScreen);
}

void PrintParameter(ILogger logger, string parameterName, object value)
{
    if (value == null)
        return;

    if (value is IReadOnlyList<string> list)
    {
        var listValue = $"{parameterName}=";
        foreach (var valueItem in list)
        {
            listValue += $"{valueItem};";
        }
        logger.Information(listValue);
    }
    else
    {
        logger.Information($"{parameterName}={value}");
    }
}