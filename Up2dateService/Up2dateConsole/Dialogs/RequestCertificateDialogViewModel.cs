using System;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;
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
        private bool isInProgress;

        public RequestCertificateDialogViewModel(IViewService viewService, IWcfClientFactory wcfClientFactory, bool showExplanation)
        {
            this.viewService = viewService ?? throw new ArgumentNullException(nameof(viewService));
            this.wcfClientFactory = wcfClientFactory ?? throw new ArgumentNullException(nameof(wcfClientFactory));
            ShowExplanation = showExplanation;
            RequestCommand = new RelayCommand(async (_) => await ExecuteRequestAsync(), CanRequest);
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

        public bool IsInProgress
        {
            get => isInProgress;
            set
            {
                if (isInProgress == value) return;
                isInProgress = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsEnabled));
            }
        }

        public bool IsEnabled => !IsInProgress;

        public string DeviceId { get; private set; }

        public bool ShowExplanation { get; }

        private bool CanRequest(object _)
        {
            return !string.IsNullOrWhiteSpace(OneTimeKey);
        }

        private async Task ExecuteRequestAsync()
        {
            IWcfService service = null;
            string error = string.Empty;
            try
            {
                service = wcfClientFactory.CreateClient();
                ResultOfstring result = service.RequestCertificate(RemoveWhiteSpaces(OneTimeKey));
                if (!result.Success)
                {
                    error = result.ErrorMessage;
                }
                else
                {
                    DeviceId = result.Value;
                }
            }
            catch (Exception e)
            {
                error = e.Message;
            }
            finally
            {
                wcfClientFactory.CloseClient(service);
            }

            if (string.IsNullOrEmpty(error))
            {
                IsInProgress = true;
                await Task.Run(() => RestartService(20000));
                await Task.Delay(5000);
                Close(true);
            }
            else
            {
                viewService.ShowMessageBox($"Failed to acquire certificate.\n\n{error}");
            }
        }

        private static string RemoveWhiteSpaces(string str)
        {
            return new string(str.Where(c => !char.IsWhiteSpace(c)).ToArray());
        }

        public static void RestartService(int timeout)
        {
            ServiceController service = new ServiceController("Up2dateService");
            try
            {
                int started = Environment.TickCount;
                if (service.Status != ServiceControllerStatus.Stopped)
                {
                    service.Stop();
                }
                service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromMilliseconds(timeout));

                int elapsed = Environment.TickCount - started;
                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMilliseconds(timeout - elapsed));
            }
            catch
            {
            }
        }

    }
}
