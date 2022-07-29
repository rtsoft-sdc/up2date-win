using System;
using System.Runtime.Serialization;

namespace Up2dateShared
{
    [DataContract(Namespace = "http://RTSoft.Ritms.Up2date.win")]
    public struct SystemInfo
    {
        public static SystemInfo Retrieve()
        {
            return new SystemInfo
            {
                MachineName = Environment.MachineName,
                Is64Bit = Environment.Is64BitOperatingSystem,
                Version = Environment.OSVersion.Version,
                ServicePack = Environment.OSVersion.ServicePack,
                PlatformID = Environment.OSVersion.Platform,
                VersionString = Environment.OSVersion.VersionString
            };
        }

        [DataMember]
        public string MachineName { get; private set; }
        [DataMember]
        public bool Is64Bit { get; private set; }
        [DataMember]
        public Version Version { get; private set; }
        [DataMember]
        public string ServicePack { get; private set; }
        [DataMember]
        public PlatformID PlatformID { get; private set; }
        [DataMember]
        public string VersionString { get; private set; }
    }
}
