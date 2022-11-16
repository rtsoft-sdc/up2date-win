using System;
using System.Threading.Tasks;
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

        public static async Task SafeInvokeAsync(Func<Task> actionAsync)
        {
            if (Application.Current.Dispatcher.CheckAccess())
            {
                await actionAsync();
            }
            else
            {
                await Application.Current.Dispatcher.Invoke(async () => await actionAsync());
            }
        }
    }
}
