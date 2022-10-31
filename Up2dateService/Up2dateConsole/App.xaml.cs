using Microsoft.Toolkit.Uwp.Notifications;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using Up2dateConsole.Helpers;

namespace Up2dateConsole
{
    public partial class App
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            //Debugger.Launch(); // todo remove for production

            if (Up2dateConsole.Properties.Settings.Default.UpgradeFlag)
            {
                Up2dateConsole.Properties.Settings.Default.Upgrade();
                Up2dateConsole.Properties.Settings.Default.UpgradeFlag = false;
                Up2dateConsole.Properties.Settings.Default.Save();
            }

            if (CommandLineHelper.IsPresent(CommandLineHelper.StartUnelevatedCommand))
            {
                Process.Start("explorer.exe", Assembly.GetEntryAssembly().Location);
                Shutdown();
                return;
            }

            var suppressGuard = CommandLineHelper.IsPresent(CommandLineHelper.AllowSecondInstanceCommand);
            new SingleInstanceHelper(this, ShowMainWindow).Guard(suppressGuard);

            base.OnStartup(e);

            ToastNotificationManagerCompat.OnActivated += ToastNotificationManagerCompat_OnActivated;
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
