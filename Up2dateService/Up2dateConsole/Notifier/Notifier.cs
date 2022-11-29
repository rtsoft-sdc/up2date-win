using System.Collections.Generic;
using System;
using Up2dateConsole.ServiceReference;
using System.Linq;
using Microsoft.Toolkit.Uwp.Notifications;
using Up2dateConsole.ViewService;

namespace Up2dateConsole.Notifier
{
    public class Notifier : INotifier
    {
        private readonly IViewService viewService;

        public Notifier(IViewService viewService)
        {
            this.viewService = viewService ?? throw new ArgumentNullException(nameof(viewService));
        }

        public void NotifyAboutChanges(IReadOnlyList<PackageItem> oldList, IReadOnlyList<PackageItem> newList)
        {
            var changes = new List<(PackageStatus oldStatus, PackageStatus newStatus, PackageItem item)>();
            foreach (var newListItem in newList)
            {
                var oldStatus = oldList.FirstOrDefault(pi => pi.Package.Filepath.Equals(newListItem.Package.Filepath, StringComparison.InvariantCultureIgnoreCase))?.Package.Status ?? PackageStatus.Unavailable;
                if (oldStatus == newListItem.Package.Status) continue;
                changes.Add((oldStatus, newListItem.Package.Status, newListItem));
            }

            IList<PackageItem> SelectChangedItems(Func<PackageStatus, bool> oldStatusCondition, Func<PackageStatus, bool> newStatusCondition)
            {
                return changes.Where(p => oldStatusCondition(p.oldStatus) && newStatusCondition(p.newStatus)).Select(p => p.item).ToList();
            }

            var downloaded = SelectChangedItems(oldStatus => oldStatus == PackageStatus.Unavailable || oldStatus == PackageStatus.Downloading,
                                                newStatus => newStatus == PackageStatus.Downloaded);
            if (downloaded.Any())
            {
                TryShowToastNotification(Texts.NewPackageAvailable, downloaded.Select(p => GetProductNameAndVersion(p)).Distinct());
            }

            var waiting = SelectChangedItems(oldStatus => oldStatus != PackageStatus.WaitingForConfirmation,
                                                newStatus => newStatus == PackageStatus.WaitingForConfirmation);
            if (waiting.Any())
            {
                TryShowToastNotification(Texts.NewPackageWaitingForConfirmation, waiting.Select(p => GetProductNameAndVersion(p)).Distinct());
            }

            var waitingForced = SelectChangedItems(oldStatus => oldStatus != PackageStatus.WaitingForConfirmationForced,
                                                newStatus => newStatus == PackageStatus.WaitingForConfirmationForced);
            if (waitingForced.Any())
            {
                TryShowToastNotification(Texts.NewPackageWaitingForConfirmationForced, waitingForced.Select(p => GetProductNameAndVersion(p)).Distinct());
            }

            var failed = SelectChangedItems(oldStatus => oldStatus != PackageStatus.Failed,
                                            newStatus => newStatus == PackageStatus.Failed);
            if (failed.Any())
            {
                TryShowToastNotification(Texts.PackageInstallationFailed, failed.Select(p => $"{GetProductNameAndVersion(p)}\n({p.ExtraInfo})").Distinct());
            }

            var installed = SelectChangedItems(oldStatus => oldStatus != PackageStatus.Installed,
                                               newStatus => newStatus == PackageStatus.Installed);
            if (installed.Any())
            {
                TryShowToastNotification(Texts.NewPackageInstalled, installed.Select(p => GetProductNameAndVersion(p)).Distinct());
            }
        }

        private string GetProductNameAndVersion(PackageItem item)
        {
            return $"{item.ProductName} {item.Version}";
        }

        private void TryShowToastNotification(Texts titleId, IEnumerable<string> details = null)
        {
            string title = viewService.GetText(titleId);
            try
            {
                ToastContentBuilder builder = new ToastContentBuilder().AddText(title);
                if (details != null)
                {
                    foreach (string text in details)
                    {
                        builder.AddText(text);
                    }
                }
                builder.Show();
            }
            catch
            {
                // Just ignore if cannot pop up the tost
            }
        }

    }
}
