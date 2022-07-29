using System;
using Up2dateConsole.Helpers;

namespace Up2dateConsole.ViewService
{
    public class DialogViewModelBase : NotifyPropertyChanged
    {
        public event EventHandler<bool> CloseDialog;

        protected void Close(bool result)
        {
            CloseDialog?.Invoke(this, result);
        }
    }
}
