
$ErrorActionPreference = 'Stop';
$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$packageArgs = @{
  packageName   = $env:ChocolateyPackageName
  unzipLocation = $toolsDir
  fileType      = 'MSI'
  file           = Get-Item $toolsDir\Up2dateSetup32.msi
  file64           = Get-Item $toolsDir\Up2dateSetup64.msi
  softwareName  = 'RITMS UP2DATE Client'
  silentArgs    = "/qn /norestart /l*v `"$($env:TEMP)\$($packageName).$($env:chocolateyPackageVersion).MsiInstall.log`""
  validExitCodes= @(0, 3010, 1641)
}


[array]$key = Get-UninstallRegistryKey -SoftwareName $packageArgs['softwareName']

if ($key.Count -eq 1) {
	$ErrorActionPreference = "Continue"
	Stop-Process -Name "Up2dateConsole"
	Stop-Service -Name "Up2dateService"
	sc.exe delete Up2dateService
	$ErrorActionPreference = "Stop"
}

Install-ChocolateyInstallPackage @packageArgs










































