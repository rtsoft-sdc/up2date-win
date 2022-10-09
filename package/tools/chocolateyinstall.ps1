
$ErrorActionPreference = 'Stop';
$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$packageArgs = @{
  packageName   = $env:ChocolateyPackageName
  unzipLocation = $toolsDir
  fileType      = 'MSI'
  file           = Get-Item $toolsDir\Up2dateSetup32.msi
  file64           = Get-Item $toolsDir\Up2dateSetup64.msi

  softwareName  = 'RITMS UP2DATE Client'

  checksum      = 'ECBF4D47B610A6F83C2D07AE937BD8E6644B89839149FE833D93B61CAE804CC1'
  checksumType  = 'sha256'
  checksum64    = '65744F1E7910F73EC8B8E390E9D8908C8824F89996E717C403417B52628DF6BD'
  checksumType64= 'sha256'

  silentArgs    = "/qn /norestart /l*v `"$($env:TEMP)\$($packageName).$($env:chocolateyPackageVersion).MsiInstall.log`""
  validExitCodes= @(0, 3010, 1641)
}


[array]$key = Get-UninstallRegistryKey -SoftwareName $packageArgs['softwareName']

if ($key.Count -eq 1) {
	try { Stop-Process -Name "Up2dateConsole" } catch { echo "Console Was Not Closed" }  
	try { Stop-Service -Name "Up2dateService" } catch { echo "Service Was Not Stopped" }
	try { sc.exe delete Up2dateService } catch { echo "Service Was Not Deleted" }
}

Install-ChocolateyPackage @packageArgs






































