namespace Up2dateShared
{
    public enum InstallPackageResult
    {
        Success,
        PackageUnavailable,
        FailedToInstallChocoPackage,
        GeneralInstallationError,
        ChocoNotInstalled,
        RestartNeeded
    }
}
