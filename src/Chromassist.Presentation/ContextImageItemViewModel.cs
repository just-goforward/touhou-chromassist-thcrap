using Chromassist.Core.Imaging;

namespace Chromassist.Presentation;

public sealed class ContextImageItemViewModel(ContextImageSelection selection)
{
    public string DisplayName { get; } = selection.DisplayName;

    public RgbaImage Image { get; } = selection.Image;
}
