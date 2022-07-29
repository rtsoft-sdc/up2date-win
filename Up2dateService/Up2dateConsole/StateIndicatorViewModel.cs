using FontAwesome.WPF;
using System;
using System.Windows.Media;
using Up2dateConsole.Helpers;

namespace Up2dateConsole
{
    public class StateIndicatorViewModel : NotifyPropertyChanged
    {
        private ServiceState state;
        private string info;
        private FontAwesomeIcon icon;
        private bool spin;
        private Brush color;

        private static readonly Brush greenBrush = new SolidColorBrush(Colors.Green);
        private static readonly Brush orangeBrush = new SolidColorBrush(Colors.Orange);
        private static readonly Brush grayBrush = new SolidColorBrush(Colors.Gray);
        private static readonly Brush redBrush = new SolidColorBrush(Colors.Red);
        private static readonly Brush blackBrush = new SolidColorBrush(Colors.Black);

        public StateIndicatorViewModel()
        {
            SetState(ServiceState.Unknown);
        }

        public ServiceState State
        {
            get => state;
            private set
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

        public FontAwesomeIcon Icon
        {
            get => icon;
            private set
            {
                if (icon == value) return;
                icon = value;
                OnPropertyChanged();
            }
        }

        public bool Spin
        {
            get => spin;
            private set
            {
                if (spin == value) return;
                spin = value;
                OnPropertyChanged();
            }
        }

        public Brush Color
        {
            get => color;
            private set
            {
                if (color == value) return;
                color = value;
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
            Spin = false;
            Info = state.ToString();
            switch (state)
            {
                case ServiceState.Error:
                    Icon = FontAwesomeIcon.ExclamationTriangle;
                    Color = redBrush;
                    break;
                case ServiceState.Active:
                    Icon = FontAwesomeIcon.CheckCircle;
                    Color = greenBrush;
                    break;
                case ServiceState.ServerUnaccessible:
                    Icon = FontAwesomeIcon.Key;
                    Color = orangeBrush;
                    break;
                case ServiceState.ClientUnaccessible:
                    Icon = FontAwesomeIcon.ExclamationTriangle;
                    Color = orangeBrush;
                    break;
                case ServiceState.Accessing:
                    Icon = FontAwesomeIcon.Spinner;
                    Color = grayBrush;
                    Spin = true;
                    break;
                case ServiceState.Unknown:
                    Icon = FontAwesomeIcon.Question;
                    Color = grayBrush;
                    break;
                default:
                    throw new InvalidOperationException($"unsupported state {state}");
            }
        }
    }
}
