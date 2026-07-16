using System.Windows;
using Chromassist.Presentation;
using Microsoft.Win32;

namespace Chromassist.App;

public sealed class ExecutablePicker : IExecutablePicker
{
    public string? PickExecutable()
    {
        var dialog = new OpenFileDialog
        {
            Title = "th18.exe 선택",
            Filter = "Touhou 18 executable (th18.exe)|th18.exe",
            CheckFileExists = true,
            Multiselect = false
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}

public sealed class UserNotificationService : IUserNotificationService
{
    public void ShowInformation(string title, string message) =>
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    public void ShowError(string title, string message) =>
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
}
