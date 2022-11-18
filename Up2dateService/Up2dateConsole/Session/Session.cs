using System;
using System.Diagnostics;
using System.Reflection;
using System.Security.Principal;
using System.Windows;
using Up2dateConsole.Helpers;

namespace Up2dateConsole.Session
{
    public class Session : ISession
    {
        public event EventHandler<EventArgs> ShuttingDown;

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
                Process.Start("explorer.exe", Assembly.GetEntryAssembly().Location);
                Shutdown();
            });
        }

        public bool IsAdminMode => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

        public bool IsShuttingDown { get; private set; }
    }
}
