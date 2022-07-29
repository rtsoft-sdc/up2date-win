using System;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace Up2dateConsole.Helpers
{
    public class SingleInstanceHelper
    {
        private const string pipeName = "Up2dateConsoleSingleInstanceGuardPipe";

        private App app;
        private readonly Action onSecondInstance;
        private NamedPipeServerStream serverPipe;

        public SingleInstanceHelper(App app, Action onSecondInstance)
        {
            this.app = app ?? throw new ArgumentNullException(nameof(app));
            this.onSecondInstance = onSecondInstance;
        }

        private bool CheckInstance()
        {
            using (var clientPipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut))
            {
                try
                {
                    clientPipe.Connect(1000);
                }
                catch (TimeoutException)
                {
                }

                if (clientPipe.IsConnected)
                {
                    clientPipe.Close();
                    return true;
                }
            }
            return false;
        }

        public void Guard(bool suppressCheck)
        {
            if (!suppressCheck)
            {
                if (CheckInstance())
                {
                    app.Shutdown();
                    return;
                }
            }

            serverPipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 2);

            Task.Run(() =>
            {
                while (true)
                {
                    serverPipe.WaitForConnection();

                    onSecondInstance?.Invoke();

                    serverPipe.Disconnect();
                }
            });

            app.Exit += App_Exit;
        }

        private void App_Exit(object sender, System.Windows.ExitEventArgs e)
        {
            serverPipe.Close();
            serverPipe.Dispose();
        }
    }
}
