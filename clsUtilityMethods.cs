using System;
using System.Collections.Generic;
using System.IO;

namespace FlexibleFileSortUtility
{
    internal static class UtilityMethods
    {
        public static void DeleteFileIgnoreErrors(string filePath)
        {
            var targetFile = new FileInfo(filePath);
            DeleteFileIgnoreErrors(targetFile);
        }

        public static void DeleteFileIgnoreErrors(FileInfo targetFile)
        {
            try
            {
                if (targetFile.Exists)
                {
                    targetFile.Delete();
                }
            }
            catch (Exception)
            {
                // Ignore errors here
            }
        }

        public static string GetTempFolderPath()
        {
            try
            {
                var tempFile = new FileInfo(Path.GetTempFileName());
                DeleteFileIgnoreErrors(tempFile);
                return tempFile.DirectoryName;
            }
            catch (Exception)
            {
                return ".";
            }
        }

        public static IComparer<string> GetStringComparer(bool ignoreCase)
        {
            if (ignoreCase)
            {
                return StringComparer.OrdinalIgnoreCase;
            }

            return StringComparer.Ordinal;
        }
    }
}
