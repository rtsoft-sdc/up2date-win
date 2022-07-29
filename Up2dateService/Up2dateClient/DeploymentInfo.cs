namespace Up2dateClient
{
    public struct DeploymentInfo
    {
        public int id;
        public string updateType;
        public string downloadType;
        public bool isInMaintenanceWindow;
        public string chunkPart;
        public string chunkName;
        public string chunkVersion;
        public string artifactFileName;
        public string artifactFileHashMd5;
        public string artifactFileHashSha1;
        public string artifactFileHashSha256;
    }
}
