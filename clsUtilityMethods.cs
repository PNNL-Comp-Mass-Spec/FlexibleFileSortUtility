using System;
using System.Collections.Generic;
using System.IO;

namespace FlexibleFileSortUtility
{
    internal class UtilityMethods
    {
        public static void DeleteFileIgnoreErrors(string filePath)
        {
            var fiFile = new FileInfo(filePath);
            DeleteFileIgnoreErrors(fiFile);
        }

        public static void DeleteFileIgnoreErrors(FileInfo fiTempFile)
        {
            try
            {
                if (fiTempFile.Exists)
                    fiTempFile.Delete();
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
                var fiTempFile = new FileInfo(Path.GetTempFileName());
                DeleteFileIgnoreErrors(fiTempFile);
                return fiTempFile.DirectoryName;
            }
            catch (Exception)
            {
                return ".";
            }
        }

        public static IComparer<string> GetStringComparer(bool ignoreCase)
        {
            if (ignoreCase)
                return StringComparer.OrdinalIgnoreCase;

            return StringComparer.Ordinal;
        }
    }
}
