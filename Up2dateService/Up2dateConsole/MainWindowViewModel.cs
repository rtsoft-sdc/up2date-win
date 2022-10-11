using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using Up2dateConsole.Dialogs;
using Up2dateConsole.Helpers;
using Up2dateConsole.ServiceReference;
using Up2dateConsole.StateIndicator;
using Up2dateConsole.ViewService;

namespace Up2dateConsole
{
    public class MainWindowViewModel : NotifyPropertyChanged
    {
        private const int InitialDelay = 1000; // milliseconds
        private const int RefreshInterval = 20000; // milliseconds

        private static bool firstTimeRefresh = true;

        private string msiFolder;
        private bool operationInProgress;
        private ServiceState serviceState;
        private string deviceId;
        private readonly Timer timer = new Timer(InitialDelay);
        private readonly IViewService viewService;
        private readonly IWcfClientFactory wcfClientFactory;

        public MainWindowViewModel(IViewService viewService, IWcfClientFactory wcfClientFactory)
        {
            this.viewService = viewService ?? throw new ArgumentNullException(nameof(viewService));
            this.wcfClientFactory = wcfClientFactory ?? throw new ArgumentNullException(nameof(wcfClientFactory));

            ShowConsoleCommand = new RelayCommand(_ => viewService.ShowMainWindow());
            QuitCommand = new RelayCommand(_ => Application.Current.Shutdown());

            EnterAdminModeCommand = new RelayCommand(ExecuteEnterAdminMode);
            RefreshCommand = new RelayCommand(async (_) => await ExecuteRefresh());
            InstallCommand = new RelayCommand(ExecuteInstall, CanInstall);
            RequestCertificateCommand = new RelayCommand(async (_) => await ExecuteRequestCertificateAsync());
            SettingsCommand = new RelayCommand(ExecuteSettings, CanSettings);

            AvailablePackages = new ObservableCollection<PackageItem>();

            timer.AutoReset = false;
            timer.Start();
            timer.Elapsed += async (o, e) => await Timer_Elapsed();
        }

        public ICommand EnterAdminModeCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand InstallCommand { get; }
        public ICommand ShowConsoleCommand { get; }
        public ICommand QuitCommand { get; }
        public ICommand RequestCertificateCommand { get; }
        public ICommand SettingsCommand { get; }

        public StateIndicatorViewModel StateIndicator { get; } = new StateIndicatorViewModel();

        public bool IsAdminMode => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

        public bool IsUserMode => !IsAdminMode;

