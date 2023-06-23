using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Up2dateConsole.Helpers;
using Up2dateConsole.ServiceReference;

namespace Up2dateConsole.Dialogs.Authorization
{
    public class OneTimeTokenPageViewModel : NotifyPropertyChanged
    {
        private readonly Func<Func<IWcfService, Task<ResultOfstring>>, bool, Task> establishConnection;
        private string oneTimeKey;

        public OneTimeTokenPageViewModel(Func<Func<IWcfService, Task<ResultOfstring>>, bool, Task> establishConnection)
        {
            this.establishConnection = establishConnection ?? throw new ArgumentNullException(nameof(establishConnection));

            RequestCommand = new RelayCommand(async (_) => await ExecuteRequestAsync(), CanRequest);
        }

        public string OneTimeKey
        {
            get => oneTimeKey;
            set
            {
                if (oneTimeKey == value) return;
                oneTimeKey = value;
                OnPropertyChanged();
            }
        }

        public ICommand RequestCommand { get; }

        private bool CanRequest(object _)
        {
            return !string.IsNullOrWhiteSpace(OneTimeKey);
        }

        private async Task ExecuteRequestAsync()
        {
            await establishConnection(async service => await service.RequestCertificateAsync(RemoveWhiteSpaces(OneTimeKey)), true);
        }

        private static string RemoveWhiteSpaces(string str)
        {
            return new string(str.Where(c => !char.IsWhiteSpace(c)).ToArray());
        }

    }
}
