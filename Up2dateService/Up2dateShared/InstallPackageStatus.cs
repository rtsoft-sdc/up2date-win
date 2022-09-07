namespace Up2dateShared
{
    public enum InstallPackageResult
    {
        Success,
        PackageNotSupported,
        PackageUnavailable,
        FailedToInstallChocoPackage,
        GeneralInstallationError,
        ChocoNotInstalled,
        InstallationPackageIsNotSigned,
        InstallationPackageIsNotSignedBySelectedIssuer,
        RestartNeeded,
        CannotStartInstaller
    }
}
