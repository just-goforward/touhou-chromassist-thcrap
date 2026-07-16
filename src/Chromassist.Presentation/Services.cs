using Chromassist.Core.Imaging;

namespace Chromassist.Presentation;

public interface IExecutablePicker
{
    string? PickExecutable();
}

public interface IContextImagePicker
{
    IReadOnlyList<ContextImageSelection> PickImages(string dialogTitle);
}

public sealed record ContextImageSelection(string DisplayName, RgbaImage Image);

public interface IUserNotificationService
{
    void ShowInformation(string title, string message);
    void ShowError(string title, string message);
}
