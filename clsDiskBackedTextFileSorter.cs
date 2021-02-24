using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PRISM;
using PRISM.FileProcessor;

namespace FlexibleFileSortUtility
{
    internal class DiskBackedTextFileSorter : EventNotifier
    {
        #region "Events"

        /// <summary>Progress was reset</summary>
        public event ProcessFilesOrDirectoriesBase.ProgressResetEventHandler ProgressReset;

        #endregion

        #region "Constants"

        public const int DEFAULT_CHUNK_SIZE_MB = 50;

        #endregion

        #region "Properties"

        public int ChunkSizeMB
        {
            get => mChunkSizeMB;
            set
            {
                if (value < 1)
                    value = 1;

                mChunkSizeMB = value;
            }
        }

        /// <summary>
        /// Set to True if the first line in the data file is a header line
        /// </summary>
        /// <remarks>Defaults to True</remarks>
        public bool HasHeaderLine { get; set; }

        public bool IgnoreCase { get; set; }

        public bool KeepEmptyLines { get; set; }

        public bool KeepTempFiles { get; set; }

        public bool ReverseSort { get; set; }

        public DirectoryInfo WorkingDirectoryPath { get; }

        #endregion

        #region "Member variables

        protected int mChunkSizeMB;

        #endregion

        #region "Public methods"

        /// <summary>
        /// Constructor
        /// </summary>
        public DiskBackedTextFileSorter(string workDirectoryPath)
        {
            HasHeaderLine = false;
            IgnoreCase = false;
            KeepEmptyLines = false;
            KeepTempFiles = false;
            ReverseSort = false;

            if (string.IsNullOrWhiteSpace(workDirectoryPath))
                throw new ArgumentNullException(workDirectoryPath, "workDirectoryPath cannot be empty");

            WorkingDirectoryPath = new DirectoryInfo(workDirectoryPath);

            if (!WorkingDirectoryPath.Exists)
                WorkingDirectoryPath.Create();
        }

        public bool SortFile(FileInfo fiInputFile, FileInfo fiOutputFile)
        {
            return SortFile(fiInputFile, fiOutputFile, 0, false, '\t');
        }

