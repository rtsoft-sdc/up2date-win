namespace Up2dateService.ErrorCodes
{
    public enum InstallChocoNupkgErrors
    {
        Ok,
        FailedToCreateDirectory,
        FailedToExtractNupkg,
        FailedToWriteDateToArchive,
        FailedToInstallNupkg
    }
}