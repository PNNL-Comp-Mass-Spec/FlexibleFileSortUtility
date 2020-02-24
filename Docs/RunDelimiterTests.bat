

..\bin\FlexibleFileSortUtility.exe Rhiz_Hydro_TMT_E_07_23Apr15_TestFile.csv /O:TestFile_InMemory_Fwd                  /Delim:, /MaxInMemory:500 /Header
..\bin\FlexibleFileSortUtility.exe Rhiz_Hydro_TMT_E_07_23Apr15_TestFile.csv /O:TestFile_InMemory_Rev                  /Delim:, /MaxInMemory:500 /Header /Reverse
..\bin\FlexibleFileSortUtility.exe Rhiz_Hydro_TMT_E_07_23Apr15_TestFile.txt /O:TestFile_InMemory_Rev_Col2             /Delim:, /MaxInMemory:500 /Header /Col:2 /Reverse
..\bin\FlexibleFileSortUtility.exe Rhiz_Hydro_TMT_E_07_23Apr15_TestFile.txt /O:TestFile_InMemory_Rev_Col2_IgnoreCase  /Delim:, /MaxInMemory:500 /Header /Col:2 /Reverse /IgnoreCase
..\bin\FlexibleFileSortUtility.exe Rhiz_Hydro_TMT_E_07_23Apr15_TestFile.txt /O:TestFile_InMemory_Fwd_Col3_Numeric     /Delim:, /MaxInMemory:500 /Header /Col:3 /IsNumeric


..\bin\FlexibleFileSortUtility.exe Rhiz_Hydro_TMT_E_07_23Apr15_TestFile.csv /O:TestFile_SwapDisk_Fwd                  /Delim:, /MaxInMemory:1 /Header
..\bin\FlexibleFileSortUtility.exe Rhiz_Hydro_TMT_E_07_23Apr15_TestFile.csv /O:TestFile_SwapDisk_Rev                  /Delim:, /MaxInMemory:1 /Header /Reverse
..\bin\FlexibleFileSortUtility.exe Rhiz_Hydro_TMT_E_07_23Apr15_TestFile.txt /O:TestFile_SwapDisk_Rev_Col2             /Delim:, /MaxInMemory:1 /Header /Col:2 /Reverse
..\bin\FlexibleFileSortUtility.exe Rhiz_Hydro_TMT_E_07_23Apr15_TestFile.txt /O:TestFile_SwapDisk_Rev_Col2_IgnoreCase  /Delim:, /MaxInMemory:1 /Header /Col:2 /Reverse /IgnoreCase
..\bin\FlexibleFileSortUtility.exe Rhiz_Hydro_TMT_E_07_23Apr15_TestFile.txt /O:TestFile_SwapDisk_Fwd_Col3_Numeric     /Delim:, /MaxInMemory:1 /Header /Col:3 /IsNumeric
