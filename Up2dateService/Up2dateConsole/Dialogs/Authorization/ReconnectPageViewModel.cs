using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Up2dateConsole.Helpers;
using Up2dateConsole.ServiceReference;

namespace Up2dateConsole.Dialogs.Authorization
{
    public class ReconnectPageViewModel : NotifyPropertyChanged
    {
        private Func<Func<IWcfService, Task<ResultOfstring>>, bool, Task> establishConnection;

        public ReconnectPageViewModel(Func<Func<IWcfService, Task<ResultOfstring>>, bool, Task> establishConnection)
        {
            this.establishConnection = establishConnection ?? throw new ArgumentNullException(nameof(establishConnection));

            RequestCommand = new RelayCommand(async (_) => await ExecuteRequestAsync());
        }

        public ICommand RequestCommand { get; }

        private async Task ExecuteRequestAsync()
        {
            await establishConnection(null, true);
        }
    }
}
