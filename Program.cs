using System;
using System.Collections.Generic;
using System.IO;
using FileProcessor;

namespace FlexibleFileSortUtility
{
    class Program
    {

        private const string PROGRAM_DATE = "May 5, 2015";

        private static string mInputFilePath;
        private static string mOutputFolderPath;

        private static bool mReverseSort;
        private static int mSortColumn;
        private static string mColumnDelimiter;
        private static bool mHasHeaderLine;
        private static bool mIsNumeric;

        private static bool mUseLogFile;
        private static string mLogFilePath;

        static int Main(string[] args)
        {
            var objParseCommandLine = new FileProcessor.clsParseCommandLine();

            mInputFilePath = string.Empty;
            mOutputFolderPath = string.Empty;

            mReverseSort = false;
            mSortColumn = 0;
            mColumnDelimiter = string.Empty;
            mHasHeaderLine = true;
            mIsNumeric = false;

            mUseLogFile = false;
            mLogFilePath = string.Empty;

            try
            {
                if (objParseCommandLine.ParseCommandLine())
                {
                    SetOptionsUsingCommandLineParameters(objParseCommandLine);
                }

                if (objParseCommandLine.NeedToShowHelp ||
                    (objParseCommandLine.ParameterCount + objParseCommandLine.NonSwitchParameterCount) == 0 ||
                    string.IsNullOrWhiteSpace(mInputFilePath))
                {
                    ShowProgramHelp();
                    return -1;
                }

                var sortUtility = new TextFileSorter
                {
                    ReverseSort = mReverseSort,
                    SortColumn = mSortColumn,
                    SortColumnIsNumeric = mIsNumeric,
                    ColumnDelimiter = mColumnDelimiter,
                    HasHeaderLine = mHasHeaderLine,
                    LogMessagesToFile = mUseLogFile,
                    LogFilePath = mLogFilePath
                };

                var success = sortUtility.ProcessFile(mInputFilePath, mOutputFolderPath);

                if (!success)
                    return -2;

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occurred in Program->Main: " + Environment.NewLine + ex.Message);
                Console.WriteLine(ex.StackTrace);
                return -1;
            }

            System.Threading.Thread.Sleep(1000);

            return 0;
        }

        private static string GetAppVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version + " (" + PROGRAM_DATE + ")";
        }

