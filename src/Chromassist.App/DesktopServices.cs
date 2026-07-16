using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Chromassist.Core.Imaging;
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

public sealed class ContextImagePicker : IContextImagePicker
{
    private const long MaximumInputBytes = 64L * 1024 * 1024;
    private const int MaximumPreviewDimension = 640;
    private const int MaximumSelectionCount = 12;

    public IReadOnlyList<ContextImageSelection> PickImages(string dialogTitle)
    {
        var dialog = new OpenFileDialog
        {
            Title = dialogTitle,
            Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp|PNG image|*.png|JPEG image|*.jpg;*.jpeg|Bitmap image|*.bmp",
            CheckFileExists = true,
            Multiselect = true
        };
        if (dialog.ShowDialog() != true)
        {
            return [];
        }

        if (dialog.FileNames.Length > MaximumSelectionCount)
        {
            throw new InvalidDataException($"한 번에 최대 {MaximumSelectionCount}개의 배경을 선택할 수 있습니다.");
        }

        return dialog.FileNames.Select(LoadImage).ToArray();
    }

    private static ContextImageSelection LoadImage(string path)
    {
        var file = new FileInfo(path);
        if (file.Length > MaximumInputBytes)
        {
            throw new InvalidDataException($"스크린샷 파일이 64 MiB 제한을 초과합니다: {file.Name}");
        }

        int sourceWidth;
        int sourceHeight;
        using (var stream = File.OpenRead(path))
        {
            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
            sourceWidth = decoder.Frames[0].PixelWidth;
            sourceHeight = decoder.Frames[0].PixelHeight;
        }

        if (sourceWidth <= 0 || sourceHeight <= 0)
        {
            throw new InvalidDataException($"이미지 크기를 읽을 수 없습니다: {file.Name}");
        }

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        if (sourceWidth >= sourceHeight)
        {
            bitmap.DecodePixelWidth = Math.Min(sourceWidth, MaximumPreviewDimension);
        }
        else
        {
            bitmap.DecodePixelHeight = Math.Min(sourceHeight, MaximumPreviewDimension);
        }

        bitmap.UriSource = new Uri(path, UriKind.Absolute);
        bitmap.EndInit();
        bitmap.Freeze();

        var converted = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
        converted.Freeze();
        var stride = checked(converted.PixelWidth * 4);
        var bgra = new byte[checked(stride * converted.PixelHeight)];
        converted.CopyPixels(bgra, stride, 0);
        var rgba = new byte[bgra.Length];
        for (var index = 0; index < bgra.Length; index += 4)
        {
            rgba[index] = bgra[index + 2];
            rgba[index + 1] = bgra[index + 1];
            rgba[index + 2] = bgra[index];
            rgba[index + 3] = bgra[index + 3];
        }

        return new ContextImageSelection(file.Name, new RgbaImage(converted.PixelWidth, converted.PixelHeight, rgba));
    }
}

public sealed class UserNotificationService : IUserNotificationService
{
    public void ShowInformation(string title, string message) =>
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    public void ShowError(string title, string message) =>
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
}
