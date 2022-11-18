using System.ComponentModel;
using System.Windows;
using Up2dateConsole.Dialogs.RequestCertificate;
using Up2dateConsole.Dialogs.Settings;
using Up2dateConsole.Helpers;
using Up2dateConsole.Helpers.InactivityMonitor;
using Up2dateConsole.ViewService;

namespace Up2dateConsole
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            IViewService viewService = new ViewService.ViewService();
            viewService.RegisterDialog(typeof(RequestCertificateDialogViewModel), typeof(RequestCertificateDialog));
            viewService.RegisterDialog(typeof(SettingsDialogViewModel), typeof(SettingsDialog));

            IWcfClientFactory wcfClientFactory = new WcfClientFactory();
            ISettings settings = new Settings();
            DataContext = new MainWindowViewModel(viewService, wcfClientFactory, new HookMonitor(false), settings, Application.Current.Shutdown);

            if (!CommandLineHelper.IsPresent(CommandLineHelper.VisibleMainWindowCommand))
            {
                Hide();
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            var viewModel = DataContext as MainWindowViewModel;
            viewModel?.OnWindowClosing();

            Hide();
            e.Cancel = true;
            base.OnClosing(e);
        }
    }
}
