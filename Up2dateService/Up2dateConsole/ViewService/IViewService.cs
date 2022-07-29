using System;
using System.Windows;

namespace Up2dateConsole.ViewService
{
    public interface IViewService
    {
        /// <summary>
        /// Hides the main window
        /// </summary>
        void HideMainWindow();

        /// <summary>
        /// Shows the main window
        /// </summary>
        void ShowMainWindow();

        /// <summary>
        /// Registers dialog
        /// </summary>
        /// <param name="viewModelType">type of ViewModel for the dialog data context</param>
        /// <param name="viewType">type of dialog view</param>
        void RegisterDialog(Type viewModelType, Type viewType);

        /// <summary>
        /// Shows modal dialog registered for the type of supplied view model
        /// </summary>
        /// <param name="viewModel">View model that will be set as data context for the dialog</param>
        /// <returns>True if OK</returns>
        bool ShowDialog(DialogViewModelBase viewModel);

        /// <summary>
        /// Displays a message box
        /// </summary>
        /// <param name="text">A String that specifies the text to display</param>
        /// <param name="caption">A String that specifies the title bar caption to display</param>
        /// <param name="buttons">A MessageBoxButton value that specifies which button or buttons to display</param>
        /// <returns>MessageBoxResult</returns>
        MessageBoxResult ShowMessageBox(string text, string caption = null, MessageBoxButton buttons = MessageBoxButton.OK);

        /// <summary>
        /// Shows Win32 SaveFileDialog
        /// </summary>
        /// <param name="title">see SaveFileDialog parameters</param>
        /// <param name="filter">see SaveFileDialog parameters</param>
        /// <param name="defaultExt">see SaveFileDialog parameters</param>
        /// <param name="initialDirectory">see SaveFileDialog parameters</param>
        /// <returns>Full path of the selected file; empty string on cancel</returns>
        string ShowSaveDialog(string title, string filter, string defaultExt = null, string initialDirectory = null);

        /// <summary>
        /// Shows Win32 OpenFileDialog
        /// </summary>
        /// <param name="title">see SaveFileDialog parameters</param>
        /// <param name="filter">see SaveFileDialog parameters</param>
        /// <param name="defaultExt">see SaveFileDialog parameters</param>
        /// <param name="initialDirectory">see SaveFileDialog parameters</param>
        /// <returns>Full path of the selected file; empty string on cancel</returns>
        string ShowOpenDialog(string title, string filter, string defaultExt = null, string initialDirectory = null);
    }
}
