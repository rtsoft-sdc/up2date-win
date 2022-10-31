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
            var viewModel = DataContext as MainWindowViewModel;
            if (viewModel != null && viewModel.IsAdminMode)
            {
                viewModel.LeaveAdminModeCommand.Execute(this);
                return;
            }

            Hide();
            e.Cancel = true;
            base.OnClosing(e);
        }
    }
}