        public bool SortFile(FileInfo fiInputFile, FileInfo fiOutputFile, int sortColumnToUse, bool sortColIsNumeric, char delimiter)
        {
            List<string> chunkFilePaths;

            // We open the writer file handle immediately to make sure we have write access to the output file
            using (var reader = new StreamReader(new FileStream(fiInputFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            using (var writer = new StreamWriter(new FileStream(fiOutputFile.FullName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
            {
                OnStatusEvent("Caching data to disk");

                string headerLine;
                long dataLinesTotal;

                if (sortColumnToUse > 0)
                    chunkFilePaths = SplitInSortedChunksSortOnColumn(reader, out headerLine, out dataLinesTotal, sortColumnToUse, sortColIsNumeric, delimiter);
                else
                {
                    chunkFilePaths = SplitInSortedChunks(reader, out headerLine, out dataLinesTotal);
                }

                if (sortColumnToUse < 1)
                {
                    MergeChunksPriorityQueue(dataLinesTotal, writer, headerLine, chunkFilePaths);
                }
                else
                {
                    MergeChunksSortedList(dataLinesTotal, writer, headerLine, chunkFilePaths.ToList(), sortColumnToUse, sortColIsNumeric, delimiter);
                }
            }

            if (!KeepTempFiles)
            {
                foreach (var chunkFile in chunkFilePaths)
                {
                    UtilityMethods.DeleteFileIgnoreErrors(chunkFile);
                }
            }

            return true;
        }

        #endregion

        #region "Private methods"

        private static void AddSortedListEntry(
           IDictionary<string, List<KeyValuePair<TextReader, string>>> lstNextKeyByFile,
           TextReader chunkReader,
           string dataLine)
        {
            AddSortedListEntry(lstNextKeyByFile, chunkReader, dataLine, dataLine);
        }

        private static void AddSortedListEntry(
            IDictionary<string, List<KeyValuePair<TextReader, string>>> lstNextKeyByFile,
            TextReader chunkReader,
            string dataLine,
            int sortColumnToUse,
            char delimiter)
        {
            var sortKey = GetSortKey(dataLine, sortColumnToUse, delimiter);

            AddSortedListEntry(lstNextKeyByFile, chunkReader, sortKey, dataLine);
        }

        private static void AddSortedListEntry(
           IDictionary<double, List<KeyValuePair<TextReader, string>>> lstNextKeyByFile,
           TextReader chunkReader,
           string dataLine,
           int sortColumnToUse,
           char delimiter)
        {
            var sortKey = GetSortKeyNumeric(dataLine, sortColumnToUse, delimiter);

            AddSortedListEntry(lstNextKeyByFile, chunkReader, sortKey, dataLine);
        }

        private static void AddSortedListEntry<T>(
            IDictionary<T, List<KeyValuePair<TextReader, string>>> lstNextKeyByFile,
            TextReader chunkReader,
            T sortKey,
            string dataLine)
        {
            if (lstNextKeyByFile.TryGetValue(sortKey, out var readers))
            {
                readers.Add(new KeyValuePair<TextReader, string>(chunkReader, dataLine));
            }
            else
            {
                readers = new List<KeyValuePair<TextReader, string>>
                    {
                        new KeyValuePair<TextReader, string>(chunkReader, dataLine)
                    };

                lstNextKeyByFile.Add(sortKey, readers);
            }
        }

        private static string GetSortKey(string dataLine, int sortColumnToUse, char delimiter)
        {
            var dataColumns = dataLine.Split(delimiter);

            if (sortColumnToUse <= dataColumns.Length)
            {
                return dataColumns[sortColumnToUse - 1];
            }

            return string.Empty;
        }

        private static double GetSortKeyNumeric(string dataLine, int sortColumnToUse, char delimiter)
        {
            var sortKey = GetSortKey(dataLine, sortColumnToUse, delimiter);

            if (double.TryParse(sortKey, out var value))
                return value;

            return 0;
        }

        private StreamWriter GetTempFile(int chunkNumber, out FileInfo fiChunkFile)
        {
            const int MAX_FAILURES = 3;
            const int MAX_RANDOM_NAME_ATTEMPTS = 255;

            var failureCount = 0;
            var namesTried = 0;

            while (true)
            {
                var tempFileName = "FileSortSwap" + chunkNumber.ToString("000") + "_";

                var oRand = new Random();
                for (var i = 0; i < 5; i++)
                {
                    var letter = (char)(oRand.Next(65, 90));
                    var number = (char)(oRand.Next(48, 57));

                    if (oRand.NextDouble() > 0.38)
                        tempFileName += letter;
                    else
                        tempFileName += number;
                }

                tempFileName += ".tmp";

                var fiTempFile = new FileInfo(Path.Combine(WorkingDirectoryPath.FullName, tempFileName));
                namesTried++;

                if (fiTempFile.Exists)
                {
                    if (namesTried > MAX_RANDOM_NAME_ATTEMPTS)
                    {
                        throw new Exception("Unable to create a new temp file after " + MAX_RANDOM_NAME_ATTEMPTS +
                                            " attempts (WorkDir is " + WorkingDirectoryPath + ")");
                    }
                    continue;
                }

                try
                {
                    var swOutFile = new StreamWriter(new FileStream(fiTempFile.FullName, FileMode.CreateNew, FileAccess.Write, FileShare.Read));
                    fiChunkFile = fiTempFile;
                    return swOutFile;
                }
                catch (Exception ex)
                {
                    failureCount += 1;
                    if (failureCount > MAX_FAILURES)
                    {
                        throw new IOException("Unable to create temp file " + fiTempFile.FullName, ex);
                    }
                }
            }
        }

        protected void MergeChunksSortedList(
            long dataLinesTotal,
            StreamWriter writer,
            string headerLine,
            IEnumerable<string> chunkFilePaths,
            int sortColumnToUse,
            bool sortColIsNumeric,
            char delimiter)
        {
            if (sortColumnToUse > 0 && sortColIsNumeric)
            {
                // The Keys in this sorted list are the next dataLine to write to the output file
                // The values are a List of readers in case more than one reader has the same dataLine for its next line
                var lstNextKeyByFile = new SortedList<double, List<KeyValuePair<TextReader, string>>>();
                MergeChunksSortedList(dataLinesTotal, writer, headerLine, chunkFilePaths, lstNextKeyByFile,
                                      sortColumnToUse, delimiter);
            }
            else
            {
                var lstNextKeyByFile = new SortedList<string, List<KeyValuePair<TextReader, string>>>(UtilityMethods.GetStringComparer(IgnoreCase));
                MergeChunksSortedList(dataLinesTotal, writer, headerLine, chunkFilePaths, lstNextKeyByFile,
                                      sortColumnToUse, delimiter);
            }
        }

        protected void MergeChunksSortedList(
            long dataLinesTotal,
            StreamWriter writer,
            string headerLine,
            IEnumerable<string> chunkFilePaths,
            SortedList<double, List<KeyValuePair<TextReader, string>>> lstNextKeyByFile,
            int sortColumnToUse,
            char delimiter)
        {
            try
            {
                if (sortColumnToUse < 1)
                    sortColumnToUse = 1;

                foreach (var chunkFile in chunkFilePaths)
                {
                    var chunkReader = new StreamReader(new FileStream(chunkFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

                    if (chunkReader.EndOfStream)
                    {
                        continue;
                    }

                    var dataLine = chunkReader.ReadLine();
                    if (dataLine == null)
                        continue;

                    // Note that sort keys are doubles in this function
                    AddSortedListEntry(lstNextKeyByFile, chunkReader, dataLine, sortColumnToUse, delimiter);
                }

                long linesWritten = 0;
                var dtLastProgress = DateTime.UtcNow;
                ResetProgress();

                if (!string.IsNullOrEmpty(headerLine))
                    writer.WriteLine(headerLine);

                while (lstNextKeyByFile.Count > 0)
                {
                    KeyValuePair<double, List<KeyValuePair<TextReader, string>>> nextItem;
                    if (ReverseSort)
                        nextItem = lstNextKeyByFile.Last();
                    else
                        nextItem = lstNextKeyByFile.First();

                    var sortKey = nextItem.Key;
                    var chunkReaders = nextItem.Value;

                    lstNextKeyByFile.Remove(sortKey);

                    foreach (var chunkReader in chunkReaders)
                    {
                        writer.WriteLine(chunkReader.Value);

                        var nextLine = chunkReader.Key.ReadLine();

                        if (nextLine != null)
                        {
                            // Note that sort keys are doubles in this function
                            AddSortedListEntry(lstNextKeyByFile, chunkReader.Key, nextLine, sortColumnToUse, delimiter);
                        }
                        else
                        {
                            chunkReader.Key.Dispose();
                        }
                    }

                    for (var i = 0; i < chunkReaders.Count; i++)
                    {
                        linesWritten++;
                        if (linesWritten % 50000 == 0 && DateTime.UtcNow.Subtract(dtLastProgress).TotalSeconds >= 0.5)
                        {
                            UpdateProgress("Writing to disk", linesWritten, dataLinesTotal);
                            dtLastProgress = DateTime.UtcNow;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error in MergeChunksSortedList (double-based): " + ex.Message, ex);
            }
        }

        protected void MergeChunksSortedList(
           long dataLinesTotal,
           StreamWriter writer,
           string headerLine,
           IEnumerable<string> chunkFilePaths,
           SortedList<string, List<KeyValuePair<TextReader, string>>> lstNextKeyByFile,
           int sortColumnToUse,
           char delimiter)
        {
            try
            {
                foreach (var chunkFile in chunkFilePaths)
                {
                    var chunkReader = new StreamReader(new FileStream(chunkFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

                    if (chunkReader.EndOfStream)
                    {
                        continue;
                    }

                    var dataLine = chunkReader.ReadLine();
                    if (dataLine == null)
                        continue;

                    if (sortColumnToUse > 0)
                        AddSortedListEntry(lstNextKeyByFile, chunkReader, dataLine, sortColumnToUse, delimiter);
                    else
                        AddSortedListEntry(lstNextKeyByFile, chunkReader, dataLine);
                }

                long linesWritten = 0;
                var dtLastProgress = DateTime.UtcNow;
                ResetProgress();

                if (!string.IsNullOrEmpty(headerLine))
                    writer.WriteLine(headerLine);

                while (lstNextKeyByFile.Count > 0)
                {
                    KeyValuePair<string, List<KeyValuePair<TextReader, string>>> nextItem;
                    if (ReverseSort)
                        nextItem = lstNextKeyByFile.Last();
                    else
                        nextItem = lstNextKeyByFile.First();

                    var sortKey = nextItem.Key;
                    var chunkReaders = nextItem.Value;

                    lstNextKeyByFile.Remove(sortKey);

                    foreach (var chunkReader in chunkReaders)
                    {
                        writer.WriteLine(chunkReader.Value);

                        var nextLine = chunkReader.Key.ReadLine();

                        if (nextLine != null)
                        {
                            if (sortColumnToUse > 0)
                                AddSortedListEntry(lstNextKeyByFile, chunkReader.Key, nextLine, sortColumnToUse, delimiter);
                            else
                                AddSortedListEntry(lstNextKeyByFile, chunkReader.Key, nextLine);
                        }
                        else
                        {
                            chunkReader.Key.Dispose();
                        }
                    }

                    for (var i = 0; i < chunkReaders.Count; i++)
                    {
                        linesWritten++;
                        if (linesWritten % 50000 == 0 && DateTime.UtcNow.Subtract(dtLastProgress).TotalSeconds >= 0.5)
                        {
                            UpdateProgress("Writing to disk", linesWritten, dataLinesTotal);
                            dtLastProgress = DateTime.UtcNow;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error in MergeChunksSortedList (string-based): " + ex.Message, ex);
            }
        }

        protected void MergeChunksPriorityQueue(
            long dataLinesTotal,
            StreamWriter writer,
            string headerLine,
            IEnumerable<string> chunkFilePaths)
        {
            try
            {
                var chunkReaders = chunkFilePaths
                    .Select(path => new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                    .Where(chunkReader => !chunkReader.EndOfStream)
                    .ToList();

                var comparer = ComparerClassFactory.GetComparer(ReverseSort, IgnoreCase);

                var queue = new PriorityQueue<KeyValuePair<string, TextReader>>(comparer);

                foreach (var chunkReader in chunkReaders)
                {
                    if (chunkReader.EndOfStream)
                    {
                        queue.Push(new KeyValuePair<string, TextReader>(null, chunkReader));
                    }
                    else
                    {
                        queue.Push(new KeyValuePair<string, TextReader>(chunkReader.ReadLine(), chunkReader));
                    }
                }

                long linesWritten = 0;
                var dtLastProgress = DateTime.UtcNow;
                ResetProgress();

                if (!string.IsNullOrEmpty(headerLine))
                    writer.WriteLine(headerLine);

                while (queue.Size > 0)
                {
                    var nextItem = queue.Pop();

                    var dataLine = nextItem.Key;
                    var chunkReader = nextItem.Value;

                    writer.WriteLine(dataLine);

                    var nextLine = chunkReader.ReadLine();
                    if (nextLine != null)
                    {
                        queue.Push(new KeyValuePair<string, TextReader>(nextLine, chunkReader));
                    }
                    else
                    {
                        chunkReader.Dispose();
                    }

                    linesWritten++;
                    if (linesWritten % 50000 == 0 && DateTime.UtcNow.Subtract(dtLastProgress).TotalSeconds >= 0.5)
                    {
                        UpdateProgress("Writing to disk", linesWritten, dataLinesTotal);
                        dtLastProgress = DateTime.UtcNow;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error in MergeChunksPriorityQueue: " + ex.Message, ex);
            }
        }

        protected void ResetProgress()
        {
            OnProgressReset(new EventArgs());
        }

        /// <summary>
        /// Splits file inputFilePath into files of size chunkSizeMB
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="headerLine"></param>
        /// <param name="dataLinesTotal"></param>
        /// <returns>The paths to the chunked files</returns>
        /// <remarks>Files are stored in the folder defined by WorkDirectory</remarks>
        protected List<string> SplitInSortedChunks(
            StreamReader reader,
            out string headerLine,
            out long dataLinesTotal)
        {
            try
            {
                headerLine = string.Empty;
                dataLinesTotal = 0;

                var chunkFilePaths = new List<string>();
                var chunkNumber = 1;
                long chunkSizeMB = ChunkSizeMB;

                var dtLastProgress = DateTime.UtcNow;
                long bytesRead = 0;
                long bytesReadTotal = 0;

                var newLineLength = Environment.NewLine.Length;

                var chunkSize = chunkSizeMB * 1024 * 1024;
                var cachedData = new List<string>();

                if (!reader.EndOfStream && HasHeaderLine)
                    headerLine = reader.ReadLine();

                while (!reader.EndOfStream)
                {
                    var dataLine = reader.ReadLine();
                    if (dataLine == null)
                        break;

                    dataLinesTotal++;
                    var addOn = dataLine.Length + newLineLength;
                    bytesRead += addOn;
                    bytesReadTotal += addOn;

                    if (bytesRead >= chunkSize)
                    {
                        chunkFilePaths.Add(WriteToChunk(ref chunkNumber, cachedData));
                        bytesRead = 0L;
                    }

                    cachedData.Add(dataLine);

                    dataLinesTotal++;
                    if (dataLinesTotal % 5000 == 0 && DateTime.UtcNow.Subtract(dtLastProgress).TotalSeconds >= 0.5)
                    {
                        dtLastProgress = UpdateProgress("Caching data to disk", bytesReadTotal, reader.BaseStream.Length);
                    }
                }

                if (cachedData.Any())
                {
                    chunkFilePaths.Add(WriteToChunk(ref chunkNumber, cachedData));
                }

                return chunkFilePaths;
            }
            catch (Exception ex)
            {
                throw new Exception("Error in SplitInSortedChunks: " + ex.Message, ex);
            }
        }

        protected List<string> SplitInSortedChunksSortOnColumn(
            StreamReader reader,
            out string headerLine,
            out long dataLinesTotal,
            int sortColumnToUse,
            bool isNumeric,
            char delimiter)
        {
            try
            {
                headerLine = string.Empty;
                dataLinesTotal = 0;

                var chunkFilePaths = new List<string>();
                var chunkNumber = 1;
                long chunkSizeMB = ChunkSizeMB;

                var dtLastProgress = DateTime.UtcNow;
                long bytesRead = 0;
                long bytesReadTotal = 0;

                var newLineLength = Environment.NewLine.Length;

                var chunkSize = chunkSizeMB * 1024 * 1024;
                var cachedData = new List<string>();
                var sortKeys = new List<string>();
                var sortKeysNumeric = new List<double>();

                if (!reader.EndOfStream && HasHeaderLine)
                    headerLine = reader.ReadLine();

                var numericComparer = Comparer<double>.Default;
                var stringComparer = UtilityMethods.GetStringComparer(IgnoreCase);

                while (!reader.EndOfStream)
                {
                    var dataLine = reader.ReadLine();
                    if (dataLine == null)
                        break;

                    dataLinesTotal++;
                    var addOn = dataLine.Length + newLineLength;
                    bytesRead += addOn;
                    bytesReadTotal += addOn;

                    if (bytesRead >= chunkSize)
                    {
                        if (isNumeric)
                            chunkFilePaths.Add(WriteToChunk(ref chunkNumber, cachedData, sortKeysNumeric, numericComparer));
                        else
                            chunkFilePaths.Add(WriteToChunk(ref chunkNumber, cachedData, sortKeys, stringComparer));

                        bytesRead = 0L;
                    }

                    cachedData.Add(dataLine);

                    if (isNumeric)
                        sortKeysNumeric.Add(GetSortKeyNumeric(dataLine, sortColumnToUse, delimiter));
                    else
                        sortKeys.Add(GetSortKey(dataLine, sortColumnToUse, delimiter));

                    dataLinesTotal++;
                    if (dataLinesTotal % 5000 == 0 && DateTime.UtcNow.Subtract(dtLastProgress).TotalSeconds >= 0.5)
                    {
                        dtLastProgress = UpdateProgress("Caching data to disk", bytesReadTotal, reader.BaseStream.Length);
                    }
                }

                if (cachedData.Any())
                {
                    if (isNumeric)
                        chunkFilePaths.Add(WriteToChunk(ref chunkNumber, cachedData, sortKeysNumeric, numericComparer));
                    else
                        chunkFilePaths.Add(WriteToChunk(ref chunkNumber, cachedData, sortKeys, stringComparer));
                }

                return chunkFilePaths;
            }
            catch (Exception ex)
            {
                throw new Exception("Error in SplitInSortedChunksSortOnColumn: " + ex.Message, ex);
            }
        }

        private DateTime UpdateProgress(string progressMessage, long bytesOrLinesProcessed, long bytesOrLinesTotal)
        {
            var percentComplete = bytesOrLinesProcessed / (float)bytesOrLinesTotal * 100;
            OnProgressUpdate(progressMessage, percentComplete);
            return DateTime.UtcNow;
        }

        private string WriteToChunk(ref int chunkNumber, List<string> cachedData)
        {
            WriteToChunkShowMessage(chunkNumber, cachedData.Count);

            cachedData.Sort(UtilityMethods.GetStringComparer(IgnoreCase));
            if (ReverseSort)
            {
                cachedData.Reverse();
            }

            var chunkFilePath = WriteToChunkWork(chunkNumber, cachedData);

            cachedData.Clear();
            chunkNumber++;

            return chunkFilePath;
        }

        private string WriteToChunk<T>(
            ref int chunkNumber,
            List<string> cachedData,
            List<T> sortKeys,
            IComparer<T> comparer)
        {
            WriteToChunkShowMessage(chunkNumber, cachedData.Count);

            var data = cachedData.ToArray();
            var keys = sortKeys.ToArray();

            Array.Sort(keys, data, comparer);

            if (ReverseSort)
            {
                Array.Reverse(data);
            }

            var chunkFilePath = WriteToChunkWork(chunkNumber, data);

            cachedData.Clear();
            sortKeys.Clear();
            chunkNumber++;

            return chunkFilePath;
        }

        private void WriteToChunkShowMessage(int chunkNumber, long dataLines)
        {
            OnStatusEvent("   sorting chunk " + chunkNumber + ": " + dataLines.ToString("#,##0") + " rows");
        }

        private string WriteToChunkWork(int chunkNumber, IEnumerable<string> buffer)
        {
            FileInfo fiChunkFile;
            using (var swOutFile = GetTempFile(chunkNumber, out fiChunkFile))
            {
                foreach (var line in buffer)
                {
                    if (!KeepEmptyLines && string.IsNullOrWhiteSpace(line))
                        continue;

                    swOutFile.WriteLine(line);
                }
            }

            return fiChunkFile.FullName;
        }

        #endregion

        #region "Event Functions"

        public void OnProgressReset(EventArgs e)
        {
            ProgressReset?.Invoke();
        }

        #endregion
    }

    internal class ComparerClassFactory
    {
        public static ComparerClassBase GetComparer(bool reverseSort, bool ignoreCase)
        {
            ComparerClassBase comparer;

            if (reverseSort)
            {
                if (ignoreCase)
                    comparer = new ComparerClassReverseIgnoreCase();
                else
                    comparer = new ComparerClassReverse();
            }
            else
            {
                if (ignoreCase)
                    comparer = new ComparerClassForwardIgnoreCase();
                else
                    comparer = new ComparerClassForward();
            }

            return comparer;
        }
    }

    internal abstract class ComparerClassBase : IComparer<KeyValuePair<string, TextReader>>
    {
        public abstract int Compare(KeyValuePair<string, TextReader> x, KeyValuePair<string, TextReader> y);
    }

    internal class ComparerClassForward : ComparerClassBase
    {
        public override int Compare(KeyValuePair<string, TextReader> x, KeyValuePair<string, TextReader> y)
        {
            return -string.CompareOrdinal(x.Key, y.Key);
        }
    }

    internal class ComparerClassReverse : ComparerClassBase
    {
        public override int Compare(KeyValuePair<string, TextReader> x, KeyValuePair<string, TextReader> y)
        {
            return string.CompareOrdinal(x.Key, y.Key);
        }
    }

    internal class ComparerClassForwardIgnoreCase : ComparerClassBase
    {
        public override int Compare(KeyValuePair<string, TextReader> x, KeyValuePair<string, TextReader> y)
        {
            return -string.Compare(x.Key, y.Key, StringComparison.OrdinalIgnoreCase);
        }
    }

    internal class ComparerClassReverseIgnoreCase : ComparerClassBase
    {
        public override int Compare(KeyValuePair<string, TextReader> x, KeyValuePair<string, TextReader> y)
        {
            return string.Compare(x.Key, y.Key, StringComparison.OrdinalIgnoreCase);
        }
    }
}
