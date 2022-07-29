using System;
using System.Windows.Input;
using Up2dateConsole.Helpers;
using Up2dateConsole.ServiceReference;
using Up2dateConsole.ViewService;

namespace Up2dateConsole.Dialogs
{
    public class RequestCertificateDialogViewModel : DialogViewModelBase
    {
        private readonly IViewService viewService;
        private readonly IWcfClientFactory wcfClientFactory;
        private string oneTimeKey;

        public RequestCertificateDialogViewModel(IViewService viewService, IWcfClientFactory wcfClientFactory)
        {
            this.viewService = viewService ?? throw new ArgumentNullException(nameof(viewService));
            this.wcfClientFactory = wcfClientFactory ?? throw new ArgumentNullException(nameof(wcfClientFactory));

            RequestCommand = new RelayCommand(ExecuteRequest, CanRequest);
        }

        public ICommand RequestCommand { get; }

        public string OneTimeKey
        {
            get => oneTimeKey;
            set
            {
                if (oneTimeKey == value) return;
                oneTimeKey = value;
                OnPropertyChanged();
            }
        }

        public string DeviceId { get; private set; }

        private bool CanRequest(object _)
        {
            return !string.IsNullOrWhiteSpace(OneTimeKey);
        }

        private void ExecuteRequest(object _)
        {
            IWcfService service = null;
            try
            {
                service = wcfClientFactory.CreateClient();
                ResultOfstring result = service.RequestCertificate(OneTimeKey);
                if (!result.Success)
                {
                    viewService.ShowMessageBox($"Failed to acquire certificate.\n\n{result.ErrorMessage}");
                }
                else
                {
                    DeviceId = result.Value;
                    Close(true);
                }
            }
            catch (Exception e)
            {
                viewService.ShowMessageBox($"Failed to acquire certificate.\n\n{e.Message}");
            }
            finally
            {
                wcfClientFactory.CloseClient(service);
            }
        }

    }
}
