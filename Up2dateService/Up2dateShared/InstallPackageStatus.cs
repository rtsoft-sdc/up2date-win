namespace Up2dateShared
{
    public enum InstallPackageStatus
    {
        Ok,
        PackageUnavailable,
        TempDirectoryFail,
        DataCannotBeRead,
        FailedToInstallChocoPackage,
        GeneralChocoError,
        PsScriptInvokeError,
        ChocoNotInstalled,
        MsiInstallationError
    }
}