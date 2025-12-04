using System;
using System.IO;

namespace TextAnalyzer.Helpers
{
    internal class AppStorageHelper
    {
        static internal string GetAppArchiveDir()
        {
            var parentDir= Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);

            var appArchiveDir = Path.Combine(parentDir, "TextAnalyzer");
            if (!Directory.Exists(appArchiveDir))
                Directory.CreateDirectory(appArchiveDir);

            return appArchiveDir;
        }
    }
}
