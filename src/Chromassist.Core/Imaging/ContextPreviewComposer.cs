namespace Chromassist.Core.Imaging;

public sealed record SpriteRegion(int X, int Y, int Width, int Height);

public sealed record ContextPreviewResult(RgbaImage Image, int SpriteCount);

public static class ContextPreviewComposer
{
    public static ContextPreviewResult Compose(
        RgbaImage background,
        RgbaImage atlas,
        IReadOnlyList<SpriteRegion> regions)
    {
        ArgumentNullException.ThrowIfNull(background);
        ArgumentNullException.ThrowIfNull(atlas);
        ArgumentNullException.ThrowIfNull(regions);

        var output = background.Clone();
        if (regions.Count == 0)
        {
            return new ContextPreviewResult(output, 0);
        }

        var columns = Math.Min(4, regions.Count);
        var rows = (int)Math.Ceiling(regions.Count / (double)columns);
        var scale = Math.Clamp((int)Math.Round(background.Width / 640d), 1, 4);

        for (var index = 0; index < regions.Count; index++)
        {
            var region = regions[index];
            ValidateRegion(atlas, region);
            var column = index % columns;
            var row = index / columns;
            var centerX = (column + 1) * background.Width / (columns + 1);
            var centerY = (row + 1) * background.Height / (rows + 1);
            var left = centerX - region.Width * scale / 2;
            var top = centerY - region.Height * scale / 2;
            BlendRegion(output, atlas, region, left, top, scale);
        }

        return new ContextPreviewResult(output, regions.Count);
    }

    private static void ValidateRegion(RgbaImage atlas, SpriteRegion region)
    {
        if (region.X < 0 || region.Y < 0 || region.Width <= 0 || region.Height <= 0 ||
            region.X + region.Width > atlas.Width || region.Y + region.Height > atlas.Height)
        {
            throw new InvalidDataException("Representative enemy-projectile region is outside the local atlas.");
        }
    }

    private static void BlendRegion(
        RgbaImage destination,
        RgbaImage source,
        SpriteRegion region,
        int destinationX,
        int destinationY,
        int scale)
    {
        for (var sourceY = 0; sourceY < region.Height; sourceY++)
        {
            for (var sourceX = 0; sourceX < region.Width; sourceX++)
            {
                var sourceOffset = ((region.Y + sourceY) * source.Width + region.X + sourceX) * 4;
                var sourceAlpha = source.Pixels[sourceOffset + 3] / 255d;
                if (sourceAlpha <= 0)
                {
                    continue;
                }

                for (var offsetY = 0; offsetY < scale; offsetY++)
                {
                    var targetY = destinationY + sourceY * scale + offsetY;
                    if (targetY < 0 || targetY >= destination.Height)
                    {
                        continue;
                    }

                    for (var offsetX = 0; offsetX < scale; offsetX++)
                    {
                        var targetX = destinationX + sourceX * scale + offsetX;
                        if (targetX < 0 || targetX >= destination.Width)
                        {
                            continue;
                        }

                        var destinationOffset = (targetY * destination.Width + targetX) * 4;
                        BlendPixel(destination.Pixels, destinationOffset, source.Pixels, sourceOffset, sourceAlpha);
                    }
                }
            }
        }
    }

    private static void BlendPixel(
        byte[] destination,
        int destinationOffset,
        byte[] source,
        int sourceOffset,
        double sourceAlpha)
    {
        var destinationAlpha = destination[destinationOffset + 3] / 255d;
        var outputAlpha = sourceAlpha + destinationAlpha * (1 - sourceAlpha);
        if (outputAlpha <= 0)
        {
            return;
        }

        for (var channel = 0; channel < 3; channel++)
        {
            var value = (source[sourceOffset + channel] * sourceAlpha +
                destination[destinationOffset + channel] * destinationAlpha * (1 - sourceAlpha)) / outputAlpha;
            destination[destinationOffset + channel] = (byte)Math.Clamp((int)Math.Round(value), 0, 255);
        }

        destination[destinationOffset + 3] = (byte)Math.Clamp((int)Math.Round(outputAlpha * 255), 0, 255);
    }
}
