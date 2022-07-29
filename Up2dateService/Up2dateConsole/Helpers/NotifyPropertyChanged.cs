using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Up2dateConsole.Helpers
{
    public class NotifyPropertyChanged : INotifyPropertyChanged
    {
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
