using System.Collections.Generic;

namespace Up2dateConsole.Notifier
{
    public interface INotifier
    {
        void NotifyAboutChanges(IReadOnlyList<PackageItem> oldList, IReadOnlyList<PackageItem> newList);
    }
}