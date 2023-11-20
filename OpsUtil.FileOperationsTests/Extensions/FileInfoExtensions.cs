using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpsUtil.FileOperationsTests.Extensions
{
    public static class FileInfoExtensions
    {
        public static string ExtensionWithoutDot(this IFileInfo fileInfo)
        {
            if (fileInfo == null)
                throw new ArgumentNullException(nameof(fileInfo));

            return fileInfo.Extension.Substring(1);
        }

    }
}
