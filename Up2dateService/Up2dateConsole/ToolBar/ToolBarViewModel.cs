using System.Windows.Input;
using Up2dateConsole.Helpers;
using Up2dateConsole.Session;

namespace Up2dateConsole.ToolBar
{
    public class ToolBarViewModel : NotifyPropertyChanged
    {
        private readonly ISession session;
        private bool canAcceptReject;

        public ToolBarViewModel(ISession session, ICommand refreshCommand, ICommand installCommand, ICommand acceptCommand,
            ICommand rejectCommand, ICommand requestCertificateCommand, ICommand settingsCommand)
        {
            this.session = session ?? throw new System.ArgumentNullException(nameof(session));

            RefreshCommand = refreshCommand;
            InstallCommand = installCommand;
            AcceptCommand = acceptCommand;
            RejectCommand = rejectCommand;
            RequestCertificateCommand = requestCertificateCommand;
            SettingsCommand = settingsCommand;
        }

        public bool CanAcceptReject
        {
            get => canAcceptReject;
            set
            {
                if (canAcceptReject == value) return;
                canAcceptReject = value;
                OnPropertyChanged();
            }
        }

        public bool IsAdminMode => session.IsAdminMode;

        public ICommand RefreshCommand { get; }
        public ICommand InstallCommand { get; }
        public ICommand AcceptCommand { get; }
        public ICommand RejectCommand { get; }
        public ICommand RequestCertificateCommand { get; }
        public ICommand SettingsCommand { get; }
    }
}