        private static bool ParseParameter(
            clsParseCommandLine objParseCommandLine,
            string parameterName,
            string description,
            ref string targetVariable)
        {
            string value;
            if (objParseCommandLine.RetrieveValueForParameter(parameterName, out value))
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

        private static bool SetOptionsUsingCommandLineParameters(FileProcessor.clsParseCommandLine objParseCommandLine)
        {
            // Returns True if no problems; otherwise, returns false

            string strValue = string.Empty;
            var lstValidParameters = new List<string> { "I", "O", "R", "Col", "Delim", "NoHeader", "IsNumeric", "L", "Log"};

            try
            {
                // Make sure no invalid parameters are present
                if (objParseCommandLine.InvalidParametersPresent(lstValidParameters))
                {
                    var badArguments = new List<string>();
                    foreach (string item in objParseCommandLine.InvalidParameters(lstValidParameters))
                    {
                        badArguments.Add("/" + item);
                    }

                    ShowErrorMessage("Invalid commmand line parameters", badArguments);

                    return false;
                }

                if (objParseCommandLine.NonSwitchParameterCount > 0)
                    mInputFilePath = objParseCommandLine.RetrieveNonSwitchParameter(0);

                if (!ParseParameter(objParseCommandLine, "I", "an input file name", ref mInputFilePath)) return false;
                if (!ParseParameter(objParseCommandLine, "O", "an output folder name", ref mOutputFolderPath)) return false;

                if (objParseCommandLine.IsParameterPresent("R"))
                {
                    mReverseSort = true;
                }

                strValue = string.Empty;
                if (!ParseParameter(objParseCommandLine, "Col", "a column number (the first column is column 1)", ref strValue)) return false;
                if (!string.IsNullOrWhiteSpace(strValue))
                {
                    if (!int.TryParse(strValue, out mSortColumn))
                    {
                        ShowErrorMessage("Invalid column number for /Col; '" + strValue + "' is not an integer");
                        return false;
                    }
                }

                strValue = string.Empty;
                if (!ParseParameter(objParseCommandLine, "Delim", "a delimiter", ref strValue)) return false;
                if (!string.IsNullOrWhiteSpace(strValue))
                {
                    mColumnDelimiter = string.Copy(strValue);
                }

                if (objParseCommandLine.IsParameterPresent("NoHeader"))
                {
                    mHasHeaderLine = false;
                }

                if (objParseCommandLine.IsParameterPresent("IsNumeric"))
                {
                    mIsNumeric = true;
                }

                if (objParseCommandLine.IsParameterPresent("L") || objParseCommandLine.IsParameterPresent("Log"))
                {
                    mUseLogFile = true;

                    if (objParseCommandLine.RetrieveValueForParameter("L", out strValue))
                        mLogFilePath = string.Copy(strValue);
                    else if (objParseCommandLine.RetrieveValueForParameter("Log", out strValue))
                        mLogFilePath = string.Copy(strValue);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error parsing the command line parameters: " + Environment.NewLine + ex.Message);
            }

            return false;
        }

        private static void ShowErrorMessage(string strMessage)
        {
            const string strSeparator = "------------------------------------------------------------------------------";

            Console.WriteLine();
            Console.WriteLine(strSeparator);
            Console.WriteLine(strMessage);
            Console.WriteLine(strSeparator);
            Console.WriteLine();

            WriteToErrorStream(strMessage);
        }

        private static void ShowErrorMessage(string strTitle, IEnumerable<string> items)
        {
            const string strSeparator = "------------------------------------------------------------------------------";

            Console.WriteLine();
            Console.WriteLine(strSeparator);
            Console.WriteLine(strTitle);
            string strMessage = strTitle + ":";

            foreach (string item in items)
            {
                Console.WriteLine("   " + item);
                strMessage += " " + item;
            }
            Console.WriteLine(strSeparator);
            Console.WriteLine();

            WriteToErrorStream(strMessage);
        }

        private static void ShowProgramHelp()
        {

            try
            {
                Console.WriteLine();
                Console.WriteLine(@"This program sorts a text file alphabetically (forward or reverse).  It supports both in-memory sorts for smaller files and use of temporary swap files for large files");
                Console.WriteLine(@"It can alternatively sort on a column in a tab-delimited or comma-separated file.  The column sort mode also supports numeric sorting.");
                Console.WriteLine();
                Console.WriteLine(@"Program syntax:" + Environment.NewLine + Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location));
                Console.WriteLine("   /I:InputFilePath [/O:OutputFolderPath] [/R] [/NoHeader] ");
                Console.WriteLine("   [/Col:ColNumber] [/Delim:Delimiter] [/IsNumeric]");
                Console.WriteLine("   [/MaxInMemory:MaxFileSizeInMemorySort]");
                Console.WriteLine("   [/Chunk:ChunkSizeMB] [/Work:TempDirectoryPath]");
                Console.WriteLine("   [/L:[LogFilePath]]");
                Console.WriteLine();
                Console.WriteLine("The Input file path is required");
                Console.WriteLine("The Output folder path is optional; if not specified, then the sorted file will be in the same folder as the input file, but will have _Sorted appended to its name");
                Console.WriteLine("Use /R to specify a reverse sort");
                Console.WriteLine();
                Console.WriteLine("The program assumes a header line is present and will not sort the first line in the file");
                Console.WriteLine("Use /NoHeader to sort the first line with the remaining lines");
                Console.WriteLine();
                Console.WriteLine("Use /Col:Colnumber to specify a column to sort on, for example /Col:2 for 2nd column in the file");
                Console.WriteLine("When using /Col, use /Delim:Delimiter to specify a delimiter other than tab.");
                Console.WriteLine("For example, for a CSV file use /Delimiter:,");
                Console.WriteLine("Use /IsNumeric to specify that data in the sort column is numeric");
                Console.WriteLine();
                Console.WriteLine("Files less than " + TextFileSorter.DEFAULT_IN_MEMORY_SORT_MAX_FILE_SIZE_MB + " MB will be sorted in memory; override with /MaxInMemory");
                Console.WriteLine();
                Console.WriteLine("When sorting larger files, will parse the file to create smaller temporary files, then will merge those files together.  " +
                                  "The merging will use " + TextFileSorter.DEFAULT_CHUNK_SIZE_MB + " MB for sorting each temporary file; override with /Chunk");
                Console.WriteLine();
                Console.WriteLine("When sorting large files, or when replacing the source file, will create the temporary files in folder " + TextFileSorter.GetTempFolder());
                Console.WriteLine("Use /Work to specify an alternate folder");
                Console.WriteLine();
                Console.WriteLine("Use /L to create a log file.  Specify the name with /L:LogFilePath");
                Console.WriteLine();
                Console.WriteLine("Program written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA) in 2015");
                Console.WriteLine("Version: " + GetAppVersion());
                Console.WriteLine();

                Console.WriteLine("E-mail: matthew.monroe@pnnl.gov or matt@alchemistmatt.com");
                Console.WriteLine("Website: http://panomics.pnnl.gov/ or http://omics.pnl.gov or http://www.sysbio.org/resources/staff/");
                Console.WriteLine();


                // Delay for 750 msec in case the user double clicked this file from within Windows Explorer (or started the program via a shortcut)
                System.Threading.Thread.Sleep(750);

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error displaying the program syntax: " + ex.Message);
            }

        }

        private static void WriteToErrorStream(string strErrorMessage)
        {
            try
            {
                using (var swErrorStream = new StreamWriter(Console.OpenStandardError()))
                {
                    swErrorStream.WriteLine(strErrorMessage);
                }
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch
            {
                // Ignore errors here
            }
        }
    }
}
