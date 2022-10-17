using Microsoft.Win32;
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
                VersionString = Environment.OSVersion.VersionString,
                MachineGuid = GetMachineGuid()
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
        [DataMember]
        public string MachineGuid { get; private set; }

        private static string GetMachineGuid()
        {
            const string keyName = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Cryptography";
            string machineGuid;
            try
            {
                machineGuid = (string)Registry.GetValue(keyName, "MachineGuid", string.Empty);
            }
            catch (Exception)
            {
                machineGuid = string.Empty;
            }
            return machineGuid;
        }
    }
}