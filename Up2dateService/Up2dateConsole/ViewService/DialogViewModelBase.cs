using System;

namespace Up2dateConsole.ViewService
{
    public class DialogViewModelBase<TTextEnum> : WindowViewModelBase<TTextEnum>, IDialogViewModel where TTextEnum : Enum
    {
        public DialogViewModelBase(IViewService viewService) : base(viewService)
        {
        }

        public event EventHandler<bool> CloseDialog;

        protected void Close(bool result)
        {
            CloseDialog?.Invoke(this, result);
        }
    }
}
