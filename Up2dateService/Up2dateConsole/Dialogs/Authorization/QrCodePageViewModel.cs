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
        private readonly IViewService viewService;
        private readonly IWcfClientFactory wcfClientFactory;

        public QrCodePageViewModel(Func<Func<IWcfService, Task<ResultOfstring>>, bool, Task> establishConnection, IViewService viewService, IWcfClientFactory wcfClientFactory)
        {
            this.establishConnection = establishConnection ?? throw new ArgumentNullException(nameof(establishConnection));
            this.viewService = viewService ?? throw new ArgumentNullException(nameof(viewService));
            this.wcfClientFactory = wcfClientFactory ?? throw new ArgumentNullException(nameof(wcfClientFactory));

            QrCodeCommand = new RelayCommand(async (_) => await ExecuteQrCodeAsync());
        }

        public ICommand QrCodeCommand { get; }

        private async Task ExecuteQrCodeAsync()
        {
            var vm = new QrCodeDialogViewModel(viewService, new QrCodeHelper(), wcfClientFactory);
            bool ok = viewService.ShowDialog(vm);
            if (ok && !string.IsNullOrWhiteSpace(vm.Cert))
            {
                await establishConnection(async service => await service.ImportCertificateAsync(vm.Cert), true);
            }
        }

    }
}
