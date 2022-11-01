using Up2dateConsole.Helpers;

namespace Up2dateConsole.Dialogs.Settings
{
    public class ServerConnectionTabViewModel : NotifyPropertyChanged
    {
        private string tokenUrl;
        private string dpsUrl;

        public ServerConnectionTabViewModel(string header)
        {
            Header = header;
        }

        public string Header { get; }

        public bool Initialize(ServiceReference.IWcfService service)
        {
            TokenUrl = service.GetRequestCertificateUrl();
            DpsUrl = service.GetProvisioningUrl();

            return true;
        }

        public bool IsValid => !string.IsNullOrWhiteSpace(TokenUrl) && !string.IsNullOrWhiteSpace(DpsUrl);

        public bool Apply(ServiceReference.IWcfService service)
        {
            service.SetRequestCertificateUrl(TokenUrl);
            service.SetProvisioningUrl(DpsUrl);

            return true;
        }

        public string TokenUrl
        {
            get => tokenUrl;
            set
            {
                if (tokenUrl == value) return;
                tokenUrl = value;
                OnPropertyChanged();
            }
        }

        public string DpsUrl
        {
            get => dpsUrl;
            set
            {
                if (dpsUrl == value) return;
                dpsUrl = value;
                OnPropertyChanged();
            }
        }

    }
}
