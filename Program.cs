using System;
using System.Collections.Generic;
using System.IO;

namespace FlexibleFileSortUtility
{
    class Program
    {

        private const string PROGRAM_DATE = "May 12, 2015";

        private static string mInputFilePath;
        private static string mOutputFolderPath;

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
            var commandLineParser = new PRISM.clsParseCommandLine();

            mInputFilePath = string.Empty;
            mOutputFolderPath = string.Empty;

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
                    SetOptionsUsingCommandLineParameters(commandLineParser);
                }

                if (commandLineParser.NeedToShowHelp ||
                    (commandLineParser.ParameterCount + commandLineParser.NonSwitchParameterCount) == 0 ||
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

                var success = sortUtility.ProcessFile(mInputFilePath, mOutputFolderPath);

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
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version + " (" + PROGRAM_DATE + ")";
        }

        private static bool ParseParameter(
            PRISM.clsParseCommandLine commandLineParser,
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
            PRISM.clsParseCommandLine commandLineParser,
            string parameterName,
            string description,
            ref int targetVariable)
        {
            var strValue = string.Empty;
            if (!ParseParameter(commandLineParser, parameterName, description, ref strValue)) return false;

            if (string.IsNullOrWhiteSpace(strValue))
                return true;

            if (int.TryParse(strValue, out targetVariable))
                return true;

            ShowErrorMessage("Invalid valid for /" + parameterName + "; '" + strValue + "' is not an integer");
            return false;
        }

        private static bool SetOptionsUsingCommandLineParameters(PRISM.clsParseCommandLine commandLineParser)
        {
            // Returns True if no problems; otherwise, returns false

            var lstValidParameters = new List<string> { 
                "I", "O", "R", "Reverse", 
                "IgnoreCase", "Header", "KeepEmpty",
                "Col", "Delim", "IsNumeric", 
                "MaxInMemory", "ChunkSize", 
                "Work", "L", "Log" };

            try
            {
                // Make sure no invalid parameters are present
                if (commandLineParser.InvalidParametersPresent(lstValidParameters))
                {
                    var badArguments = new List<string>();
                    foreach (var item in commandLineParser.InvalidParameters(lstValidParameters))
                    {
                        badArguments.Add("/" + item);
                    }

                    ShowErrorMessage("Invalid commmand line parameters", badArguments);

                    return false;
                }

                if (commandLineParser.NonSwitchParameterCount > 0)
                    mInputFilePath = commandLineParser.RetrieveNonSwitchParameter(0);

                if (!ParseParameter(commandLineParser, "I", "an input file name", ref mInputFilePath)) return false;
                if (!ParseParameter(commandLineParser, "O", "an output folder name", ref mOutputFolderPath)) return false;

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

                var strValue = string.Empty;
                if (!ParseParameter(commandLineParser, "Delim", "a delimiter", ref strValue)) return false;
                if (!string.IsNullOrWhiteSpace(strValue))
                {
                    mColumnDelimiter = string.Copy(strValue);
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

                    if (commandLineParser.RetrieveValueForParameter("L", out strValue))
                        mLogFilePath = string.Copy(strValue);
                    else if (commandLineParser.RetrieveValueForParameter("Log", out strValue))
                        mLogFilePath = string.Copy(strValue);
                }

                return true;
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error parsing the command line parameters", ex);
            }

            return false;
        }

        private static void ShowErrorMessage(string message, Exception ex = null)
        {
            PRISM.ConsoleMsgUtils.ShowError(message, ex);
        }

        private static void ShowErrorMessage(string message, IEnumerable<string> items)
        {
            PRISM.ConsoleMsgUtils.ShowErrors(message, items);
        }

        private static void ShowProgramHelp()
        {

            try
            {
                Console.WriteLine();
                Console.WriteLine("This program sorts a text file alphabetically (forward or reverse).");
                Console.WriteLine("It supports both in-memory sorts for smaller files and use of temporary swap files for large files.");
                Console.WriteLine("It can alternatively sort on a column in a tab-delimited or comma-separated file.");
                Console.WriteLine("The column sort mode also supports numeric sorting.");
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
                Console.WriteLine("The Output folder path is optional; if not specified, the sorted file will be in the same folder as the input file, but will have _Sorted appended to its name");
                Console.WriteLine("If the output folder is different than the input file's folder, the output file name will match the input file name");
                Console.WriteLine();
                Console.WriteLine("Use /R or /Reverse to specify a reverse sort");
                Console.WriteLine("Use /IgnoreCase to disable case-sensitive sorting (ignored if /IsNumeric is used)");
                Console.WriteLine("Use /Header to indicate that a header line is present and that line should be the first line of the output file");
                Console.WriteLine("Empty lines will be skipped by default; use /KeepEmpty to retain them");
                Console.WriteLine();
                Console.WriteLine("Use /Col:Colnumber to specify a column to sort on, for example /Col:2 for the 2nd column in the file");
                Console.WriteLine("When using /Col, use /Delim:Delimiter to specify a delimiter other than tab.");
                Console.WriteLine("For example, for a CSV file use /Delimiter:,");
                Console.WriteLine("Use /IsNumeric to specify that data in the sort column is numeric");
                Console.WriteLine();
                Console.WriteLine("Files less than " + TextFileSorter.DEFAULT_IN_MEMORY_SORT_MAX_FILE_SIZE_MB + " MB will be sorted in memory; override with /MaxInMemory");
                Console.WriteLine();
                Console.WriteLine("When sorting larger files, will parse the file to create smaller temporary files, then will merge those files together.  " +
                                  "The merging will use " + TextFileSorter.DEFAULT_CHUNK_SIZE_MB + " MB for sorting each temporary file; override with /ChunkSize");
                Console.WriteLine();
                Console.WriteLine("When sorting large files, or when replacing the source file, will create the temporary files in folder " + UtilityMethods.GetTempFolderPath());
                Console.WriteLine(@"Use /Work to specify an alternate folder, for example /Work:C:\Temp");
                Console.WriteLine();
                Console.WriteLine("Use /L to create a log file.  Specify the name with /L:LogFilePath");
                Console.WriteLine();
                Console.WriteLine("Program written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA) in 2015");
                Console.WriteLine("Version: " + GetAppVersion());
                Console.WriteLine();

                Console.WriteLine("E-mail: matthew.monroe@pnnl.gov or matt@alchemistmatt.com");
                Console.WriteLine("Website: http://panomics.pnnl.gov/ or http://omics.pnl.gov or http://www.sysbio.org/resources/staff/");
                Console.WriteLine();


                // Delay for 1.5 seconds in case the user double clicked this file from within Windows Explorer (or started the program via a shortcut)
                System.Threading.Thread.Sleep(1500);

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
