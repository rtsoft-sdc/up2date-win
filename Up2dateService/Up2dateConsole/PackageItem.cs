using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Up2dateConsole.Helpers;
using Up2dateConsole.ServiceReference;
using Up2dateConsole.ViewService;

namespace Up2dateConsole
{
    public class PackageItem : NotifyPropertyChanged
    {
        private readonly IViewService viewService;
        private bool isSelected;

        public PackageItem(Package package, IViewService viewService)
        {
            Package = package;
            this.viewService = viewService ?? throw new ArgumentNullException(nameof(viewService));
        }

        public Package Package { get; }

        public string Filename => Path.GetFileName(Package.Filepath);

        public PackageStatus PackageStatus => Package.Status;

        public string Status => ConvertToString(Package.Status);

        public string ExtraInfo => Package.Status == PackageStatus.SuggestedToInstall
                    ? viewService.GetText(Texts.SuggestedForInstallation)
                    : Package.Status == PackageStatus.ForcedWaitingForConfirmation
                    ? viewService.GetText(Texts.StronglyRecommended)
                    : ConvertToString(Package.ErrorCode);

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

        private Dictionary<InstallPackageResult, Texts> resultToText = new Dictionary<InstallPackageResult, Texts> {
            { InstallPackageResult.PackageNotSupported, Texts.PackageNotSupported },
            { InstallPackageResult.PackageUnavailable, Texts.PackageUnavailable },
            { InstallPackageResult.FailedToInstallChocoPackage, Texts.FailedToInstallChocoPackage },
            { InstallPackageResult.GeneralInstallationError, Texts.GeneralInstallationError },
            { InstallPackageResult.ChocoNotInstalled, Texts.ChocoNotInstalled },
            { InstallPackageResult.SignatureVerificationFailed, Texts.SignatureVerificationFailed },
            { InstallPackageResult.RestartNeeded, Texts.RestartNeeded },
            { InstallPackageResult.CannotStartInstaller, Texts.CannotStartInstaller }
        };

        private string ConvertToString(InstallPackageResult result)
        {
            return result == InstallPackageResult.Success
                ? null
                : viewService.GetText(resultToText.ContainsKey(result) ? resultToText[result] : Texts.InstallationErrorUnknown);
        }

        private Dictionary<PackageStatus, Texts> statusToText = new Dictionary<PackageStatus, Texts>
        {
                { PackageStatus.Unavailable, Texts.PackageStatusUnavailable },
                { PackageStatus.Available, Texts.PackageStatusAvailable },
                { PackageStatus.Downloading, Texts.PackageStatusDownloading },
                { PackageStatus.Downloaded, Texts.PackageStatusDownloaded },
                { PackageStatus.SuggestedToInstall, Texts.PackageStatusDownloaded },
                { PackageStatus.ForcedWaitingForConfirmation, Texts.PackageStatusDownloaded },
                { PackageStatus.Rejected, Texts.PackageStatusRejected },
                { PackageStatus.Installing, Texts.PackageStatusInstalling },
                { PackageStatus.Installed, Texts.PackageStatusInstalled },
                { PackageStatus.RestartNeeded, Texts.PackageStatusRestartNeeded },
                { PackageStatus.Failed, Texts.PackageStatusFailed }
        };

        private string ConvertToString(PackageStatus status)
        {
            return viewService.GetText(statusToText.ContainsKey(status) ? statusToText[status] : Texts.PackageStatusUnknown);
        }
    }
}
