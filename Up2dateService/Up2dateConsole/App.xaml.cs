using Microsoft.Toolkit.Uwp.Notifications;
using NLog;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Up2dateConsole.Helpers;

namespace Up2dateConsole
{
    public partial class App
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        protected override void OnStartup(StartupEventArgs e)
        {
            //Debugger.Launch(); // todo remove for production
            logger.Info("Console start.");

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

            var suppressGuard = CommandLineHelper.IsPresent(CommandLineHelper.AllowSecondInstanceCommand);
            new SingleInstanceHelper(this, ShowMainWindow).Guard(suppressGuard);

            base.OnStartup(e);

            SetupExceptionHandling();

            ToastNotificationManagerCompat.OnActivated += ToastNotificationManagerCompat_OnActivated;
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
            string message = $"Unhandled exception ({source})";
            try
            {
                AssemblyName assemblyName = Assembly.GetExecutingAssembly().GetName();
                message = string.Format("Unhandled exception in {0} v{1}", assemblyName.Name, assemblyName.Version);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception in LogUnhandledException");
            }
            finally
            {
                logger.Error(exception, message);
            }
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
