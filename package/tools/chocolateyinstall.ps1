
$ErrorActionPreference = 'Stop';
$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$packageArgs = @{
  packageName   = $env:ChocolateyPackageName
  unzipLocation = $toolsDir
  fileType      = 'MSI'
  file           = Get-Item $toolsDir\Up2dateSetup32.msi
  file64           = Get-Item $toolsDir\Up2dateSetup64.msi

  softwareName  = '*Up2date*'

  checksum      = '2CE42C49EDA71DA2311AAB9A773BD88FDD306FBF697F56ED243C0F930261E77E'
  checksumType  = 'sha256'
  checksum64    = '66E87B1D287293998AF7BAE927ADB27C975CAEF584060A87434D305A0DD70BCF'
  checksumType64= 'sha256'

  silentArgs    = "/qn /norestart /l*v `"$($env:TEMP)\$($packageName).$($env:chocolateyPackageVersion).MsiInstall.log`""
  validExitCodes= @(0, 3010, 1641)
}


[array]$key = Get-UninstallRegistryKey -SoftwareName $packageArgs['softwareName']

if ($key.Count -eq 1) {
  Stop-Service -Name "Up2dateService"
  sc.exe delete Up2dateService
}

Install-ChocolateyPackage @packageArgs

































