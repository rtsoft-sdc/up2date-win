using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Up2dateConsole.Dialogs.Authorization;
using Up2dateConsole.Dialogs.Settings;
using Up2dateConsole.Helpers;
using Up2dateConsole.Notifier;
using Up2dateConsole.ServiceReference;
using Up2dateConsole.Session;
using Up2dateConsole.StateIndicator;
using Up2dateConsole.StatusBar;
using Up2dateConsole.ToolBar;
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
        private readonly Timer timer = new Timer(InitialDelay);
        private readonly IViewService viewService;
        private readonly IWcfClientFactory wcfClientFactory;
        private readonly ISettings settings;
        private readonly ISession session;
        private readonly IProcessHelper processHelper;
        private readonly INotifier notifier;
        private readonly IServiceHelper serviceHelper;
        private bool isSettingsDialogActive = false;
        private bool canAcceptReject;
        private bool canDelete;
        private bool canInstall;

        public MainWindowViewModel(IViewService viewService, IWcfClientFactory wcfClientFactory, ISettings settings, ISession session,
            IProcessHelper processHelper, INotifier notifier, IServiceHelper serviceHelper)
        {
            this.viewService = viewService ?? throw new ArgumentNullException(nameof(viewService));
            this.wcfClientFactory = wcfClientFactory ?? throw new ArgumentNullException(nameof(wcfClientFactory));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.processHelper = processHelper ?? throw new ArgumentNullException(nameof(processHelper));
            this.notifier = notifier ?? throw new ArgumentNullException(nameof(notifier));
            this.serviceHelper = serviceHelper ?? throw new ArgumentNullException(nameof(serviceHelper));
            ShowConsoleCommand = new RelayCommand(_ => viewService.ShowMainWindow());
            QuitCommand = new RelayCommand(_ => session.Shutdown());

            EnterAdminModeCommand = new RelayCommand(_ => session.ToAdminMode());
            LeaveAdminModeCommand = new RelayCommand(_ => session.ToUserMode());
            RefreshCommand = new RelayCommand(async _ => await ExecuteRefresh(), _ => !OperationInProgress);
            InstallCommand = new RelayCommand(ExecuteInstall, _ => canInstall && IsServiceRunning);
            AcceptCommand = new RelayCommand(async _ => await Accept(true), _ => canAcceptReject && IsServiceRunning);
            RejectCommand = new RelayCommand(async _ => await Accept(false), _ => canAcceptReject && IsServiceRunning);
            DeleteCommand = new RelayCommand(async _ => await Delete(), _ => canDelete && IsServiceRunning);
            RequestCertificateCommand = new RelayCommand(async _ => await RequestCertificateAsync(showExplanation: false), _ => IsServiceRunning);
            SettingsCommand = new RelayCommand(ExecuteSettings);
            StartServiceCommand = new RelayCommand(async _ => await ExecuteStartService(), _ => !IsServiceRunning);
            StopServiceCommand = new RelayCommand(async _ => await ExecuteStopService(), _ => IsServiceRunning);

            StatusBar = new StatusBarViewModel(session, EnterAdminModeCommand, processHelper);
            ToolBar = new ToolBarViewModel(session, RefreshCommand, InstallCommand, AcceptCommand, RejectCommand, DeleteCommand, RequestCertificateCommand, SettingsCommand);

            session.ShuttingDown += Session_ShuttingDown;

            AvailablePackages = new ObservableCollection<PackageItem>();
            CollectionViewSource.GetDefaultView(AvailablePackages).CurrentChanged += (o, e) => OnCurrentChanged();

            timer.AutoReset = false;
            timer.Start();
            timer.Elapsed += async (o, e) => await Timer_Elapsed();
        }

        private async Task Delete()
        {
            IList<PackageItem> selectedItems = AvailablePackages.Where(p => p.IsSelected).ToList();
            if (selectedItems.Count != 1) return;

            PackageItem seletedItem = selectedItems.First();

            if (MessageBoxResult.Cancel == viewService.ShowMessageBox(string.Format(GetText(Texts.ConfirmDeleteFmt), seletedItem.ProductName, seletedItem.Version), MessageBoxButton.OKCancel))
            {
                return;
            }

            string error = await CallServiceAsync(async service =>
            {
                Result r = await service.DeletePackageAsync(seletedItem.Package);
                return r.Success ? string.Empty : r.ErrorMessage;
            });

            if (!string.IsNullOrEmpty(error))
            {
                viewService.ShowMessageBox($"{GetText(Texts.CannotDeletePackage)}\n{error}");
            }

            await ExecuteRefresh();
        }

        private void Session_ShuttingDown(object sender, EventArgs e)
        {
            timer.Stop();
        }

        private async Task ExecuteStopService()
        {
            string error = serviceHelper.StopService();
            if (!string.IsNullOrEmpty(error))
            {
                viewService.ShowMessageBox(viewService.GetText(Texts.CannotStopService) + "\n" + error);
            }
            await ExecuteRefresh();
        }

        private async Task ExecuteStartService()
        {
            string error = serviceHelper.StartService();
            if (!string.IsNullOrEmpty(error))
            {
                viewService.ShowMessageBox(viewService.GetText(Texts.CannotStopService) + "\n" + error);
            }
            await ExecuteRefresh();
        }

        public ICommand EnterAdminModeCommand { get; }
        public ICommand LeaveAdminModeCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand InstallCommand { get; }
        public ICommand AcceptCommand { get; }
        public ICommand RejectCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ShowConsoleCommand { get; }
        public ICommand StartServiceCommand { get; }
        public ICommand StopServiceCommand { get; }
        public ICommand QuitCommand { get; }
        public ICommand RequestCertificateCommand { get; }
        public ICommand SettingsCommand { get; }

        public StatusBarViewModel StatusBar { get; }
        public ToolBarViewModel ToolBar { get; }

        public bool IsAdminMode => session.IsAdminMode;

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
                StatusBar.SetState(value);
                ThreadHelper.SafeInvoke(CommandManager.InvalidateRequerySuggested);
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
                StatusBar.SetBusy(operationInProgress);
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public ObservableCollection<PackageItem> AvailablePackages { get; }

        private void OnCurrentChanged()
        {
            IList<PackageItem> selectedItems = AvailablePackages.Where(p => p.IsSelected).ToList();
            if (selectedItems.Count != 1)
            {
                canAcceptReject = false;
                canDelete = false;
                canInstall = false;
            }
            else
            {
                Package package = selectedItems.First().Package;
                canAcceptReject =  package.Status == PackageStatus.WaitingForConfirmation
                    || package.Status == PackageStatus.WaitingForConfirmationForced;
                canDelete = package.Status == PackageStatus.Downloaded
                    || package.Status == PackageStatus.Rejected
                    || package.Status == PackageStatus.Failed;
                canInstall = package.Status == PackageStatus.Downloaded
                    || package.Status == PackageStatus.Rejected
                    || package.Status == PackageStatus.Failed;
            }
            ToolBar.CanAcceptReject = canAcceptReject;
            ToolBar.CanDelete = canDelete;
            ToolBar.CanInstall = canInstall;
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

            if (!session.IsShuttingDown)
            {
                timer.Start();
            }
        }

        private async Task RequestCertificateAsync(bool showExplanation)
        {
            AuthorizationDialogViewModel vm = new AuthorizationDialogViewModel(viewService, wcfClientFactory, showExplanation, processHelper);
            bool success = viewService.ShowDialog(vm);
            if (success)
            {
                await ExecuteRefresh();

                string message = ServiceState == ServiceState.Active
                    ? GetText(Texts.GoodConnectionMessage)
                    : vm.IsPlainTokenMode
                        ? GetText(Texts.BadConnectionMessage)
                        : GetText(Texts.BadCertificateMessage);

                viewService.ShowMessageBox(message);
            }
        }

        private void ExecuteSettings(object obj)
        {
            viewService.ShowMainWindow(); // needed for calling the command from tray

            if (isSettingsDialogActive) return;

            SettingsDialogViewModel vm = new SettingsDialogViewModel(viewService, wcfClientFactory, settings, IsServiceRunning);
            if (!vm.IsInitialized) return;

            isSettingsDialogActive = true;
            bool dialogOK = viewService.ShowDialog(vm);
            isSettingsDialogActive = false;

            if (dialogOK)
            {
                session.OnSettingsUpdated();
            }
        }

        private async Task Accept(bool accept)
        {
            IList<PackageItem> selectedItems = AvailablePackages.Where(p => p.IsSelected).ToList();
            if (selectedItems.Count != 1) return;

            Package package = selectedItems.First().Package;

            string error = await CallServiceAsync(async service =>
            {
                if (accept)
                {
                    await service.AcceptInstallationAsync(package);
                }
                else
                {
                    await service.RejectInstallationAsync(package);
                }
                return string.Empty;
            });

            if (!string.IsNullOrEmpty(error))
            {
                viewService.ShowMessageBox($"{GetText(accept ? Texts.CannotAcceptInstallation : Texts.CannotRejectInstallation)}\n{error}");
            }

            await ExecuteRefresh();
        }

        private async void ExecuteInstall(object _)
        {
            Package[] selectedPackages = AvailablePackages
                .Where(p => p.IsSelected && (p.Package.Status == PackageStatus.Downloaded
                                         || p.Package.Status == PackageStatus.Rejected
                                         || p.Package.Status == PackageStatus.Failed))
                .Select(p => p.Package)
                .ToArray();

            string error = await CallServiceAsync(async service =>
            {
                await service.StartInstallationAsync(selectedPackages);
                return string.Empty;
            });

            if (!string.IsNullOrEmpty(error))
            {
                viewService.ShowMessageBox($"{GetText(Texts.CannotStartInstallation)}\n{error}");
            }

            await ExecuteRefresh();
        }

        private async Task ExecuteRefresh()
        {
            Package[] packages = null;

            string error = await CallServiceAsync(async service =>
            {
                packages = await service.GetPackagesAsync();
                MsiFolder = await service.GetMsiFolderAsync();
                StatusBar.SetConnectionInfo(await service.GetDeviceIdAsync(), await service.GetTenantAsync(), await service.GetHawkbitEndpointAsync());
                return string.Empty;
            });

            if (!string.IsNullOrEmpty(error))
            {
                StatusBar.SetConnectionInfo(null, null, null);
                return;
            }

            OperationInProgress = true;

            List<Package> selected = AvailablePackages.Where(p => p.IsSelected).Select(p => p.Package).ToList();

            List<PackageItem> packageItems = new List<PackageItem>();
            foreach (Package p in packages)
            {
                bool wasSelected = selected.Any(s => s.Filepath.Equals(p.Filepath, StringComparison.InvariantCultureIgnoreCase));
                packageItems.Add(new PackageItem(p, viewService) { IsSelected = wasSelected });
            }

            if (!firstTimeRefresh)
            {
                notifier.NotifyAboutChanges(AvailablePackages, packageItems);
            }

            ThreadHelper.SafeInvoke(() => // collection view can be updated only from UI thread!
            {
                AvailablePackages.Clear();
                foreach (PackageItem pi in packageItems)
                {
                    AvailablePackages.Add(pi);
                }
            });

            if (firstTimeRefresh)
            {
                firstTimeRefresh = false;
                PromptIfCertificateNotAvailable();
            }

            OperationInProgress = false;

            OnCurrentChanged();
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
                    StatusBar.SetInfo($"{GetText(Texts.Active)}");
                    break;
                case ClientStatus.Stopped:
                    ServiceState = ServiceState.Error;
                    StatusBar.SetInfo($"{GetText(Texts.UnexpectedStop)} {clientState.LastError}");
                    break;
                case ClientStatus.Reconnecting:
                    ServiceState = ServiceState.Error;
                    StatusBar.SetInfo($"{GetText(Texts.Reconnecting)}");
                    break;
                case ClientStatus.AuthorizationError:
                    ServiceState = ServiceState.AuthorizationError;
                    StatusBar.SetInfo($"{GetText(Texts.AuthorizationError)} {clientState.LastError}");
                    break;
                case ClientStatus.NoCertificate:
                    ServiceState = ServiceState.NoCertificate;
                    StatusBar.SetInfo(GetText(Texts.CertificateNotAvailable));
                    break;
                default:
                    throw new InvalidOperationException($"unsupported status {clientState.Status}");
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

        public bool IsServiceRunning => serviceHelper.IsServiceRunning;

        private string GetText(Texts text)
        {
            return viewService.GetText(text);
        }

        private async Task<string> CallServiceAsync(Func<IWcfService, Task<string>> callAsync)
        {
            OperationInProgress = true;
            IWcfService service = null;
            string error = string.Empty;
            try
            {
                service = wcfClientFactory.CreateClient();
                error = await callAsync(service);
                ServiceState = ServiceState.Active;
                StatusBar.SetInfo(GetText(Texts.Active));
                var clientState = service.GetClientState();
                UpdateState(clientState);
            }
            catch (System.ServiceModel.EndpointNotFoundException)
            {
                ServiceState = ServiceState.ClientUnaccessible;
                error = GetText(Texts.ServiceNotResponding);
                StatusBar.SetInfo(error);
            }
            catch (Exception e)
            {
                ServiceState = ServiceState.ClientUnaccessible;
                error = $"{e.Message}\n\n{e.StackTrace}";
                StatusBar.SetInfo($"{GetText(Texts.ServiceAccessError)}\n{error}");
            }
            finally
            {
                wcfClientFactory.CloseClient(service);
                OperationInProgress = false;
            }

            return error;
        }
    }
}
