using System.ComponentModel;
using System.Windows;
using Up2dateConsole.Dialogs.RequestCertificate;
using Up2dateConsole.Dialogs.Settings;
using Up2dateConsole.Helpers;
using Up2dateConsole.Helpers.InactivityMonitor;
using Up2dateConsole.Session;
using Up2dateConsole.ViewService;

namespace Up2dateConsole
{
    public partial class MainWindow
    {
        ISession session;

        public MainWindow()
        {
            InitializeComponent();

            IViewService viewService = new ViewService.ViewService();
            viewService.RegisterDialog(typeof(RequestCertificateDialogViewModel), typeof(RequestCertificateDialog));
            viewService.RegisterDialog(typeof(SettingsDialogViewModel), typeof(SettingsDialog));

            IWcfClientFactory wcfClientFactory = new WcfClientFactory();
            ISettings settings = new Settings();
            session = new Session.Session(new HookMonitor(false), settings);
            DataContext = new MainWindowViewModel(viewService, wcfClientFactory, settings, session);

            if (!CommandLineHelper.IsPresent(CommandLineHelper.VisibleMainWindowCommand))
            {
                Hide();
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            session.OnWindowClosing();

            Hide();
            e.Cancel = true;
            base.OnClosing(e);
        }
    }
}
