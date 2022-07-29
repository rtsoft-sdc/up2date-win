using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Windows;

namespace Up2dateConsole.ViewService
{
    public class ViewService : IViewService
    {
        private Dictionary<Type, Type> registeredDialogs = new Dictionary<Type, Type>();
        private Stack<Window> nestedWindows = new Stack<Window>();
        private Window TopWindow => nestedWindows.Peek();
        private Window ActiveDialog => nestedWindows.Count > 1 ? nestedWindows.Peek() : null;

        public ViewService()
        {
            nestedWindows.Push(Application.Current.MainWindow);
        }

        public void HideMainWindow()
        {
            TopWindow.Hide();
        }

        public void ShowMainWindow()
        {
            TopWindow.Show();
            TopWindow.WindowState = WindowState.Normal;
            TopWindow.Activate();
        }

        public MessageBoxResult ShowMessageBox(string text, string caption = null, MessageBoxButton buttons = MessageBoxButton.OK)
        {
            caption = caption ?? TopWindow?.Title;
            return MessageBox.Show(TopWindow, text, caption, buttons);
        }

        public string ShowSaveDialog(string title, string filter, string defaultExt = null, string initialDirectory = null)
        {
            var dlg = new SaveFileDialog
            {
                Title = title,
                Filter = filter,
                DefaultExt = defaultExt,
                InitialDirectory = initialDirectory
            };
            return dlg.ShowDialog() == true ? dlg.FileName : string.Empty;
        }

        public string ShowOpenDialog(string title, string filter, string defaultExt = null, string initialDirectory = null)
        {
            var dlg = new OpenFileDialog
            {
                Title = title,
                Filter = filter,
                DefaultExt = defaultExt,
                InitialDirectory = initialDirectory
            };
            return dlg.ShowDialog() == true ? dlg.FileName : string.Empty;
        }

        public void RegisterDialog(Type viewModelType, Type viewType)
        {
            if (!viewModelType.IsSubclassOf(typeof(DialogViewModelBase))) throw new ArgumentException("Dialog viewmodel must be subclassed from DialogViewModelBase.", nameof(viewModelType));
            if (!viewType.IsSubclassOf(typeof(Window))) throw new ArgumentException("Dialog view must be subclassed from Window.", nameof(viewType));

            registeredDialogs[viewModelType] = viewType;
        }

        public bool ShowDialog(DialogViewModelBase viewModel)
        {
            if (viewModel is null) throw new ArgumentNullException(nameof(viewModel));

            var viewModelType = viewModel.GetType();
            if (!registeredDialogs.ContainsKey(viewModelType)) throw new ArgumentException("Dialog is not registered.", nameof(viewModel));

            var dlg = (Window)Activator.CreateInstance(registeredDialogs[viewModelType]);
            dlg.DataContext = viewModel;
            dlg.Owner = TopWindow.IsVisible ? TopWindow : null;
            dlg.Closed += ActiveDialog_Closed;

            viewModel.CloseDialog += ViewModel_CloseDialog;
            nestedWindows.Push(dlg);

            return dlg.ShowDialog() == true;
        }

        private void ActiveDialog_Closed(object sender, EventArgs e)
        {
            var dialog = (Window)sender;
            ((DialogViewModelBase)dialog.DataContext).CloseDialog -= ViewModel_CloseDialog;
            dialog.Closed -= ActiveDialog_Closed;

            nestedWindows.Pop();
        }

        private void ViewModel_CloseDialog(object sender, bool result)
        {
            if (ActiveDialog is null) return;

            ActiveDialog.DialogResult = result;
        }
    }
}
