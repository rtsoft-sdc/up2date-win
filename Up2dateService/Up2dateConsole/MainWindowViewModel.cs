using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Up2dateConsole.Dialogs.RequestCertificate;
using Up2dateConsole.Dialogs.Settings;
using Up2dateConsole.Helpers;
using Up2dateConsole.Helpers.InactivityMonitor;
using Up2dateConsole.ServiceReference;
using Up2dateConsole.Session;
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
        private readonly IInactivityMonitor inactivityMonitor;
        private readonly ISettings settings;
        private readonly ISession session;
        private readonly string ServiceName = "Up2dateService";
        private bool IsSettingsDialogActive = false;
        private SystemInfo? systemInfo;

        public MainWindowViewModel(IViewService viewService, IWcfClientFactory wcfClientFactory,
            IInactivityMonitor inactivityMonitor, ISettings settings, ISession session)
        {
            this.viewService = viewService ?? throw new ArgumentNullException(nameof(viewService));
            this.wcfClientFactory = wcfClientFactory ?? throw new ArgumentNullException(nameof(wcfClientFactory));
            this.inactivityMonitor = inactivityMonitor ?? throw new ArgumentNullException(nameof(inactivityMonitor));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            ShowConsoleCommand = new RelayCommand(_ => viewService.ShowMainWindow());
            QuitCommand = new RelayCommand(_ => session.Shutdown());

            EnterAdminModeCommand = new RelayCommand(_ => session.ToAdminMode());
            LeaveAdminModeCommand = new RelayCommand(_ => session.ToUserMode());
            RefreshCommand = new RelayCommand(async _ => await ExecuteRefresh(), _ => !OperationInProgress);
            InstallCommand = new RelayCommand(ExecuteInstall, CanInstall);
            AcceptCommand = new RelayCommand(async _ => await Accept(true), _ => CanAcceptReject);
            RejectCommand = new RelayCommand(async _ => await Accept(false), _ => CanAcceptReject);
            RequestCertificateCommand = new RelayCommand(async _ => await ExecuteRequestCertificateAsync(), _ => IsServiceRunning);
            SettingsCommand = new RelayCommand(ExecuteSettings);
            StartServiceCommand = new RelayCommand(async _ => await ExecuteStartService(), _ => !IsServiceRunning);
            StopServiceCommand = new RelayCommand(async _ => await ExecuteStopService(), _ => IsServiceRunning);

            session.ShuttingDown += Session_ShuttingDown;

            AvailablePackages = new ObservableCollection<PackageItem>();
            CollectionViewSource.GetDefaultView(AvailablePackages).CurrentChanged += (o, e) => OnPropertyChanged(nameof(CanAcceptReject));

            timer.AutoReset = false;
            timer.Start();
            timer.Elapsed += async (o, e) => await Timer_Elapsed();

            if (session.IsAdminMode)
            {
                inactivityMonitor.MonitorKeyboardEvents = true;
                inactivityMonitor.MonitorMouseEvents = true;
                inactivityMonitor.Elapsed += InactivityMonitor_Elapsed;
                UpdateInactivityMonitor();
            }
        }

        private void Session_ShuttingDown(object sender, EventArgs e)
        {
            timer.Stop();
            //todo wait for async tasks to complete
        }

        public void OnWindowClosing()
        {
            if (session.IsAdminMode && settings.LeaveAdminModeOnClose && !session.IsShuttingDown)
            {
                session.ToUserMode();
            }
        }

        private void InactivityMonitor_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (session.IsAdminMode)
            {
                session.ToUserMode();
            }
            inactivityMonitor.Reset();
        }

        private async Task ExecuteStopService()
        {
            try
            {
                using (ServiceController sc = new ServiceController(ServiceName))
                {
                    if (!sc.Status.Equals(ServiceControllerStatus.Stopped) && !sc.Status.Equals(ServiceControllerStatus.StopPending))
                    {
                        sc.Stop();
                        ServiceHelper.ChangeStartMode(sc, ServiceStartMode.Manual);
                    }
                }
            }
            catch (Exception e)
            {
                viewService.ShowMessageBox(viewService.GetText(Texts.CannotStopService) + "\n" + e.Message);
            }
            await ExecuteRefresh();
        }

        private async Task ExecuteStartService()
        {
            try
            {
                using (ServiceController sc = new ServiceController(ServiceName))
                {
                    if (!sc.Status.Equals(ServiceControllerStatus.Running) && !sc.Status.Equals(ServiceControllerStatus.StartPending))
                    {
                        ServiceHelper.ChangeStartMode(sc, ServiceStartMode.Automatic);
                        sc.Start();
                    }
                }
            }
            catch (Exception e)
            {
                viewService.ShowMessageBox(viewService.GetText(Texts.CannotStartService) + "\n" + e.Message);
            }
            await ExecuteRefresh();
        }

        public ICommand EnterAdminModeCommand { get; }
        public ICommand LeaveAdminModeCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand InstallCommand { get; }
        public ICommand AcceptCommand { get; }
        public ICommand RejectCommand { get; }
        public ICommand ShowConsoleCommand { get; }
        public ICommand StartServiceCommand { get; }
        public ICommand StopServiceCommand { get; }
        public ICommand QuitCommand { get; }
        public ICommand RequestCertificateCommand { get; }
        public ICommand SettingsCommand { get; }

        public StateIndicatorViewModel StateIndicator { get; } = new StateIndicatorViewModel();

        public bool IsAdminMode => session.IsAdminMode;

        public bool IsUserMode => !session.IsAdminMode;

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

        public SystemInfo SystemInfo
        {
            get
            {
                if (!systemInfo.HasValue)
                {
                    IWcfService service = wcfClientFactory.CreateClient();
                    systemInfo = service.GetSystemInfo();
                    wcfClientFactory.CloseClient(service);
                }
                return systemInfo.Value;
            }
        }

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
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public ObservableCollection<PackageItem> AvailablePackages { get; }

        public bool CanAcceptReject
        {
            get
            {
                IList<PackageItem> selectedItems = AvailablePackages.Where(p => p.IsSelected).ToList();
                if (selectedItems.Count != 1) return false;

                Package package = selectedItems.First().Package;
                return package.Status == PackageStatus.WaitingForConfirmation
                    || package.Status == PackageStatus.WaitingForConfirmationForced;
            }
        }

        private async Task Timer_Elapsed()
        {
            if (timer.Interval == InitialDelay)
            {
                timer.Interval = RefreshInterval;
            }
            if (!OperationInProgress)
            {
                await ThreadHelper.SafeInvokeAsync(ExecuteRefresh);
            }
            timer.Start();
        }

        private async Task ExecuteRequestCertificateAsync()
        {
            await RequestCertificateAsync(showExplanation: false);
        }

        private async Task RequestCertificateAsync(bool showExplanation)
        {
            RequestCertificateDialogViewModel vm = new RequestCertificateDialogViewModel(viewService, wcfClientFactory, showExplanation, SystemInfo.MachineGuid);
            bool success = viewService.ShowDialog(vm);
            if (success)
            {
                await ExecuteRefresh();

                string message = ServiceState == ServiceState.Active
                    ? GetText(Texts.GoodConnectionMessage)
                    : vm.IsSecureConnection
                        ? GetText(Texts.BadCertificateMessage)
                        : GetText(Texts.BadConnectionMessage);

                viewService.ShowMessageBox(message);
            }
        }

        private void ExecuteSettings(object obj)
        {
            viewService.ShowMainWindow(); // needed for calling the command from tray

            if (IsSettingsDialogActive) return;

            SettingsDialogViewModel vm = new SettingsDialogViewModel(viewService, wcfClientFactory, settings, IsServiceRunning);
            if (!vm.IsInitialized) return;

            IsSettingsDialogActive = true;
            viewService.ShowDialog(vm);
            IsSettingsDialogActive = false;

            if (session.IsAdminMode)
            {
                UpdateInactivityMonitor();
            }
        }

        private void UpdateInactivityMonitor()
        {
            const int MillisecondsInSecond = 1000;
            const int MinTimeoutSec = 5;

            inactivityMonitor.Enabled = settings.LeaveAdminModeOnInactivity;
            var timeout = settings.LeaveAdminModeOnInactivityTimeout;
            if (timeout < MinTimeoutSec)
            {
                timeout = MinTimeoutSec;
            }
            inactivityMonitor.Interval = timeout * MillisecondsInSecond;
        }

        private async Task Accept(bool accept)
        {
            IList<PackageItem> selectedItems = AvailablePackages.Where(p => p.IsSelected).ToList();
            if (selectedItems.Count != 1) return;

            OperationInProgress = true;
            IWcfService service = null;

            Package package = selectedItems.First().Package;

            try
            {
                service = wcfClientFactory.CreateClient();
                if (accept)
                {
                    await service.AcceptInstallationAsync(package);
                }
                else
                {
                    await service.RejectInstallationAsync(package);
                }
                ServiceState = ServiceState.Active;
                StateIndicator.SetInfo($"{GetText(Texts.Active)}");
            }
            catch (System.ServiceModel.EndpointNotFoundException)
            {
                ServiceState = ServiceState.ClientUnaccessible;
                var message = GetText(Texts.CannotRejectInstallation);
                StateIndicator.SetInfo(message);
                viewService.ShowMessageBox($"{GetText(Texts.CannotRejectInstallation)}\n{message}");
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

        private bool CanInstall(object _)
        {
            List<PackageItem> selected = AvailablePackages.Where(p => p.IsSelected).ToList();
            return selected.Any() && selected.All(p => p.Package.Status == PackageStatus.Downloaded
                                                    || p.Package.Status == PackageStatus.Rejected
                                                    || p.Package.Status == PackageStatus.Failed);
        }

        private async void ExecuteInstall(object _)
        {
            OperationInProgress = true;
            IWcfService service = null;

            Package[] selectedPackages = AvailablePackages
                .Where(p => p.IsSelected && (p.Package.Status == PackageStatus.Downloaded
                                         || p.Package.Status == PackageStatus.Rejected
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
                foreach (PackageItem pi in packageItems)
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

            OnPropertyChanged(nameof(CanAcceptReject));
        }

        private void PromptIfCertificateNotAvailable()
        {
            if (ServiceState == ServiceState.NoCertificate)
            {
                ThreadHelper.SafeInvoke(async () =>
                {
                    if (session.IsAdminMode)
                    {
                        await RequestCertificateAsync(showExplanation: true);
                    }
                    else
                    {
                        string message = $"{GetText(Texts.DeviceNotInitialized)}\n\n{GetText(Texts.MachineGuidHint)}";
                        MessageBoxResult r = viewService.ShowMessageBox(message, buttons: MessageBoxButton.OKCancel);
                        if (r == MessageBoxResult.OK)
                        {
                            session.ToAdminMode();
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
            {
                return changes.Where(p => oldStatusCondition(p.oldStatus) && newStatusCondition(p.newStatus)).Select(p => p.item).ToList();
            }

            var downloaded = SelectChangedItems(oldStatus => oldStatus == PackageStatus.Unavailable || oldStatus == PackageStatus.Downloading,
                                                newStatus => newStatus == PackageStatus.Downloaded);
            if (downloaded.Any())
            {
                TryShowToastNotification(Texts.NewPackageAvailable, downloaded.Select(p => GetProductNameAndVersion(p)).Distinct());
            }

            var waiting = SelectChangedItems(oldStatus => oldStatus != PackageStatus.WaitingForConfirmation,
                                                newStatus => newStatus == PackageStatus.WaitingForConfirmation);
            if (waiting.Any())
            {
                TryShowToastNotification(Texts.NewPackageWaitingForConfirmation, waiting.Select(p => GetProductNameAndVersion(p)).Distinct());
            }

            var waitingForced = SelectChangedItems(oldStatus => oldStatus != PackageStatus.WaitingForConfirmationForced,
                                                newStatus => newStatus == PackageStatus.WaitingForConfirmationForced);
            if (waitingForced.Any())
            {
                TryShowToastNotification(Texts.NewPackageWaitingForConfirmationForced, waitingForced.Select(p => GetProductNameAndVersion(p)).Distinct());
            }

            var failed = SelectChangedItems(oldStatus => oldStatus != PackageStatus.Failed,
                                            newStatus => newStatus == PackageStatus.Failed);
            if (failed.Any())
            {
                TryShowToastNotification(Texts.PackageInstallationFailed, failed.Select(p => $"{GetProductNameAndVersion(p)}\n({p.ExtraInfo})").Distinct());
            }

            var installed = SelectChangedItems(oldStatus => oldStatus != PackageStatus.Installed,
                                               newStatus => newStatus == PackageStatus.Installed);
            if (installed.Any())
            {
                TryShowToastNotification(Texts.NewPackageInstalled, installed.Select(p => GetProductNameAndVersion(p)).Distinct());
            }
        }

        private string GetProductNameAndVersion(PackageItem item)
        {
            return $"{item.ProductName} {item.Version}";
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
                    case ServiceState.Unknown:
                    case ServiceState.Active:
                    default:
                        break;
                }
                System.Version v = Assembly.GetEntryAssembly().GetName().Version;
                return $"RITMS UP2DATE v{v.Major}.{v.Minor}.{v.Build}" + (string.IsNullOrEmpty(extraText) ? string.Empty : "\n" + extraText);
            }
        }

        public bool IsServiceRunning
        {
            get
            {
                using (ServiceController sc = new ServiceController(ServiceName))
                {
                    return sc.Status.Equals(ServiceControllerStatus.Running);
                }
            }
        }

        private string GetText(Texts text)
        {
            return viewService.GetText(text);
        }
    }
}
