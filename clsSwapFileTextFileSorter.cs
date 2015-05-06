using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FlexibleFileSortUtility
{
    internal class SwapFileTextFileSorter
    {
        #region "Events, Delegates, and related variables"

        // PercentComplete ranges from 0 to 100, but can contain decimal percentage values
        public event clsProcessFilesOrFoldersBase.ProgressChangedEventHandler ProgressChanged;

        #endregion

        public bool ReverseSort { get; private set; }

        public DirectoryInfo WorkDirectory { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public SwapFileTextFileSorter(string workDirectoryPath, bool reverseSort)
        {
            if (string.IsNullOrWhiteSpace(workDirectoryPath))
                throw new ArgumentNullException(workDirectoryPath, "workDirectoryPath cannot be empty");

            WorkDirectory = new DirectoryInfo(workDirectoryPath);

            if (!WorkDirectory.Exists)
                WorkDirectory.Create();

            ReverseSort = reverseSort;
        }

        public IEnumerable<string> SplitInSortedChunks(string filepath, long chunkSize)
        {
            var dtLastProgress = DateTime.UtcNow;
            long linesRead = 0;
            long bytesRead = 0;
            var newLineLength = Environment.NewLine.Length;

            var buffer = new List<string>();

            using (var reader = new StreamReader(filepath))
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (line == null)
                        break;

                    linesRead++;
                    bytesRead += line.Length + newLineLength;
                    if (bytesRead >= chunkSize)
                    {
                        bytesRead = 0L;
                        yield return FlushBuffer(buffer);
                    }

                    buffer.Add(line);

                    linesRead++;
                    if (linesRead % 5000 == 0 && DateTime.UtcNow.Subtract(dtLastProgress).TotalSeconds >= 0.5)
                    {
                        dtLastProgress = StoreDataInUpdateProgress(reader.BaseStream.Length, bytesRead);                        
                    }
                }

            if (buffer.Any())
            {
                yield return FlushBuffer(buffer);
            }

        }

        public IEnumerable<string> SplitInSortedChunksSortOnColumn(
            string filepath, 
            long chunkSize, 
            int sortColumnToUse,             
            bool isNumeric,
            char delimiter)
        {
            var dtLastProgress = DateTime.UtcNow;
            long linesRead = 0;
            long bytesRead = 0;
            var newLineLength = Environment.NewLine.Length;

            var cachedData = new List<string>();
            var sortKeys = new List<string>();
            var sortKeysNumeric = new List<double>();

            using (var reader = new StreamReader(filepath))
                while (!reader.EndOfStream)
                {
                    var dataLine = reader.ReadLine();
                    if (dataLine == null)
                        break;

                    linesRead++;
                    bytesRead += dataLine.Length + newLineLength;
                    if (bytesRead >= chunkSize)
                    {
                        bytesRead = 0L;
                        if (isNumeric)
                            yield return FlushBuffer(cachedData, sortKeysNumeric);
                        else
                            yield return FlushBuffer(cachedData, sortKeys);
                    }

                    cachedData.Add(dataLine);

                    var dataColumns = dataLine.Split(delimiter);

                    if (isNumeric)
                    {
                        if (sortColumnToUse <= dataColumns.Length)
                        {
                            double value;
                            sortKeysNumeric.Add(double.TryParse(dataColumns[sortColumnToUse - 1], out value) ? value : 0);
                        }
                        else
                        {
                            sortKeysNumeric.Add(0);
                        }
                    }
                    else
                    {
                        if (sortColumnToUse <= dataColumns.Length)
                        {
                            sortKeys.Add(dataColumns[sortColumnToUse - 1]);
                        }
                        else
                        {
                            sortKeys.Add(string.Empty);
                        }
                    }

                    linesRead++;
                    if (linesRead % 5000 == 0 && DateTime.UtcNow.Subtract(dtLastProgress).TotalSeconds >= 0.5)
                    {
                        dtLastProgress = StoreDataInUpdateProgress(reader.BaseStream.Length, bytesRead);
                    }
                }

            if (cachedData.Any())
            {
                if (isNumeric)
                    yield return FlushBuffer(cachedData, sortKeysNumeric);
                else
                    yield return FlushBuffer(cachedData, sortKeys);
            }

        }

        private string FlushBuffer(List<string> cachedData)
        {
            cachedData.Sort(StringComparer.Ordinal);
            if (ReverseSort)
            {
                cachedData.Reverse();
            }

            var chunkFilePath = FlushBufferWriteData(cachedData);

            cachedData.Clear();
            return chunkFilePath;
        }

        private string FlushBuffer<T>(List<string> cachedData, List<T> sortKeys)
        {
            var data = cachedData.ToArray();
            var keys = sortKeys.ToArray();

            Array.Sort(keys, data);

            if (ReverseSort)
            {
                Array.Reverse(data);
            }

            var chunkFilePath = FlushBufferWriteData(data);

            cachedData.Clear();
            return chunkFilePath;
        }

        private string FlushBufferWriteData(IEnumerable<string> buffer)
        {
            var fiChunkFile = GetTempFile();

            using (var swOutFile = new StreamWriter(new FileStream(fiChunkFile.FullName, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite)))
            {
                foreach (var line in buffer)
                {
                    swOutFile.WriteLine(line);
                }
            }

            return fiChunkFile.FullName;
        }

        private FileInfo GetTempFile()
        {
            while (true)
            {
                var tempFileName = "SwapFile_";

                var oRand = new Random();
                for (var i = 0; i < 6; i++)
                {
                    char letter = (char)(oRand.Next(1, 26));
                    tempFileName += letter;
                }

                tempFileName += ".tmp";

                var fiTempFile = new FileInfo(Path.Combine(WorkDirectory.FullName, tempFileName));

                if (!fiTempFile.Exists)
                {
                    return fiTempFile;
                }

            }
        }

        private DateTime StoreDataInUpdateProgress(long fileBytesTotal, long bytesRead)
        {
            var percentComplete = bytesRead / (float)fileBytesTotal * 100;
            OnProgressChanged(new ProgressChangedEventArgs("Caching data to disk", percentComplete));
            return DateTime.UtcNow;
        }

        #region "Event Functions"

        public void OnProgressChanged(ProgressChangedEventArgs e)
        {
            if (ProgressChanged != null)
                ProgressChanged(this, e);
        }

        #endregion

    }
}
