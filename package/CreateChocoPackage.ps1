# Description: Script allows to generate choco packages
# Usage:
#   1. Put MSI Installers to tools\ directory (Up2dateSetup32.msi and Up2dateSetup64.msi)
#   2. Change version in up2date.nuspec (if needed)
#   2. Execute CreateChocoPackage.ps1 script
#   3. Done
$content=(Get-Content -path tools\chocolateyinstall.ps1 -Raw)       # Reading Content of tools\chocolateyinstall.ps1
$checksum=(checksum -t sha256 -f tools\Up2dateSetup32.msi)          # Calculating SHA256 checksum of 32bit installer (Up2dateSetup32.msi) 
$replaceTo="checksum      = '$checksum'"                            # Prepare line of script to change
$replaced=$content -replace 'checksum      = ''(.*)''',$replaceTo   # Replacing line "checksum      = '(.*)'" to block prepared in previous step
$checksum=(checksum -t sha256 -f tools\Up2dateSetup64.msi)          # Calculating SHA256 checksum of 64bit installer (Up2dateSetup64.msi) 
$content=$replaced
$replaceTo="checksum64    = '$checksum'"                            # Prepare line of script to change
$replaced=$content -replace 'checksum64    = ''(.*)''',$replaceTo   # Replacing line "checksum64      = '(.*)'" to block prepared in previous step
echo $replaced | Set-Content -Path tools\chocolateyinstall.ps1      # Replace content of tools\chocolateyinstall.ps1 to the prepared data
choco pack                                                          # Create Choco package