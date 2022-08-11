using Up2dateConsole.Helpers;

namespace Up2dateConsole.StateIndicator
{
    public class StateIndicatorViewModel : NotifyPropertyChanged
    {
        private ServiceState state;
        private string info;

        public StateIndicatorViewModel()
        {
            SetState(ServiceState.Unknown);
        }

        public ServiceState State
        {
            get => state;
            set
            {
                if (state == value) return;
                state = value;
                OnPropertyChanged();
            }
        }

        public string Info
        {
            get => info;
            private set
            {
                if (info == value) return;
                info = value;
                OnPropertyChanged();
            }
        }

        public void SetInfo(string info)
        {
            Info = info ?? state.ToString();
        }

        public void SetState(ServiceState state)
        {
            State = state;
            Info = state.ToString();
        }
    }
}
