$content=(Get-Content -path tools\chocolateyinstall.ps1 -Raw)
$checksum=(checksum -t sha256 -f tools\Up2dateSetup32.msi)
$replaceTo="checksum      = '$checksum'"
$replaced=$content -replace 'checksum      = ''(.*)''',$replaceTo
$checksum=(checksum -t sha256 -f tools\Up2dateSetup64.msi)
$content=$replaced
$replaceTo="checksum64    = '$checksum'"
$replaced=$content -replace 'checksum64    = ''(.*)''',$replaceTo
echo $replaced | Set-Content -Path tools\chocolateyinstall.ps1
choco pack