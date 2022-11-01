using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Up2dateConsole.Helpers;
using Up2dateConsole.ServiceReference;
using Up2dateConsole.ViewService;

namespace Up2dateConsole.Dialogs.Settings
{
    public class SettingsDialogViewModel : DialogViewModelBase
    {
        private readonly IViewService viewService;
        private readonly IWcfClientFactory wcfClientFactory;
        private bool isServiceAvailable;
        private ServerConnectionTabViewModel serverConnectionTab;
        private InstallationPolicyTabViewModel installationPolicyTab;
        private ConsoleSecurityTabViewModel consoleSecurityTab;


        public SettingsDialogViewModel(IViewService viewService, IWcfClientFactory wcfClientFactory, bool isServiceAvailable)
        {
            this.viewService = viewService ?? throw new ArgumentNullException(nameof(viewService));
            this.wcfClientFactory = wcfClientFactory ?? throw new ArgumentNullException(nameof(wcfClientFactory));
            this.isServiceAvailable = isServiceAvailable;

            OkCommand = new RelayCommand(ExecuteOk, CanOk);

            if (isServiceAvailable)
            {
                serverConnectionTab = new ServerConnectionTabViewModel(viewService.GetText(Texts.ServerConnection));
                installationPolicyTab = new InstallationPolicyTabViewModel(viewService.GetText(Texts.InstallationPolicy), viewService, wcfClientFactory);
                Tabs.Add(serverConnectionTab);
                Tabs.Add(installationPolicyTab);
            }
            consoleSecurityTab = new ConsoleSecurityTabViewModel(viewService.GetText(Texts.ConsoleSecurity));
            Tabs.Add(consoleSecurityTab);

            IsInitialized = Initialize();
        }

        public ObservableCollection<object> Tabs { get; } = new ObservableCollection<object>();

        public bool IsInitialized { get; }

        public ICommand OkCommand { get; }

        private bool CanOk(object obj)
        {
            var isValid = consoleSecurityTab.IsValid;
            if (isServiceAvailable)
            {
                isValid = isValid && serverConnectionTab.IsValid && installationPolicyTab.IsValid;
            }
            return isValid;
        }

        private void ExecuteOk(object obj)
        {
            if (!consoleSecurityTab.Apply()) return;

            if (!isServiceAvailable)
            {
                Close(true);
                return;
            }

            IWcfService service = null;
            string error = string.Empty;
            try
            {
                service = wcfClientFactory.CreateClient();

                if (!serverConnectionTab.Apply(service)) return;
                if (!installationPolicyTab.Apply(service)) return;
            }
            catch (Exception e)
            {
                error = e.Message;
            }
            finally
            {
                wcfClientFactory.CloseClient(service);
            }

            if (!string.IsNullOrEmpty(error))
            {
                viewService.ShowMessageBox(Texts.ServiceAccessError);
                Close(false);
            }

            Close(true);
        }

        private bool Initialize()
        {
            consoleSecurityTab.Initialize();
            if (!isServiceAvailable) return true;

            IWcfService service = null;
            string error = string.Empty;
            try
            {
                service = wcfClientFactory.CreateClient();
                serverConnectionTab.Initialize(service);
                installationPolicyTab.Initialize(service);
            }
            catch (Exception e)
            {
                error = e.Message;
            }
            finally
            {
                wcfClientFactory.CloseClient(service);
            }

            if (!string.IsNullOrEmpty(error))
            {
                viewService.ShowMessageBox(Texts.ServiceAccessError);
                return false;
            }

            return true;
        }
    }
}
