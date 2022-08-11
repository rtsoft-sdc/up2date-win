using System;
using System.Globalization;
using System.IO;
using Up2dateConsole.Helpers;
using Up2dateConsole.ServiceReference;

namespace Up2dateConsole
{
    public class PackageItem : NotifyPropertyChanged
    {
        private readonly Func<PackageStatus, string> statusToString;
        private bool isSelected;

        public PackageItem(Package package, Func<PackageStatus, string> statusToString)
        {
            Package = package;
            this.statusToString = statusToString ?? throw new ArgumentNullException(nameof(statusToString));
        }

        public Package Package { get; }

        public string Filename => Path.GetFileName(Package.Filepath);

        public string Status => statusToString(Package.Status);

        public string ProductName => string.IsNullOrWhiteSpace(Package.DisplayName) ? Package.ProductName : Package.DisplayName;

        public string Publisher => Package.Publisher;

        public string Version => Package.DisplayVersion;

        public string UrlInfoAbout => Package.UrlInfoAbout;

        public int? SizeMb => Package.EstimatedSize / 1024;

        public string InstallDate
        {
            get
            {
                if (Package.InstallDate == null) return null;

                return DateTime.TryParseExact(Package.InstallDate, "yyyyMMdd", null, DateTimeStyles.None, out DateTime date)
                    ? date.ToString("d")
                    : Package.InstallDate;
            }
        }

        public bool IsSelected
        {
            get => isSelected;
            set
            {
                if (isSelected == value) return;
                isSelected = value;
                OnPropertyChanged();
            }
        }
    }
}
