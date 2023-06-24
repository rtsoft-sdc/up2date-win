using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Up2dateConsole.Helpers;
using Up2dateConsole.ServiceReference;

namespace Up2dateConsole.Dialogs.Authorization
{
    public class PlainTokenPageViewModel : NotifyPropertyChanged
    {
        private Func<Func<IWcfService, Task<ResultOfstring>>, bool, Task> establishConnection;
        private string hawkbitUrl;
        private string controllerId;
        private bool isUnsafeConnection;
        private bool isCertificateAvailable;
        private string deviceToken;

        public PlainTokenPageViewModel(Func<Func<IWcfService, Task<ResultOfstring>>, bool, Task> establishConnection)
        {
            this.establishConnection = establishConnection ?? throw new ArgumentNullException(nameof(establishConnection));

            RequestCommand = new RelayCommand(async (_) => await ExecuteRequestAsync(), CanRequest);
            BackToProtectedModeCommand = new RelayCommand(async (_) => await ExecuteBackToProtectedModeAsync());
        }

        public string HawkbitUrl
        {
            get => hawkbitUrl;
            set
            {
                if (hawkbitUrl == value) return;
                hawkbitUrl = value;
                OnPropertyChanged();
            }
        }

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

        public string DeviceToken
        {
            get => deviceToken;
            set
            {
                if (deviceToken == value) return;
                deviceToken = value;
                OnPropertyChanged();
            }
        }

        public ICommand RequestCommand { get; }

        public ICommand BackToProtectedModeCommand { get; }

        public bool CanBackToProtectedMode => isUnsafeConnection && isCertificateAvailable;

        internal void Initialize(IWcfService service)
        {
            hawkbitUrl = service.GetUnsafeConnectionUrl();
            controllerId = service.GetUnsafeConnectionDeviceId();
            if (string.IsNullOrEmpty(controllerId))
            {
                controllerId = service.GetSystemInfo().MachineGuid;
            }
            isUnsafeConnection = service.IsUnsafeConnection();
            isCertificateAvailable = service.IsCertificateAvailable();
            deviceToken = service.GetUnsafeConnectionToken();
        }

        private bool CanRequest(object _)
        {
            return !string.IsNullOrWhiteSpace(HawkbitUrl) && !string.IsNullOrWhiteSpace(ControllerId) && !string.IsNullOrEmpty(DeviceToken);
        }

        private async Task ExecuteRequestAsync()
        {
            await establishConnection(SetupCredentials, false);
        }

        private async Task<ResultOfstring> SetupCredentials(IWcfService service)
        {
            Result r = await service.SetupUnsafeConnectionAsync(HawkbitUrl, ControllerId, DeviceToken);
            return new ResultOfstring() { Success = r.Success, ErrorMessage = r.ErrorMessage, Value = string.Empty };
        }

        private async Task ExecuteBackToProtectedModeAsync()
        {
            await establishConnection(null, true);
        }
    }
}
