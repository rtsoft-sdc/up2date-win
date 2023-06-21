using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Up2dateConsole.Helpers;
using Up2dateConsole.ServiceReference;
using Up2dateConsole.ViewService;

namespace Up2dateConsole.Dialogs.Authorization
{
    public class ImportCertificatePageViewModel : NotifyPropertyChanged
    {
        private Func<Func<IWcfService, Task<ResultOfstring>>, bool, Task> establishConnection;
        private readonly IViewService viewService;

        public ImportCertificatePageViewModel(Func<Func<IWcfService, Task<ResultOfstring>>, bool, Task> establishConnection,
            IViewService viewService)
        {
            this.establishConnection = establishConnection ?? throw new ArgumentNullException(nameof(establishConnection));
            this.viewService = viewService ?? throw new ArgumentNullException(nameof(viewService));

            LoadCommand = new RelayCommand(async (_) => await ExecuteLoadAsync());
        }

        public ICommand LoadCommand { get; }

        private async Task ExecuteLoadAsync()
        {
            var certFilePath = viewService.ShowOpenDialog(viewService.GetText(Texts.LoadCertificate),
                "X.509 certificate files|*.cer|All files|*.*");
            if (string.IsNullOrWhiteSpace(certFilePath)) return;

            await establishConnection(async service => await service.ImportCertificateFileAsync(certFilePath), true);
        }
    }
}
