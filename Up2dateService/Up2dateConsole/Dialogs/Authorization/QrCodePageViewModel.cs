using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Up2dateConsole.Dialogs.QrCode;
using Up2dateConsole.Helpers;
using Up2dateConsole.ServiceReference;
using Up2dateConsole.ViewService;

namespace Up2dateConsole.Dialogs.Authorization
{
    public class QrCodePageViewModel : NotifyPropertyChanged
    {
        private Func<Func<IWcfService, Task<ResultOfstring>>, bool, Task> establishConnection;
        private string controllerId;
        private readonly IViewService viewService;
        private readonly IWcfClientFactory wcfClientFactory;
        private readonly IProcessHelper processHelper;

        public QrCodePageViewModel(Func<Func<IWcfService, Task<ResultOfstring>>, bool, Task> establishConnection,
            IViewService viewService, IWcfClientFactory wcfClientFactory, IProcessHelper processHelper)
        {
            this.establishConnection = establishConnection ?? throw new ArgumentNullException(nameof(establishConnection));
            this.viewService = viewService ?? throw new ArgumentNullException(nameof(viewService));
            this.wcfClientFactory = wcfClientFactory ?? throw new ArgumentNullException(nameof(wcfClientFactory));
            this.processHelper = processHelper ?? throw new ArgumentNullException(nameof(processHelper));

            QrCodeCommand = new RelayCommand(async (_) => await ExecuteQrCodeAsync(), CanExecuteQrCode);
        }

        public ICommand QrCodeCommand { get; }

        public string ControllerId
        {
            get => controllerId;
            set
            {
                if (controllerId == value) return;
                controllerId = value;
                OnPropertyChanged();
            }
        }

        private async Task ExecuteQrCodeAsync()
        {
            var vm = new QrCodeDialogViewModel(viewService, new QrCodeHelper(), wcfClientFactory, processHelper, ControllerId);
            bool ok = viewService.ShowDialog(vm);
            if (ok && !string.IsNullOrWhiteSpace(vm.Cert))
            {
                await establishConnection(async service => await service.ImportCertificateAsync(vm.Cert), true);
            }
        }

        internal void Initialize(IWcfService service)
        {
            ControllerId = service.GetDeviceId();
            if (string.IsNullOrEmpty(controllerId))
            {
                ControllerId = service.GetSystemInfo().MachineGuid;
            }
        }

        private bool CanExecuteQrCode(object _)
        {
            return !string.IsNullOrWhiteSpace(ControllerId) 
                && Uri.IsWellFormedUriString(controllerId, UriKind.Relative);
        }

    }
}
