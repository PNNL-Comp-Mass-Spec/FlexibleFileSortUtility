@echo off

xcopy FlexibleFileSortUtility.dll F:\Documents\Projects\DataMining\Protein_Digestion_Simulator\ProteinDigestionSimulator\bin\ /D /Y
xcopy FlexibleFileSortUtility.dll F:\Documents\Projects\DataMining\Protein_Digestion_Simulator\ProteinDigestionSimulator\bin\DLL\ /D /Y
xcopy FlexibleFileSortUtility.dll F:\Documents\Projects\DataMining\Protein_Digestion_Simulator\Lib\ /D /Y

xcopy FlexibleFileSortUtility.dll F:\Documents\Projects\KenAuberry\Organism_Database_Handler\Executables\Debug\ /D /Y
xcopy FlexibleFileSortUtility.dll F:\Documents\Projects\KenAuberry\Organism_Database_Handler\Executables\Release\ /D /Y
xcopy FlexibleFileSortUtility.dll F:\Documents\Projects\KenAuberry\Organism_Database_Handler\Protein_Uploader\bin\ /D /Y
xcopy FlexibleFileSortUtility.dll F:\Documents\Projects\KenAuberry\Organism_Database_Handler\Lib\ /D /Y
xcopy FlexibleFileSortUtility.dll F:\Documents\Projects\DataMining\Validate_Fasta_File\TestValidateFastaFileDLL\bin\ /D /Y

xcopy FlexibleFileSortUtility.dll F:\Documents\Projects\DataMining\DMS_Managers\Analysis_Manager\AM_Common\ /D /Y

xcopy FlexibleFileSortUtility.dll F:\Documents\Projects\DataMining\Validate_Fasta_File\Lib\ /D /Y
xcopy FlexibleFileSortUtility.dll F:\Documents\Projects\DataMining\Validate_Fasta_File\bin\ /D /Y
xcopy FlexibleFileSortUtility.dll F:\Documents\Projects\DataMining\Validate_Fasta_File\bin\DLL\ /D /Y

if not "%1"=="NoPause" pause
