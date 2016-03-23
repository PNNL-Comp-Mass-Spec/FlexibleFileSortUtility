== Flexible File Sort Utility ==

This program sorts a text file alphabetically (forward or reverse).
It supports both in-memory sorts for smaller files and use of temporary swap files for large files.
It can alternatively sort on a column in a tab-delimited or comma-separated file.
The column sort mode also supports numeric sorting.

Program syntax:
FlexibleFileSortUtility.exe
   /I:InputFilePath [/O:OutputFolderPath]
   [/R] [/Reverse] [/IgnoreCase] [/Header] [/KeepEmpty]
   [/Col:ColNumber] [/Delim:Delimiter] [/IsNumeric]
   [/MaxInMemory:MaxFileSizeMBInMemorySort]
   [/ChunkSize:ChunkSizeMB] [/Work:TempDirectoryPath]
   [/L:[LogFilePath]]

The Input file path is required

The Output folder path is optional; if not specified, the sorted file will be 
in the same folder as the input file, but will have _Sorted appended to its name.

If the output folder is different than the input file's folder, the output file 
name will match the input file name.

Use /R or /Reverse to specify a reverse sort

Use /IgnoreCase to disable case-sensitive sorting (ignored if /IsNumeric is used)

Use /Header to indicate that a header line is present and that line 
should be the first line of the output file.

Empty lines will be skipped by default; use /KeepEmpty to retain them.

Use /Col:Colnumber to specify a column to sort on, for example /Col:2 for the 2nd column in the file

When using /Col, use /Delim:Delimiter to specify a delimiter other than tab.
For example, for a CSV file use /Delimiter:,

Use /IsNumeric to specify that data in the sort column is numeric

Files less than 50 MB will be sorted in memory; override with /MaxInMemory

When sorting larger files, will parse the file to create smaller temporary files, 
then will merge those files together.  The merging will use 50 MB for sorting 
each temporary file; override with /ChunkSize

When sorting large files, or when replacing the source file, will create 
the temporary files in your temporary directory.
Use /Work to specify an alternate folder, for example /Work:C:\Temp

Use /L to create a log file.  Specify the name with /L:LogFilePath

-------------------------------------------------------------------------------
Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA) in 2015

E-mail: matthew.monroe@pnnl.gov or matt@alchemistmatt.com
Website: http://panomics.pnnl.gov/ or http://omics.pnl.gov or http://www.sysbio.org/resources/staff/   
-------------------------------------------------------------------------------

Licensed under the Apache License, Version 2.0; you may not use this file except 
in compliance with the License.  You may obtain a copy of the License at 
http://www.apache.org/licenses/LICENSE-2.0

Notice: This computer software was prepared by Battelle Memorial Institute, 
hereinafter the Contractor, under Contract No. DE-AC05-76RL0 1830 with the 
Department of Energy (DOE).  All rights in the computer software are reserved 
by DOE on behalf of the United States Government and the Contractor as 
provided in the Contract.  NEITHER THE GOVERNMENT NOR THE CONTRACTOR MAKES ANY 
WARRANTY, EXPRESS OR IMPLIED, OR ASSUMES ANY LIABILITY FOR THE USE OF THIS 
SOFTWARE.  This notice including this sentence must appear on any copies of 
this computer software.
