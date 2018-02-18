using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace FlexibleFileSortUtility
{
    public class TextFileSorter : PRISM.FileProcessor.ProcessFilesBase
    {

        #region "Constants"

        public const int DEFAULT_IN_MEMORY_SORT_MAX_FILE_SIZE_MB = 50;
        public const int DEFAULT_CHUNK_SIZE_MB = DiskBackedTextFileSorter.DEFAULT_CHUNK_SIZE_MB;
        private const int MIN_ALLOWED_REPORTED_FREE_MEMORY_MB_AT_START = 30;

        #endregion

        #region "Properties"

        /// <summary>
        /// Delimiter to use when SortColumn is > 0
        /// </summary>
        public string ColumnDelimiter
        {
            get
            {
                return mColumnDelimiter;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    mColumnDelimiter = value;
            }
        }

        public int ChunkSizeMB
        {
            get
            {
                return mChunkSizeMB;
            }
            set
            {

                var chunkSizeThresholdMB = (int)(mFreeMemoryMBAtStart * 0.95);

                if (value > chunkSizeThresholdMB)
                    value = chunkSizeThresholdMB;

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

        /// <summary>
        /// Set to True to keep empty lines instead of discarding them
        /// </summary>
        public bool KeepEmptyLines { get; set; }

        /// <summary>
        /// Set to True to disable case-sensitive sorting
        /// </summary>
        public bool IgnoreCase { get; set; }

        /// <summary>
        /// Maximum file size to sort in-memory
        /// </summary>
        public int MaxFileSizeMBForInMemorySort
        {
            get
            {
                return mMaxFileSizeMBForInMemorySort;
            }
            set
            {

                var freeMemoryThresholdMB = (int)(mFreeMemoryMBAtStart * 0.8);

                if (value > freeMemoryThresholdMB)
                    value = freeMemoryThresholdMB;

                if (value < 10)
                    value = 10;

                mMaxFileSizeMBForInMemorySort = value;
            }
        }

        /// <summary>
        /// Sort in reverse
        /// </summary>
        public bool ReverseSort { get; set; }

        /// <summary>
        /// Column number to sort on (1st column is column 1, 2nd is column 2, etc.)
        /// </summary>
        public int SortColumn { get; set; }

        /// <summary>
        /// Set to true to treat the data in the SortColumn as numeric
        /// </summary>
        public bool SortColumnIsNumeric { get; set; }

        /// <summary>
        /// Folder path for temporary files
        /// </summary>
        public string WorkingDirectoryPath { get; set; }

        #endregion

        #region "Member variables

        private string mColumnDelimiter;
        private int mChunkSizeMB;
        private int mMaxFileSizeMBForInMemorySort;

        private bool mWarnedAvailablePhysicalMemoryError;
        private bool mWarnedPerformanceCounterError;
        private readonly PerformanceCounter mFreeMemoryPerformanceCounter;

        private readonly float mFreeMemoryMBAtStart;

        #endregion

        #region "Public methods"

        /// <summary>
        /// Constructor
        /// </summary>
        public TextFileSorter()
        {
            mFreeMemoryPerformanceCounter = new PerformanceCounter("Memory", "Available MBytes")
            {
                ReadOnly = true
            };

            mFreeMemoryMBAtStart = GetFreeMemoryMB();
            if (mFreeMemoryMBAtStart < MIN_ALLOWED_REPORTED_FREE_MEMORY_MB_AT_START)
                mFreeMemoryMBAtStart = MIN_ALLOWED_REPORTED_FREE_MEMORY_MB_AT_START;

            ResetToDefaults();

        }

        public override string GetErrorMessage()
        {
            return base.GetBaseClassErrorMessage();
        }
      
        public override bool ProcessFile(
            string inputFilePath,
            string outputFolderPath,
            string strParameterFilePath,
            bool blnResetErrorCode)
        {
            if (blnResetErrorCode)
                SetBaseClassErrorCode(eProcessFilesErrorCodes.NoError);

            try
            {
                if (string.IsNullOrWhiteSpace(inputFilePath))
                {
                    ShowErrorMessage("Input folder name is empty");
                    base.SetBaseClassErrorCode(eProcessFilesErrorCodes.InvalidInputFilePath);
                    return false;
                }

                if (!CleanupFilePaths(ref inputFilePath, ref outputFolderPath))
                {
                    base.SetBaseClassErrorCode(eProcessFilesErrorCodes.FilePathError);
                    return false;
                }

                try
                {

                    // Obtain the full path to the input file
                    var fiInputFile = new FileInfo(inputFilePath);

                    if (string.IsNullOrWhiteSpace(mOutputFolderPath))
                    {
                        mOutputFolderPath = fiInputFile.DirectoryName;
                    }

                    if (string.IsNullOrWhiteSpace(mOutputFolderPath))
                    {
                        ShowErrorMessage("Parent directory is null for the output folder: " + mOutputFolderPath);
                        base.SetBaseClassErrorCode(eProcessFilesErrorCodes.InvalidOutputFolderPath);
                        return false;
                    }

                    var diOutputFolder = new DirectoryInfo(mOutputFolderPath);
                    string outputFilePath;

                    if (string.Equals(fiInputFile.DirectoryName, diOutputFolder.FullName,
                                      StringComparison.CurrentCultureIgnoreCase))
                    {
                        outputFilePath = Path.Combine(mOutputFolderPath,
                            Path.GetFileNameWithoutExtension(fiInputFile.Name) + "_Sorted" + Path.GetExtension(fiInputFile.Name));
                    }
                    else
                    {
                        outputFilePath = Path.Combine(mOutputFolderPath, fiInputFile.Name);
                    }

                    var success = SortFile(inputFilePath, outputFilePath);
                    return success;

                }
                catch (Exception ex)
                {
                    HandleException("Error calling SortFile", ex);
                    return false;
                }
            }
            catch (Exception ex)
            {
                HandleException("Error in ProcessFile", ex);
                return false;
            }


        }

        public void ResetToDefaults()
        {
            mWarnedAvailablePhysicalMemoryError = false;
            mWarnedPerformanceCounterError = false;

            ChunkSizeMB = DEFAULT_CHUNK_SIZE_MB;
            MaxFileSizeMBForInMemorySort = DEFAULT_IN_MEMORY_SORT_MAX_FILE_SIZE_MB;

            ColumnDelimiter = "\t";
            HasHeaderLine = false;
            KeepEmptyLines = false;
            IgnoreCase = false;
            ReverseSort = false;
            SortColumn = 0;
            SortColumnIsNumeric = false;

            WorkingDirectoryPath = UtilityMethods.GetTempFolderPath();
        }

        public bool SortFile(string inputFilePath, string outputFilePath)
        {
            if (string.IsNullOrEmpty(inputFilePath))
            {
                ShowErrorMessage("Input file path cannot be blank: " + inputFilePath);
                return false;
            }

            if (string.IsNullOrEmpty(outputFilePath))
            {
                ShowErrorMessage("Output file path cannot be blank: " + inputFilePath);
                return false;
            }

            var fiInputFile = new FileInfo(inputFilePath);
            if (!fiInputFile.Exists)
            {
                ShowErrorMessage("Input file not found: " + inputFilePath);
                return false;
            }

            var fiOutputFile = new FileInfo(outputFilePath);
            if (fiOutputFile.Directory == null)
            {
                ShowErrorMessage("Parent directory for output file is null: " + outputFilePath);
                return false;
            }

            if (!fiOutputFile.Directory.Exists)
            {
                fiOutputFile.Directory.Create();
                return false;
            }

            var success = false;

            if (BytesToMB(fiInputFile.Length) <= mMaxFileSizeMBForInMemorySort)
            {
                success = SortFileInMemory(fiInputFile, fiOutputFile);
            }
            else
            {
                success = SortFileUseSwap(fiInputFile, fiOutputFile);
            }

            return success;
        }

        public bool SortFileInMemory(FileInfo fiInputFile, FileInfo fiOutputFile)
        {
            try
            {
                if (!fiInputFile.Exists)
                {
                    ShowErrorMessage("File not found: " + fiInputFile.FullName);
                    return false;
                }

                var inputFilePathOriginal = string.Copy(fiInputFile.FullName);

                char delimiter;
                int sortColumnToUse;

                var replaceFile = PrepareForSort(fiInputFile, ref fiOutputFile, out sortColumnToUse, out delimiter);

                SortFileInMemoryWork(fiInputFile, fiOutputFile, sortColumnToUse, delimiter);

                FinalizeFilesAfterSort(fiInputFile, fiOutputFile, replaceFile, inputFilePathOriginal);

                ShowMessage("Done");

                return true;
            }
            catch (Exception ex)
            {
                HandleException("Error in SortFileInMemory", ex);
                return false;
            }

        }

        public bool SortFileUseSwap(FileInfo fiInputFile, FileInfo fiOutputFile)
        {
            try
            {
                if (!fiInputFile.Exists)
                {
                    ShowErrorMessage("File not found: " + fiInputFile.FullName);
                    return false;
                }

                var inputFilePathOriginal = string.Copy(fiInputFile.FullName);

                char delimiter;
                int sortColumnToUse;

                var replaceFile = PrepareForSort(fiInputFile, ref fiOutputFile, out sortColumnToUse, out delimiter);

                var diskBackedFileSorter = new DiskBackedTextFileSorter(WorkingDirectoryPath)
                {
                    ChunkSizeMB = ChunkSizeMB,
                    HasHeaderLine = HasHeaderLine,
                    KeepEmptyLines = KeepEmptyLines,
                    IgnoreCase = IgnoreCase,
                    KeepTempFiles = false,
                    ReverseSort = ReverseSort                    
                };

                RegisterEvents(diskBackedFileSorter);
                diskBackedFileSorter.ProgressReset += DiskBackedFileSorter_ProgressReset;

                var success = diskBackedFileSorter.SortFile(fiInputFile, fiOutputFile, sortColumnToUse, SortColumnIsNumeric, delimiter);
                if (!success)
                {
                    ShowMessage("Call to DiskBackedTextFileSorter.SortFile returned false");
                    return false;
                }

                FinalizeFilesAfterSort(fiInputFile, fiOutputFile, replaceFile, inputFilePathOriginal);

                ShowMessage("Done");

                return true;
            }
            catch (Exception ex)
            {
                HandleException("Error in SortFileUseSwap", ex);
                return false;
            }

        }

        #endregion

        #region "Private methods"

        private double BytesToMB(long length)
        {
            return length / 1024.0 / 1024;
        }

        private void FinalizeFilesAfterSort(
            FileInfo fiInputFile, 
            FileInfo fiOutputFile, 
            bool replaceFile,
            string inputFilePathOriginal)
        {
            fiInputFile.Refresh();
            fiOutputFile.Refresh();

            if (replaceFile &&
                !string.Equals(fiInputFile.FullName, fiOutputFile.FullName, StringComparison.CurrentCultureIgnoreCase))
            {
                ShowMessage("Replacing file " + fiInputFile.FullName + " with " + fiOutputFile.FullName);

                fiInputFile.Delete();
                fiOutputFile.MoveTo(inputFilePathOriginal);
            }
        }

        private float GetFreeMemoryMB()
        {

            float freeMemoryMB = 0;

            try
            {

                int iterations = 0;
                freeMemoryMB = 0;
                while (freeMemoryMB < float.Epsilon && iterations <= 3)
                {
                    freeMemoryMB = mFreeMemoryPerformanceCounter.NextValue();
                    if (freeMemoryMB < float.Epsilon)
                    {
                        // You sometimes have to call .NextValue() several times before it returns a useful number
                        // Wait 1 second and then try again
                        Thread.Sleep(1000);
                    }
                    iterations += 1;
                }

            }
            catch (Exception ex)
            {
                // To avoid seeing this in the logs continually, we will only post this log message between 12 am and 12:30 am
                // A possible fix for this is to add the user who is running this process to the "Performance Monitor Users" group in "Local Users and Groups" on the machine showing this error.  
                // Alternatively, add the user to the "Administrators" group.
                // In either case, you will need to reboot the computer for the change to take effect
                if (!mWarnedPerformanceCounterError)
                {
                    mWarnedPerformanceCounterError = true;
                    ShowWarning("Error instantiating the Memory.[Available MBytes] performance counter: " + ex.Message);
                }
            }


            try
            {
                if (freeMemoryMB < float.Epsilon)
                {
                    // The Performance counters are still reporting a value of 0 for available memory; use an alternate method

                    freeMemoryMB = Convert.ToSingle(new Microsoft.VisualBasic.Devices.ComputerInfo().AvailablePhysicalMemory / 1024.0 / 1024.0);
                }

            }
            catch (Exception ex)
            {
                if (!mWarnedAvailablePhysicalMemoryError)
                {
                    mWarnedAvailablePhysicalMemoryError = true;
                    ShowWarning("Error determining available memory using Devices.ComputerInfo().AvailablePhysicalMemory: " + ex.Message);
                }
            }

            return freeMemoryMB;

        }

        private IComparer<string> GetCurrentStringComparer()
        {
            return UtilityMethods.GetStringComparer(IgnoreCase);
        }

        private bool PrepareForSort(
            FileInfo fiInputFile,
            ref FileInfo fiOutputFile,
            out int sortColumnToUse,
            out char delimiter)
        {
            ShowMessage("Sorting file " + fiInputFile.FullName);
            if (ReverseSort)
            {
                ShowMessage("SortMode=Reverse");
            }
            else
            {
                ShowMessage("SortMode=Forward");
            }

            delimiter = '\t';
            sortColumnToUse = SortColumn;

            if (sortColumnToUse > 0)
            {
                ShowMessage("SortColumn=" + sortColumnToUse);

                if (string.IsNullOrEmpty(mColumnDelimiter))
                {
                    mColumnDelimiter = "\t";
                }

                var delimiterText = "<Tab>";
                if (mColumnDelimiter[0] != '\t')
                {
                    delimiter = mColumnDelimiter[0];
                    delimiterText = mColumnDelimiter.Substring(0, 1);
                }

                ShowMessage("Delimiter=" + delimiterText);
            }

            var replaceFile = false;

            if (string.Equals(fiInputFile.FullName, fiOutputFile.FullName, StringComparison.CurrentCultureIgnoreCase))
            {
                // Input and output files are identical

                var outputFilePathToUse = Path.GetFileName(Path.GetTempFileName());

                if (!string.IsNullOrWhiteSpace(WorkingDirectoryPath))
                {
                    outputFilePathToUse = Path.Combine(WorkingDirectoryPath, outputFilePathToUse);
                }

                fiOutputFile = new FileInfo(outputFilePathToUse);
                replaceFile = true;
            }

            return replaceFile;
        }

        private string ReadDataLine(StreamReader reader, int newLineLength, List<string> cachedData, ref long bytesRead)
        {
            var dataLine = reader.ReadLine();

            if (string.IsNullOrEmpty(dataLine))
            {
                dataLine = string.Empty;
            }

            bytesRead += dataLine.Length + newLineLength;

            cachedData.Add(dataLine);

            return dataLine;
        }

        private void SortFileInMemoryWork(
            FileInfo fiInputFile, 
            FileInfo fiOutputFile, 
            int sortColumnToUse, 
            char delimiter)
        {
            var headerLine = string.Empty;

            // We open the writer file handle immediately to make sure we have write access to the output file
            using (var reader = new StreamReader(new FileStream(fiInputFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            using (var writer = new StreamWriter(new FileStream(fiOutputFile.FullName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
            {
                ShowMessage("Caching data in memory");

                if (!reader.EndOfStream && HasHeaderLine)
                {
                    headerLine = reader.ReadLine();
                }

                List<string> cachedData;

                if (sortColumnToUse < 1)
                {
                    cachedData = StoreDataInMemory(reader);
                    WriteInMemoryDataToDisk(writer, cachedData, headerLine);
                }
                else
                {
                    if (SortColumnIsNumeric)
                    {
                        List<double> sortKeysNumeric;
                        cachedData = StoreDataInMemory(reader, delimiter, sortColumnToUse, out sortKeysNumeric);
                        WriteInMemoryColumnDataToDisk(writer, ref cachedData, ref sortKeysNumeric, headerLine, Comparer<double>.Default);
                    }
                    else
                    {
                        List<string> sortKeys;
                        cachedData = StoreDataInMemory(reader, delimiter, sortColumnToUse, out sortKeys);
                        WriteInMemoryColumnDataToDisk(writer, ref cachedData, ref sortKeys, headerLine, GetCurrentStringComparer());
                    }
                }
            }
            
        }

        private List<string> StoreDataInMemory(StreamReader reader)
        {
            var dtLastProgress = DateTime.UtcNow;
            long linesRead = 0;
            long bytesRead = 0;
            var newLineLength = Environment.NewLine.Length;

            var cachedData = new List<string>();

            while (!reader.EndOfStream)
            {
                ReadDataLine(reader, newLineLength, cachedData, ref bytesRead);

                linesRead++;
                if (linesRead % 5000 == 0 && DateTime.UtcNow.Subtract(dtLastProgress).TotalSeconds >= 0.5)
                {
                    dtLastProgress = StoreDataInMemoryUpdateProgress(reader.BaseStream.Length, bytesRead);
                }
            }

            return cachedData;
        }

        private List<string> StoreDataInMemory(StreamReader reader, char delimiter, int sortColumnToUse, out List<string> sortKeys)
        {
            var dtLastProgress = DateTime.UtcNow;
            long linesRead = 0;
            long bytesRead = 0;
            var newLineLength = Environment.NewLine.Length;

            var cachedData = new List<string>();
            sortKeys = new List<string>();

            while (!reader.EndOfStream)
            {
                var dataLine = ReadDataLine(reader, newLineLength, cachedData, ref bytesRead);

                var dataColumns = dataLine.Split(delimiter);

                if (sortColumnToUse <= dataColumns.Length)
                {
                    sortKeys.Add(dataColumns[sortColumnToUse - 1]);
                }
                else
                {
                    sortKeys.Add(string.Empty);
                }

                linesRead++;
                if (linesRead % 5000 == 0 && DateTime.UtcNow.Subtract(dtLastProgress).TotalSeconds >= 0.5)
                {
                    dtLastProgress = StoreDataInMemoryUpdateProgress(reader.BaseStream.Length, bytesRead);
                }
            }

            return cachedData;
        }
      
        private List<string> StoreDataInMemory(StreamReader reader, char delimiter, int sortColumnToUse, out List<double> sortKeys)
        {
            var dtLastProgress = DateTime.UtcNow;
            long linesRead = 0;
            long bytesRead = 0;
            var newLineLength = Environment.NewLine.Length;

            var cachedData = new List<string>();
            sortKeys = new List<double>();

            while (!reader.EndOfStream)
            {
                var dataLine = ReadDataLine(reader, newLineLength, cachedData, ref bytesRead);

                var dataColumns = dataLine.Split(delimiter);

                if (sortColumnToUse <= dataColumns.Length)
                {
                    double value;
                    sortKeys.Add(double.TryParse(dataColumns[sortColumnToUse - 1], out value) ? value : 0);
                }
                else
                {
                    sortKeys.Add(0);
                }

                linesRead++;
                if (linesRead % 5000 == 0 && DateTime.UtcNow.Subtract(dtLastProgress).TotalSeconds >= 0.5)
                {
                    dtLastProgress = StoreDataInMemoryUpdateProgress(reader.BaseStream.Length, bytesRead);
                }
            }

            return cachedData;
        }

        private DateTime StoreDataInMemoryUpdateProgress(long fileBytesTotal, long bytesRead)
        {
            var percentComplete = bytesRead / (float)fileBytesTotal * 100;
            UpdateProgress("Caching data in memory", percentComplete);
            return DateTime.UtcNow;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="writer"></param>
        /// <param name="cachedData"></param>
        /// <param name="sortKeys"></param>
        /// <param name="headerLine"></param>
        /// <param name="comparer"></param>
        /// <remarks>cachedData and sortKeys are passed by ref because the memory is deallocated after the data is copied to array variables</remarks>
        private void WriteInMemoryColumnDataToDisk<T>(
            TextWriter writer, 
            ref List<string> cachedData, 
            ref List<T> sortKeys, 
            string headerLine,
            IComparer<T> comparer)
        {
            ShowMessage("Copying list data to arrays");

            var data = cachedData.ToArray();
            cachedData = null;

            var keys = sortKeys.ToArray();
            sortKeys = null;

            if (keys.Length > 100000)
            {
                GarbageCollectNow();
            }

            ShowMessage("Sorting " + data.Length.ToString("#,##0") + " rows");
            Array.Sort(keys, data, comparer);

            if (ReverseSort)
            {
                Array.Reverse(data);
            }

            ShowMessage("Writing to disk");

            if (HasHeaderLine)
                writer.WriteLine(headerLine);

            long linesWritten = 0;
            var dtLastProgress = DateTime.UtcNow;
            ResetProgress();

            foreach (var dataValue in data)
            {
                if (!KeepEmptyLines && string.IsNullOrWhiteSpace(dataValue))
                    continue;

                writer.WriteLine(dataValue);

                linesWritten++;
                if (linesWritten % 50000 == 0 && DateTime.UtcNow.Subtract(dtLastProgress).TotalSeconds >= 0.5)
                {
                    var percentComplete = linesWritten / (float)data.Length * 100;
                    UpdateProgress("Writing to disk", percentComplete);
                    dtLastProgress = DateTime.UtcNow;
                }
            }

        }

        private void WriteInMemoryDataToDisk(StreamWriter writer, List<string> cachedData, string headerLine)
        {
            ShowMessage("Sorting " + cachedData.Count.ToString("#,##0") + " rows");
            cachedData.Sort(GetCurrentStringComparer());

            if (ReverseSort)
            {
                cachedData.Reverse();
            }

            ShowMessage("Writing to disk");

            if (HasHeaderLine)
            {
                writer.WriteLine(headerLine);
            }

            foreach (var dataValue in cachedData)
            {
                if (!KeepEmptyLines && string.IsNullOrWhiteSpace(dataValue))
                    continue;

                writer.WriteLine(dataValue);
            }
        }

        #endregion


        #region "Event Handlers"

        private void DiskBackedFileSorter_ProgressReset()
        {
            ResetProgress();
        }

        #endregion

    }
}
