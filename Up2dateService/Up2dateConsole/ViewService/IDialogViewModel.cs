using System;

namespace Up2dateConsole.ViewService
{
    public interface IDialogViewModel
    {
        event EventHandler<bool> CloseDialog;
    }
}
