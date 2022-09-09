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
        SignatureVerificationFailed,
        RestartNeeded,
        CannotStartInstaller
    }
}