        public ServiceState ServiceState
        {
            get => serviceState;
            set
            {
                if (serviceState == value) return;
                serviceState = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TaskbarIcon));
                OnPropertyChanged(nameof(TaskbarIconText));
                OnPropertyChanged(nameof(IsDeviceIdAvailable));
                StateIndicator.SetState(value);
                ThreadHelper.SafeInvoke(CommandManager.InvalidateRequerySuggested);
            }
        }


        public string DeviceId
        {
            get => deviceId;
            private set
            {
                if (deviceId == value) return;
                deviceId = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsDeviceIdAvailable));
            }
        }

        public bool IsDeviceIdAvailable => !string.IsNullOrEmpty(DeviceId) && ServiceState == ServiceState.Active;

        public string MsiFolder
        {
            get => msiFolder;
            set
            {
                if (msiFolder == value) return;
                msiFolder = value;
                OnPropertyChanged();
            }
        }

        public bool OperationInProgress
        {
            get => operationInProgress;
            set
            {
                if (operationInProgress == value) return;
                operationInProgress = value;
                OnPropertyChanged();
                StateIndicator.IsBusy = operationInProgress;
            }
        }

        public ObservableCollection<PackageItem> AvailablePackages { get; }

        private async Task Timer_Elapsed()
        {
            if (timer.Interval == InitialDelay)
            {
                timer.Interval = RefreshInterval;
            }
            if (!OperationInProgress)
            {
                await ExecuteRefresh();
            }
            timer.Start();
        }

        private async Task ExecuteRequestCertificateAsync()
        {
            await RequestCertificateAsync(showExplanation: false);
        }

        private async Task RequestCertificateAsync(bool showExplanation)
        {
            RequestCertificateDialogViewModel vm = new RequestCertificateDialogViewModel(viewService, wcfClientFactory, showExplanation);
            bool success = viewService.ShowDialog(vm);
            if (success)
            {
                await ExecuteRefresh();
                if (ServiceState == ServiceState.AuthorizationError)
                {
                    string message = string.Format(GetText(Texts.BadCertificateMessage), vm.DeviceId);
                    viewService.ShowMessageBox(message);
                }
                else
                {
                    string message = string.Format(GetText(Texts.GoodCertificateMessage), vm.DeviceId);
                    viewService.ShowMessageBox(message);
                }
            }
        }

        private void ExecuteEnterAdminMode(object _)
        {
            if (IsAdminMode) return;

            using (Process p = new Process())
            {
                p.StartInfo.FileName = Assembly.GetEntryAssembly().Location;
                p.StartInfo.Arguments = CommandLineHelper.AllowSecondInstanceCommand + " " + CommandLineHelper.VisibleMainWindowCommand;
                p.StartInfo.Verb = "runas";

                bool started = false;
                try
                {
                    started = p.Start();
                }
                catch (Exception)
                {
                    started = false;
                }

                if (started)
                {
                    Application.Current.Shutdown();
                }
            };
        }

        private bool CanSettings(object obj)
        {
            // todo block if service is not available
            return true;
        }

        private void ExecuteSettings(object obj)
        {
            SettingsDialogViewModel vm = new SettingsDialogViewModel(viewService, wcfClientFactory);
            if (!vm.IsInitialized) return;

            viewService.ShowDialog(vm);
        }

        private bool CanInstall(object _)
        {
            List<PackageItem> selected = AvailablePackages.Where(p => p.IsSelected).ToList();
            return selected.Any() && selected.All(p => p.Package.Status == PackageStatus.Downloaded
                                                    || p.Package.Status == PackageStatus.SuggestedToInstall
                                                    || p.Package.Status == PackageStatus.Failed);
        }

        private async void ExecuteInstall(object _)
        {
            OperationInProgress = true;
            IWcfService service = null;

            Package[] selectedPackages = AvailablePackages
                .Where(p => p.IsSelected && (p.Package.Status == PackageStatus.Downloaded
                                            || p.Package.Status == PackageStatus.SuggestedToInstall
                                            || p.Package.Status == PackageStatus.Failed))
                .Select(p => p.Package)
                .ToArray();
            try
            {
                service = wcfClientFactory.CreateClient();
                await service.StartInstallationAsync(selectedPackages);
                ServiceState = ServiceState.Active;
                StateIndicator.SetInfo($"{GetText(Texts.Active)}");
            }
            catch (System.ServiceModel.EndpointNotFoundException)
            {
                ServiceState = ServiceState.ClientUnaccessible;
                var message = GetText(Texts.CannotStartInstallation);
                StateIndicator.SetInfo(message);
                viewService.ShowMessageBox($"{GetText(Texts.CannotStartInstallation)}\n{message}");
            }
            catch (Exception e)
            {
                ServiceState = ServiceState.ClientUnaccessible;
                var message = e.Message;
                StateIndicator.SetInfo(message);
                viewService.ShowMessageBox($"{GetText(Texts.CannotStartInstallation)}\n{message}\n\n{e.StackTrace}");
            }
            finally
            {
                wcfClientFactory.CloseClient(service);
                OperationInProgress = false;
            }

            await ExecuteRefresh();
        }

        private async Task ExecuteRefresh()
        {
            OperationInProgress = true;
            IWcfService service = null;
            Package[] packages = null;
            ClientState clientState;
            try
            {
                service = wcfClientFactory.CreateClient();
                packages = await service.GetPackagesAsync();
                MsiFolder = await service.GetMsiFolderAsync();
                clientState = service.GetClientState();
                DeviceId = await service.GetDeviceIdAsync();
            }
            catch (System.ServiceModel.EndpointNotFoundException)
            {
                ServiceState = ServiceState.ClientUnaccessible;
                StateIndicator.SetInfo(GetText(Texts.ServiceNotResponding));
                DeviceId = null;
                OperationInProgress = false;
                return;
            }
            catch (Exception e)
            {
                ServiceState = ServiceState.ClientUnaccessible;
                StateIndicator.SetInfo($"{GetText(Texts.ServiceAccessError)}\n{e.Message}\n\n{e.StackTrace}");
                DeviceId = null;
                OperationInProgress = false;
                return;
            }
            finally
            {
                wcfClientFactory.CloseClient(service);
            }

            List<Package> selected = AvailablePackages.Where(p => p.IsSelected).Select(p => p.Package).ToList();

            List<PackageItem> packageItems = new List<PackageItem>();
            foreach (Package p in packages)
            {
                bool wasSelected = selected.Any(s => s.Filepath.Equals(p.Filepath, StringComparison.InvariantCultureIgnoreCase));
                packageItems.Add(new PackageItem(p, viewService) { IsSelected = wasSelected });
            }

            if (!firstTimeRefresh)
            {
                NotifyAboutChanges(AvailablePackages, packageItems);
            }

            ThreadHelper.SafeInvoke(() => // collection view can be updated only from UI thread!
            {
                AvailablePackages.Clear();
                foreach (var pi in packageItems)
                {
                    AvailablePackages.Add(pi);
                }
            });

            UpdateState(clientState);

            if (firstTimeRefresh)
            {
                firstTimeRefresh = false;
                PromptIfCertificateNotAvailable();
            }

            OperationInProgress = false;
        }

        private void PromptIfCertificateNotAvailable()
        {
            if (ServiceState == ServiceState.NoCertificate)
            {
                ThreadHelper.SafeInvoke(async () =>
                {
                    if (IsAdminMode)
                    {
                        await RequestCertificateAsync(showExplanation: true);
                    }
                    else
                    {
                        MessageBoxResult r = viewService.ShowMessageBox(GetText(Texts.DeviceNotInitialized), buttons: MessageBoxButton.OKCancel);
                        if (r == MessageBoxResult.OK)
                        {
                            ExecuteEnterAdminMode(null);
                        }
                    }
                });
            }
        }

        private void UpdateState(ClientState clientState)
        {
            switch (clientState.Status)
            {
                case ClientStatus.Running:
                    ServiceState = ServiceState.Active;
                    StateIndicator.SetInfo($"{GetText(Texts.Active)}");
                    break;
                case ClientStatus.Stopped:
                    ServiceState = ServiceState.Error;
                    StateIndicator.SetInfo($"{GetText(Texts.UnexpectedStop)} {clientState.LastError}");
                    break;
                case ClientStatus.Reconnecting:
                    ServiceState = ServiceState.Error;
                    StateIndicator.SetInfo($"{GetText(Texts.Reconnecting)}");
                    break;
                case ClientStatus.AuthorizationError:
                    ServiceState = ServiceState.AuthorizationError;
                    StateIndicator.SetInfo($"{GetText(Texts.AuthorizationError)} {clientState.LastError}");
                    break;
                case ClientStatus.NoCertificate:
                    ServiceState = ServiceState.NoCertificate;
                    StateIndicator.SetInfo(GetText(Texts.CertificateNotAvailable));
                    break;
                default:
                    throw new InvalidOperationException($"unsupported status {clientState.Status}");
            }
        }

        private void NotifyAboutChanges(IReadOnlyList<PackageItem> oldList, IReadOnlyList<PackageItem> newList)
        {
            var changes = new List<(PackageStatus oldStatus, PackageStatus newStatus, PackageItem item)>();
            foreach (var newListItem in newList)
            {
                var oldStatus = oldList.FirstOrDefault(pi => pi.Package.Filepath.Equals(newListItem.Package.Filepath, StringComparison.InvariantCultureIgnoreCase))?.Package.Status ?? PackageStatus.Unavailable;
                if (oldStatus == newListItem.Package.Status) continue;
                changes.Add((oldStatus, newListItem.Package.Status, newListItem));
            }

            IList<PackageItem> SelectChangedItems(Func<PackageStatus, bool> oldStatusCondition, Func<PackageStatus, bool> newStatusCondition)
                => changes.Where(p => oldStatusCondition(p.oldStatus) && newStatusCondition(p.newStatus)).Select(p => p.item).ToList();

            var downloaded = SelectChangedItems(oldStatus => oldStatus == PackageStatus.Unavailable || oldStatus == PackageStatus.Downloading,
                                                newStatus => newStatus == PackageStatus.Downloaded);
            if (downloaded.Any())
            {
                TryShowToastNotification(Texts.NewPackageAvailable, downloaded.Select(p => p.ProductName));
            }

            var suggested = SelectChangedItems(oldStatus => oldStatus == PackageStatus.Unavailable || oldStatus == PackageStatus.Downloading || oldStatus == PackageStatus.Downloaded,
                                                newStatus => newStatus == PackageStatus.SuggestedToInstall);
            if (suggested.Any())
            {
                TryShowToastNotification(Texts.NewPackageSuggested, suggested.Select(p => p.ProductName));
            }

            var failed = SelectChangedItems(oldStatus => oldStatus != PackageStatus.Failed,
                                            newStatus => newStatus == PackageStatus.Failed);
            if (failed.Any())
            {
                TryShowToastNotification(Texts.PackageInstallationFailed, failed.Select(p => $"{p.ProductName}\n({p.ExtraInfo})"));
            }

            var installed = SelectChangedItems(oldStatus => oldStatus != PackageStatus.Installed,
                                               newStatus => newStatus == PackageStatus.Installed);
            if (installed.Any())
            {
                TryShowToastNotification(Texts.NewPackageInstalled, installed.Select(p => p.ProductName).Distinct());
            }
        }

        private void TryShowToastNotification(Texts titleId, IEnumerable<string> details = null)
        {
            string title = GetText(titleId);
            try
            {
                ToastContentBuilder builder = new ToastContentBuilder().AddText(title);
                if (details != null)
                {
                    foreach (string text in details)
                    {
                        builder.AddText(text);
                    }
                }
                builder.Show();
            }
            catch
            {
                // Just ignore if cannot pop up the tost
            }
        }

        public string TaskbarIcon
        {
            get
            {
                string iconPath = null;
                switch (ServiceState)
                {
                    case ServiceState.Active:
                    case ServiceState.Unknown:
                        iconPath = "/Images/Active.ico";
                        break;
                    case ServiceState.ClientUnaccessible:
                        iconPath = "/Images/ClientUnaccessible.ico";
                        break;
                    case ServiceState.NoCertificate:
                    case ServiceState.AuthorizationError:
                        iconPath = "/Images/ServerUnaccessible.ico";
                        break;
                    case ServiceState.Error:
                        iconPath = "/Images/Error.ico";
                        break;
                }
                return iconPath;
            }
        }

        public string TaskbarIconText
        {
            get
            {
                string extraText = string.Empty;
                switch (ServiceState)
                {
                    case ServiceState.ClientUnaccessible:
                        extraText = GetText(Texts.ClientUnaccessible);
                        break;
                    case ServiceState.NoCertificate:
                        extraText = GetText(Texts.NoCertificate);
                        break;
                    case ServiceState.AuthorizationError:
                        extraText = GetText(Texts.AuthorizationError);
                        break;
                    case ServiceState.Error:
                        extraText = GetText(Texts.AgentOrServerFilure);
                        break;
                    default:
                        break;
                }
                return "RITMS UP2DATE" + (string.IsNullOrEmpty(extraText) ? string.Empty : "\n" + extraText);
            }
        }

        private string GetText(Texts text)
        {
            return viewService.GetText(text);
        }
    }
}
