using System;
using Up2dateConsole.Helpers;

namespace Up2dateConsole.ViewService
{
    public class WindowViewModelBase<TTextEnum> : NotifyPropertyChanged where TTextEnum : Enum
    {
        protected readonly IViewService viewService;

        public WindowViewModelBase(IViewService viewService)
        {
            this.viewService = viewService ?? throw new ArgumentNullException(nameof(viewService));
        }

        protected string GetText(TTextEnum textEnum)
        {
            return viewService.GetTextFromResource(GetType(), textEnum);
        }
    }
}
