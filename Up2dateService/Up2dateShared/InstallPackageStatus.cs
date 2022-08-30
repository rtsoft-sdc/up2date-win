namespace Up2dateShared
{
    public enum InstallPackageStatus
    {
        Ok,
        PackageUnavailable,
        TempDirectoryFail,
        InvalidChocoPackage,
        FailedToInstallChocoPackage,
        GeneralChocoError,
        PsScriptInvokeError,
        ChocoNotInstalled,
        MsiInstallationError
    }
}
