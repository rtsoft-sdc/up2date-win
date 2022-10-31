using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using Up2dateConsole.Dialogs;
using Up2dateConsole.Helpers;
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

            DataContext = new MainWindowViewModel(viewService, wcfClientFactory);

            if (!CommandLineHelper.IsPresent(CommandLineHelper.VisibleMainWindowCommand))
            {
                Hide();
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if ((DataContext as MainWindowViewModel).IsAdminMode)
            {
                Process.Start("explorer.exe", Assembly.GetEntryAssembly().Location);
                Application.Current.Shutdown();
                return;
            }

            Hide();
            e.Cancel = true;
            base.OnClosing(e);
        }
    }
}
