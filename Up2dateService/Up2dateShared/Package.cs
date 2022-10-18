using System.Runtime.Serialization;

namespace Up2dateShared
{
    [DataContract(Namespace = "http://RTSoft.Ritms.Up2date.win")]
    public struct Package
    {
        // informartion from MSI file
        [DataMember]
        public string Filepath { get; set; }
        [DataMember]
        public string ProductCode { get; set; }     // format as in registry, e.g.: {5673D71A-7C3A-3C2E-BF77-EA4890864EE4}
        [DataMember]
        public string ProductName { get; set; }     // name of product from MSI file

        // informartion from registry for installed package
        [DataMember]
        public string DisplayName { get; set; }     // name of installed package
        [DataMember]
        public string Publisher { get; set; }       // package publisher
        [DataMember]
        public string DisplayVersion { get; set; }  // vendor-provided string representation of product version
        [DataMember]
        public int? Version { get; set; }           // bytes: [3]-major, [2]-minor, [1][0]-revision
        [DataMember]
        public int? EstimatedSize { get; set; }     // in kilobytes
        [DataMember]
        public string InstallDate { get; set; }     // format: yyyymmdd (without any separators)
        [DataMember]
        public string UrlInfoAbout { get; set; }

        // current service state of the package
        [DataMember]
        public PackageStatus Status { get; set; }
        [DataMember]
        public InstallPackageResult ErrorCode { get; set; }
    }

    [DataContract(Namespace = "http://RTSoft.Ritms.Up2date.win")]
    public enum PackageStatus
    {
        [EnumMember]
        Unavailable,
        [EnumMember]
        Available,
        [EnumMember]
        Downloading,
        [EnumMember]
        Downloaded,
        [EnumMember]
        SuggestedToInstall,
        [EnumMember]
        WaitingForConfirmation,
        [EnumMember]
        Rejected,
        [EnumMember]
        Installing,
        [EnumMember]
        Installed,
        [EnumMember]
        RestartNeeded,
        [EnumMember]
        Failed
    }
}
