using System;
using System.Windows;

namespace Up2dateConsole.Helpers
{
    public class ThreadHelper
    {
        public static void SafeInvoke(Action action)
        {
            if (Application.Current.Dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() => action());
            }
        }
    }
}
