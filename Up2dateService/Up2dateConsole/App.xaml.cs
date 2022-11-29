using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Up2dateConsole.Dialogs.RequestCertificate;
using Up2dateConsole.Dialogs.Settings;
using Up2dateConsole.Helpers;
using Up2dateConsole.Helpers.InactivityMonitor;
using Up2dateConsole.Notifier;
using Up2dateConsole.Session;
using Up2dateConsole.ViewService;

namespace Up2dateConsole
{
    public partial class App
    {
        private static Logger logger = new Logger();

        protected override void OnStartup(StartupEventArgs e)
        {
            //Debugger.Launch(); // todo remove for production

            if (Up2dateConsole.Properties.Settings.Default.UpgradeFlag)
            {
                logger.Info("First start after installation or upgrade - upgrading settings.");

                Up2dateConsole.Properties.Settings.Default.Upgrade();
                Up2dateConsole.Properties.Settings.Default.UpgradeFlag = false;
                Up2dateConsole.Properties.Settings.Default.Save();
            }

            if (CommandLineHelper.IsPresent(CommandLineHelper.StartUnelevatedCommand))
            {
                logger.Info("Restarting unelevated...");

                Process.Start("explorer.exe", Assembly.GetEntryAssembly().Location);
                Shutdown();
                return;
            }

            var allowSecondInstance = CommandLineHelper.IsPresent(CommandLineHelper.AllowSecondInstanceCommand);

            var singleInstanceHelper = new SingleInstanceHelper(this, ShowMainWindow);
            if (singleInstanceHelper.IsAnotherInstanceRunning() && !allowSecondInstance)
            {
                Shutdown();
                return;
            }
            singleInstanceHelper.SetGuard();

            base.OnStartup(e);

            ToastNotificationManagerCompat.OnActivated += ToastNotificationManagerCompat_OnActivated;

            MainWindow = CreateMainWindow();

            if (CommandLineHelper.IsPresent(CommandLineHelper.VisibleMainWindowCommand))
            {
                MainWindow.Show();
            }

            SetupExceptionHandling();
        }

        private Window CreateMainWindow()
        {
            var mainWindow = new MainWindow();

            IViewService viewService = new ViewService.ViewService();
            viewService.RegisterDialog(typeof(RequestCertificateDialogViewModel), typeof(RequestCertificateDialog));
            viewService.RegisterDialog(typeof(SettingsDialogViewModel), typeof(SettingsDialog));

            IWcfClientFactory wcfClientFactory = new WcfClientFactory();
            ISettings settings = new Settings();
            ISession session = new Session.Session(new HookMonitor(false), settings);
            IProcessHelper processHelper = new ProcessHelper();
            INotifier notifier = new Notifier.Notifier(viewService);
            IServiceHelper serviceHelper = new ServiceHelper();
            mainWindow.DataContext = new MainWindowViewModel(viewService, wcfClientFactory, settings, session, processHelper, notifier, serviceHelper);

            mainWindow.Closing += (w, e) =>
            {
                session.OnWindowClosing();
                ((Window)w).Hide();
                e.Cancel = true;
            };

            SessionEnding += (w, e) =>
            {
                logger.Info($"Console exiting due to {e.ReasonSessionEnding}");
                session.OnWindowsSessionEnding();
            };

            string mode = session.IsAdminMode ? "Admin" : "User";
            logger.Info($"Console started in {mode} mode");

            return mainWindow;
        }

        private void SetupExceptionHandling()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                LogUnhandledException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");

            DispatcherUnhandledException += (s, e) =>
            {
                LogUnhandledException(e.Exception, "Application.Current.DispatcherUnhandledException");
                e.Handled = true;
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                LogUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");
                e.SetObserved();
            };
        }

        private void LogUnhandledException(Exception exception, string source)
        {
            logger.Error(exception, $"Unhandled exception ({source})");
        }

        private void ToastNotificationManagerCompat_OnActivated(ToastNotificationActivatedEventArgsCompat toastArgs)
        {
            ShowMainWindow();
        }

        private void ShowMainWindow()
        {
            ThreadHelper.SafeInvoke(() =>
            {
                MainWindow.Show();
                if (MainWindow.WindowState == WindowState.Minimized)
                {
                    MainWindow.WindowState = WindowState.Normal;
                }
                MainWindow.Activate();
            });
        }
    }
}
