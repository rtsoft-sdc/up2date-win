using System;
using Up2dateConsole.Helpers;

namespace Up2dateConsole.ViewService
{
    public class DialogViewModelBase : NotifyPropertyChanged, IDialogViewModel
    {
        public event EventHandler<bool> CloseDialog;

        public virtual bool OnClosing()
        {
            return true; // allows to close dialog
        }

        protected void Close(bool result)
        {
            CloseDialog?.Invoke(this, result);
        }
    }
}
