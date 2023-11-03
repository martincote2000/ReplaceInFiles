

using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.IO.Abstractions;

namespace ReplaceInFiles.DependencyInjection
{
    public static class ReplaceInFileExtensions
    {

        public static IServiceCollection RegisterReplaceInFiles(this IServiceCollection services)
        {
            services.AddTransient<IFileReplacer, FileReplacer>()
                .AddTransient<IFolderSearcher, FolderSearcher>()
                .AddTransient<IFileSearcher, FileSearcher>()
                .AddTransient<IFileSystem, FileSystem>();

            return services;
        }
    }
}
