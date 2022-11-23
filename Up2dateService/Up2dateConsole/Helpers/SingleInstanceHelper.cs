using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Threading.Tasks;
using Up2dateConsole.Session;

namespace Up2dateConsole.Helpers
{
    public class SingleInstanceHelper
    {
        private const string pipeName = "Up2dateConsoleSingleInstanceGuardPipe";

        private App app;
        private readonly Action onSecondInstance;
        private NamedPipeServerStream serverPipe;
        private readonly string fullPipeName;

        public SingleInstanceHelper(App app, Action onSecondInstance)
        {
            this.app = app ?? throw new ArgumentNullException(nameof(app));
            this.onSecondInstance = onSecondInstance;
            fullPipeName = $"{pipeName}_{Process.GetCurrentProcess().SessionId}";
        }

        public bool IsAnotherInstanceRunning()
        {
            var sessionId = Process.GetCurrentProcess().SessionId;
            using (var clientPipe = new NamedPipeClientStream(".", fullPipeName, PipeDirection.InOut))
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

        public void SetGuard()
        {
            serverPipe = new NamedPipeServerStream(fullPipeName, PipeDirection.InOut, 2);

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
