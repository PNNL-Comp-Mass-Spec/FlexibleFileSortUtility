rem 19 seconds total runtime in 2018
rem 12 seconds total runtime in 2020

..\bin\FlexibleFileSortUtility.exe Rhiz_Hydro_TMT_E_07_23Apr15_TestFile.txt /O:TestFile_InMemory_Fwd /MaxInMemory:500 /Header
..\bin\FlexibleFileSortUtility.exe Rhiz_Hydro_TMT_E_07_23Apr15_TestFile.txt /O:TestFile_InMemory_Fwd_IgnoreCase /MaxInMemory:500 /Header /IgnoreCase
..\bin\FlexibleFileSortUtility.exe Rhiz_Hydro_TMT_E_07_23Apr15_TestFile.txt /O:TestFile_InMemory_Rev /MaxInMemory:500 /Header /Reverse

..\bin\FlexibleFileSortUtility.exe Rhiz_Hydro_TMT_E_07_23Apr15_TestFile.txt /O:TestFile_InMemory_Fwd_Col2 /MaxInMemory:500 /Header /Col:2
..\bin\FlexibleFileSortUtility.exe Rhiz_Hydro_TMT_E_07_23Apr15_TestFile.txt /O:TestFile_InMemory_Fwd_Col2_IgnoreCase /MaxInMemory:500 /Header /Col:2 /IgnoreCase
..\bin\FlexibleFileSortUtility.exe Rhiz_Hydro_TMT_E_07_23Apr15_TestFile.txt /O:TestFile_InMemory_Rev_Col2 /MaxInMemory:500 /Header /Col:2 /Reverse
..\bin\FlexibleFileSortUtility.exe Rhiz_Hydro_TMT_E_07_23Apr15_TestFile.txt /O:TestFile_InMemory_Rev_Col2_IgnoreCase /MaxInMemory:500 /Header /Col:2 /Reverse /IgnoreCase

..\bin\FlexibleFileSortUtility.exe Rhiz_Hydro_TMT_E_07_23Apr15_TestFile.txt /O:TestFile_InMemory_Fwd_Col3_Numeric /MaxInMemory:500 /Header /Col:3 /IsNumeric
..\bin\FlexibleFileSortUtility.exe Rhiz_Hydro_TMT_E_07_23Apr15_TestFile.txt /O:TestFile_InMemory_Rev_Col3_Numeric /MaxInMemory:500 /Header /Col:3 /IsNumeric /Reverse
