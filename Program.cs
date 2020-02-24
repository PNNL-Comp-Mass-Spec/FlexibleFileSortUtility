using System;
using System.Collections.Generic;
using System.IO;
using PRISM;

namespace FlexibleFileSortUtility
{
    class Program
    {

        private const string PROGRAM_DATE = "February 24, 2020";

        private static string mInputFilePath;
        private static string mOutputDirectoryPath;

        private static bool mReverseSort;
        private static bool mIgnoreCase;
        private static bool mHasHeaderLine;
        private static bool mKeepEmptyLines;

        private static int mSortColumn;
        private static string mColumnDelimiter;
        private static bool mSortColumnIsNumeric;

        private static int mMaxFileSizeMBForInMemorySort;
        private static int mChunkSizeMB;

        private static string mWorkingDirectoryPath;
        private static bool mUseLogFile;
        private static string mLogFilePath;

        private static DateTime mLastProgressStatus;

        static int Main(string[] args)
        {
            var commandLineParser = new clsParseCommandLine();

            mInputFilePath = string.Empty;
            mOutputDirectoryPath = string.Empty;

            mReverseSort = false;
            mIgnoreCase = false;
            mHasHeaderLine = false;
            mKeepEmptyLines = false;

            mSortColumn = 0;
            mColumnDelimiter = string.Empty;
            mSortColumnIsNumeric = false;

            mMaxFileSizeMBForInMemorySort = TextFileSorter.DEFAULT_IN_MEMORY_SORT_MAX_FILE_SIZE_MB;
            mChunkSizeMB = TextFileSorter.DEFAULT_CHUNK_SIZE_MB;
            mWorkingDirectoryPath = UtilityMethods.GetTempFolderPath();

            mUseLogFile = false;
            mLogFilePath = string.Empty;

            mLastProgressStatus = DateTime.UtcNow;

            try
            {
                if (commandLineParser.ParseCommandLine())
                {
                    var parseSuccess = SetOptionsUsingCommandLineParameters(commandLineParser);
                    if (!parseSuccess)
                    {
                        System.Threading.Thread.Sleep(750);
                        return -1;
                    }
                }

                if (commandLineParser.NeedToShowHelp ||
                    commandLineParser.ParameterCount + commandLineParser.NonSwitchParameterCount == 0 ||
                    string.IsNullOrWhiteSpace(mInputFilePath))
                {
                    ShowProgramHelp();
                    return -1;
                }

                var sortUtility = new TextFileSorter
                {
                    ReverseSort = mReverseSort,
                    IgnoreCase = mIgnoreCase,
                    HasHeaderLine = mHasHeaderLine,
                    KeepEmptyLines = mKeepEmptyLines,
                    SortColumn = mSortColumn,
                    SortColumnIsNumeric = mSortColumnIsNumeric,
                    ColumnDelimiter = mColumnDelimiter,
                    MaxFileSizeMBForInMemorySort = mMaxFileSizeMBForInMemorySort,
                    ChunkSizeMB = mChunkSizeMB,
                    WorkingDirectoryPath = mWorkingDirectoryPath,
                    LogMessagesToFile = mUseLogFile,
                    LogFilePath = mLogFilePath
                };

                // Attach events
                sortUtility.StatusEvent += SortUtility_StatusEvent;
                sortUtility.ErrorEvent += SortUtility_ErrorEvent;
                sortUtility.WarningEvent += SortUtility_WarningEvent;
                sortUtility.ProgressUpdate += SortUtility_ProgressUpdate;

                var success = sortUtility.ProcessFile(mInputFilePath, mOutputDirectoryPath);

                if (!success)
                    return -2;

            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error occurred in Program->Main", ex);
                return -1;
            }

            return 0;
        }

        private static string GetAppVersion()
        {
            return PRISM.FileProcessor.ProcessFilesOrDirectoriesBase.GetAppVersion(PROGRAM_DATE);
        }

