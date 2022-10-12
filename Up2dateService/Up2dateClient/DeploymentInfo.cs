using System.Runtime.InteropServices;

namespace Up2dateClient
{
    [StructLayout(LayoutKind.Sequential)]
    public struct DeploymentInfo
    {
        public int id;
        public string updateType;
        public string downloadType;
        [MarshalAs(UnmanagedType.I1)] public bool isInMaintenanceWindow;
        public string chunkPart;
        public string chunkName;
        public string chunkVersion;
        public string artifactFileName;
        public string artifactFileHashMd5;
        public string artifactFileHashSha1;
        public string artifactFileHashSha256;
    }
}
