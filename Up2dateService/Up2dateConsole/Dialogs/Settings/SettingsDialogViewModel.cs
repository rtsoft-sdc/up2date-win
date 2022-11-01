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

        public SettingsDialogViewModel(IViewService viewService, IWcfClientFactory wcfClientFactory, bool isServiceAvailable)
        {
            this.viewService = viewService ?? throw new ArgumentNullException(nameof(viewService));
            this.wcfClientFactory = wcfClientFactory ?? throw new ArgumentNullException(nameof(wcfClientFactory));
            this.isServiceAvailable = isServiceAvailable;

            OkCommand = new RelayCommand(ExecuteOk, CanOk);

            if (isServiceAvailable)
            {
                ServerConnectionTab = new ServerConnectionTabViewModel(viewService.GetText(Texts.ServerConnection));
                InstallationPolicyTab = new InstallationPolicyTabViewModel(viewService.GetText(Texts.InstallationPolicy), viewService, wcfClientFactory);
                Tabs.Add(ServerConnectionTab);
                Tabs.Add(InstallationPolicyTab);
            }
            ConsoleSecurityTab = new ConsoleSecurityTabViewModel(viewService.GetText(Texts.ConsoleSecurity));
            Tabs.Add(ConsoleSecurityTab);

            IsInitialized = Initialize();
        }

        public ObservableCollection<object> Tabs { get; } = new ObservableCollection<object>();

        public ServerConnectionTabViewModel ServerConnectionTab { get; }

        public InstallationPolicyTabViewModel InstallationPolicyTab { get; }

        public ConsoleSecurityTabViewModel ConsoleSecurityTab { get; }

        public bool IsInitialized { get; }

        public ICommand OkCommand { get; }

        private bool CanOk(object obj)
        {
            var isValid = ConsoleSecurityTab.IsValid;
            if (isServiceAvailable)
            {
                isValid = isValid && ServerConnectionTab.IsValid && InstallationPolicyTab.IsValid;
            }
            return isValid;
        }

        private void ExecuteOk(object obj)
        {
            if (!ConsoleSecurityTab.Apply()) return;

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

                if (!ServerConnectionTab.Apply(service)) return;
                if (!InstallationPolicyTab.Apply(service)) return;
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
            ConsoleSecurityTab.Initialize();
            if (!isServiceAvailable) return true;

            IWcfService service = null;
            string error = string.Empty;
            try
            {
                service = wcfClientFactory.CreateClient();
                ServerConnectionTab.Initialize(service);
                InstallationPolicyTab.Initialize(service);
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