        private static bool ParseParameter(
            clsParseCommandLine commandLineParser,
            string parameterName,
            string description,
            ref string targetVariable)
        {
            if (commandLineParser.RetrieveValueForParameter(parameterName, out var value))
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    ShowErrorMessage("/" + parameterName + " does not have " + description);
                    return false;
                }
                targetVariable = string.Copy(value);
            }
            return true;
        }

        private static bool ParseParameterInt(
            clsParseCommandLine commandLineParser,
            string parameterName,
            string description,
            ref int targetVariable)
        {
            var value = string.Empty;
            if (!ParseParameter(commandLineParser, parameterName, description, ref value)) return false;

            if (string.IsNullOrWhiteSpace(value))
                return true;

            if (int.TryParse(value, out targetVariable))
                return true;

            ShowErrorMessage("Invalid valid for /" + parameterName + "; '" + value + "' is not an integer");
            return false;
        }

        private static bool SetOptionsUsingCommandLineParameters(clsParseCommandLine commandLineParser)
        {
            // Returns True if no problems; otherwise, returns false

            var validParameters = new List<string> {
                "I", "O", "R", "Reverse",
                "IgnoreCase", "Header", "KeepEmpty",
                "Col", "Delim", "IsNumeric",
                "MaxInMemory", "ChunkSize",
                "Work", "L", "Log" };

            try
            {
                // Make sure no invalid parameters are present
                if (commandLineParser.InvalidParametersPresent(validParameters))
                {
                    var badArguments = new List<string>();
                    foreach (var item in commandLineParser.InvalidParameters(validParameters))
                    {
                        badArguments.Add("/" + item);
                    }

                    ShowErrorMessage("Invalid command line parameters", badArguments);

                    return false;
                }

                if (commandLineParser.NonSwitchParameterCount > 0)
                    mInputFilePath = commandLineParser.RetrieveNonSwitchParameter(0);

                if (!ParseParameter(commandLineParser, "I", "an input file name", ref mInputFilePath)) return false;
                if (!ParseParameter(commandLineParser, "O", "an output folder name", ref mOutputDirectoryPath)) return false;

                if (commandLineParser.IsParameterPresent("R") ||
                    commandLineParser.IsParameterPresent("Reverse"))
                {
                    mReverseSort = true;
                }

                if (commandLineParser.IsParameterPresent("IgnoreCase"))
                {
                    mIgnoreCase = true;
                }

                if (commandLineParser.IsParameterPresent("Header"))
                {
                    mHasHeaderLine = true;
                }

                if (commandLineParser.IsParameterPresent("KeepEmpty"))
                {
                    mKeepEmptyLines = true;
                }


                if (!ParseParameterInt(commandLineParser, "Col", "a column number (the first column is column 1)", ref mSortColumn)) return false;

                var value = string.Empty;
                if (!ParseParameter(commandLineParser, "Delim", "a delimiter", ref value)) return false;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    mColumnDelimiter = string.Copy(value);
                }

                if (commandLineParser.IsParameterPresent("IsNumeric"))
                {
                    mSortColumnIsNumeric = true;
                }

                if (!ParseParameterInt(commandLineParser, "MaxInMemory", "a file size, in MB", ref mMaxFileSizeMBForInMemorySort)) return false;

                if (!ParseParameterInt(commandLineParser, "ChunkSize", "a memory size, in MB", ref mChunkSizeMB)) return false;

                if (!ParseParameter(commandLineParser, "Work", "a folder path", ref mWorkingDirectoryPath)) return false;

                if (commandLineParser.IsParameterPresent("L") || commandLineParser.IsParameterPresent("Log"))
                {
                    mUseLogFile = true;

                    if (commandLineParser.RetrieveValueForParameter("L", out value))
                        mLogFilePath = string.Copy(value);
                    else if (commandLineParser.RetrieveValueForParameter("Log", out value))
                        mLogFilePath = string.Copy(value);
                }

                return true;
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error parsing the command line parameters", ex);
                return false;
            }

        }

        private static void ShowErrorMessage(string message, Exception ex = null)
        {
            ConsoleMsgUtils.ShowError(message, ex);
        }

        private static void ShowErrorMessage(string message, IEnumerable<string> items)
        {
            ConsoleMsgUtils.ShowErrors(message, items);
        }

        private static void ShowProgramHelp()
        {

            try
            {
                Console.WriteLine();
                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                    "This program sorts a text file alphabetically (forward or reverse). " +
                    "It supports both in-memory sorts for smaller files and use of temporary swap files for large files. " +
                    "It can alternatively sort on a column in a tab-delimited or comma-separated file. " +
                    "The column sort mode also supports numeric sorting."));
                Console.WriteLine();
                Console.WriteLine("Program syntax:" + Environment.NewLine + Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location));
                Console.WriteLine("   /I:InputFilePath [/O:OutputFolderPath]");
                Console.WriteLine("   [/R] [/Reverse] [/IgnoreCase] [/Header] [/KeepEmpty]");
                Console.WriteLine("   [/Col:ColNumber] [/Delim:Delimiter] [/IsNumeric]");
                Console.WriteLine("   [/MaxInMemory:MaxFileSizeMBInMemorySort]");
                Console.WriteLine("   [/ChunkSize:ChunkSizeMB] [/Work:TempDirectoryPath]");
                Console.WriteLine("   [/L:[LogFilePath]]");
                Console.WriteLine();
                Console.WriteLine("The Input file path is required");
                Console.WriteLine();
                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                    "The Output folder path is optional; if not specified, the sorted file will be in the same folder as the input file, " +
                    "but will have _Sorted appended to its name"));
                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                    "If the output folder is different than the input file's folder, the output file name will match the input file name"));
                Console.WriteLine();
                Console.WriteLine("Use /R or /Reverse to specify a reverse sort");
                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                    "Use /IgnoreCase to disable case-sensitive sorting (ignored if /IsNumeric is used)"));
                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                    "Use /Header to indicate that a header line is present and that line should be the first line of the output file"));
                Console.WriteLine("Empty lines will be skipped by default; use /KeepEmpty to retain them");
                Console.WriteLine();
                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                    "Use /Col:ColNumber to specify a column to sort on, for example /Col:2 for the 2nd column in the file"));
                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                    "When using /Col, use /Delim:Delimiter to specify a delimiter other than tab. " +
                    "For example, for a CSV file use /Delimiter:,"));
                Console.WriteLine();
                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                    "Use /IsNumeric to specify that data in the sort column is numeric"));
                Console.WriteLine();
                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(string.Format(
                    "Files less than {0} MB will be sorted in memory; override with /MaxInMemory",
                    TextFileSorter.DEFAULT_IN_MEMORY_SORT_MAX_FILE_SIZE_MB)));
                Console.WriteLine();
                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(string.Format(
                    "When sorting larger files, will parse the file to create smaller temporary files, then will merge those files together. " +
                    "The merging will use {0} MB for sorting each temporary file; override with /ChunkSize",
                    TextFileSorter.DEFAULT_CHUNK_SIZE_MB)));
                Console.WriteLine();
                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                    "When sorting large files, or when replacing the source file, will create the temporary files in folder " +
                    UtilityMethods.GetTempFolderPath()));
                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                    @"Use /Work to specify an alternate folder, for example /Work:C:\Temp"));
                Console.WriteLine();
                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                    "Use /L to create a log file.  Specify the name with /L:LogFilePath"));
                Console.WriteLine();
                Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                    "Program written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA) in 2015"));
                Console.WriteLine("Version: " + GetAppVersion());
                Console.WriteLine();

                Console.WriteLine("E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov");
                Console.WriteLine("Website: https://omics.pnl.gov/ or https://panomics.pnnl.gov/");
                Console.WriteLine();

                System.Threading.Thread.Sleep(750);

            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error displaying the program syntax", ex);
            }

        }

        #region "Event Handlers"

        private static void SortUtility_ProgressUpdate(string progressMessage, float percentComplete)
        {
            if (DateTime.UtcNow.Subtract(mLastProgressStatus).TotalMilliseconds > 250)
            {
                mLastProgressStatus = DateTime.UtcNow;
                // Console.Write(".");
            }
        }

        private static void SortUtility_ErrorEvent(string message, Exception ex)
        {
            ShowErrorMessage(message, ex);
        }

        private static void SortUtility_StatusEvent(string message)
        {
            Console.WriteLine(message);
        }

        private static void SortUtility_WarningEvent(string message)
        {
            PRISM.ConsoleMsgUtils.ShowWarning(message);
        }


        #endregion
    }
}
