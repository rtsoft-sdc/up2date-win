using System;
using System.Diagnostics;
using System.Reflection;
using System.Security.Principal;
using System.Timers;
using System.Windows;
using Up2dateConsole.Helpers;
using Up2dateConsole.Helpers.InactivityMonitor;

namespace Up2dateConsole.Session
{
    public class Session : ISession
    {
        private readonly IInactivityMonitor inactivityMonitor;
        private readonly ISettings settings;

        public event EventHandler<EventArgs> ShuttingDown;

        public Session(IInactivityMonitor inactivityMonitor, ISettings settings)
        {
            this.inactivityMonitor = inactivityMonitor ?? throw new ArgumentNullException(nameof(inactivityMonitor));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));

            if (IsAdminMode)
            {
                inactivityMonitor.MonitorKeyboardEvents = true;
                inactivityMonitor.MonitorMouseEvents = true;
                inactivityMonitor.Elapsed += InactivityMonitor_Elapsed;
                OnSettingsUpdated();
            }
        }

        public void Shutdown()
        {
            IsShuttingDown = true;
            ShuttingDown?.Invoke(this, EventArgs.Empty);
            Application.Current?.Shutdown();
        }

        public void ToAdminMode()
        {
            if (IsAdminMode) return;

            using (Process p = new Process())
            {
                p.StartInfo.FileName = Assembly.GetEntryAssembly().Location;
                p.StartInfo.Arguments = CommandLineHelper.AllowSecondInstanceCommand + " " + CommandLineHelper.VisibleMainWindowCommand;
                p.StartInfo.Verb = "runas";

                bool started = false;
                try
                {
                    started = p.Start();
                }
                catch (Exception)
                {
                    started = false;
                }

                if (started)
                {
                    Shutdown();
                }
            }
        }

        public void ToUserMode()
        {
            if (!IsAdminMode) return;

            ThreadHelper.SafeInvoke(() =>
            {
                // Call shutdown first (it may be quite lengthy), so the new instance would not terminate itself due to "single instance check"
                Shutdown();
                Process.Start("explorer.exe", Assembly.GetEntryAssembly().Location);
            });
        }

        public bool IsAdminMode => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

        public bool IsShuttingDown { get; private set; }

        public void OnSettingsUpdated()
        {
            const int MillisecondsInSecond = 1000;
            const int MinTimeoutSec = 5;

            inactivityMonitor.Enabled = settings.LeaveAdminModeOnInactivity;
            var timeout = settings.LeaveAdminModeOnInactivityTimeout;
            if (timeout < MinTimeoutSec)
            {
                timeout = MinTimeoutSec;
            }
            inactivityMonitor.Interval = timeout * MillisecondsInSecond;
        }

        public void OnWindowClosing()
        {
            if (IsAdminMode && settings.LeaveAdminModeOnClose && !IsShuttingDown)
            {
                ToUserMode();
            }
        }

        public void OnWindowsSessionEnding()
        {
            IsShuttingDown = true;
            ShuttingDown?.Invoke(this, EventArgs.Empty);
        }

        private void InactivityMonitor_Elapsed(object sender, ElapsedEventArgs e)
        {
            ToUserMode();
            inactivityMonitor.Reset();
        }
    }
}
