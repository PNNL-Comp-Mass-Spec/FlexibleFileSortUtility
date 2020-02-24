rem 21 seconds total runtime in 2018
rem 13 seconds total runtime in 2020

..\bin\FlexibleFileSortUtility.exe Rhiz_Hydro_TMT_E_07_23Apr15_TestFile.txt /O:TestFile_SwapDisk_Fwd /MaxInMemory:1 /Header
..\bin\FlexibleFileSortUtility.exe Rhiz_Hydro_TMT_E_07_23Apr15_TestFile.txt /O:TestFile_SwapDisk_Fwd_IgnoreCase /MaxInMemory:1 /Header /IgnoreCase
..\bin\FlexibleFileSortUtility.exe Rhiz_Hydro_TMT_E_07_23Apr15_TestFile.txt /O:TestFile_SwapDisk_Rev /MaxInMemory:1 /Header /Reverse

..\bin\FlexibleFileSortUtility.exe Rhiz_Hydro_TMT_E_07_23Apr15_TestFile.txt /O:TestFile_SwapDisk_Fwd_Col2 /MaxInMemory:1 /Header /Col:2
..\bin\FlexibleFileSortUtility.exe Rhiz_Hydro_TMT_E_07_23Apr15_TestFile.txt /O:TestFile_SwapDisk_Fwd_Col2_IgnoreCase /MaxInMemory:1 /Header /Col:2 /IgnoreCase
..\bin\FlexibleFileSortUtility.exe Rhiz_Hydro_TMT_E_07_23Apr15_TestFile.txt /O:TestFile_SwapDisk_Rev_Col2 /MaxInMemory:1 /Header /Col:2 /Reverse
..\bin\FlexibleFileSortUtility.exe Rhiz_Hydro_TMT_E_07_23Apr15_TestFile.txt /O:TestFile_SwapDisk_Rev_Col2_IgnoreCase /MaxInMemory:1 /Header /Col:2 /Reverse /IgnoreCase

..\bin\FlexibleFileSortUtility.exe Rhiz_Hydro_TMT_E_07_23Apr15_TestFile.txt /O:TestFile_SwapDisk_Fwd_Col3_Numeric /MaxInMemory:1 /Header /Col:3 /IsNumeric
..\bin\FlexibleFileSortUtility.exe Rhiz_Hydro_TMT_E_07_23Apr15_TestFile.txt /O:TestFile_SwapDisk_Rev_Col3_Numeric /MaxInMemory:1 /Header /Col:3 /IsNumeric /Reverse
