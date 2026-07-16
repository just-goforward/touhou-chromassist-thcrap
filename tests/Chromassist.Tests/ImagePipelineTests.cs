using Chromassist.Core.Imaging;
using Chromassist.Core.Presets;

namespace Chromassist.Tests;

public sealed class ImagePipelineTests
{
    [Fact]
    public void PngRoundTripPreservesRgbaBytes()
    {
        var source = CreateFixture();
        using var stream = new MemoryStream();

        PngCodec.Write(stream, source);
        stream.Position = 0;
        var decoded = PngCodec.Read(stream);

        Assert.Equal(source.Width, decoded.Width);
        Assert.Equal(source.Height, decoded.Height);
        Assert.Equal(source.Pixels, decoded.Pixels);
    }

    [Fact]
    public void PresetTransformationPreservesProtectedInvariants()
    {
        var source = CreateFixture();
        var transformed = PresetTransformer.Transform(source, PresetCatalog.Create(Chromassist.Core.Models.PresetKind.Deutan, 50));
        var invariant = ImageInvariantChecker.Compare(source, transformed);

        Assert.True(invariant.Success);
        Assert.Equal(source.Pixels[0..4], transformed.Pixels[0..4]);
        Assert.Equal(source.Pixels.Where((_, index) => index % 4 == 3), transformed.Pixels.Where((_, index) => index % 4 == 3));
        Assert.NotEqual(source.Pixels, transformed.Pixels);
    }

    [Fact]
    public void SameImageAndPresetProduceIdenticalPng()
    {
        var source = CreateFixture();
        var preset = PresetCatalog.Create(Chromassist.Core.Models.PresetKind.Protan, 50);

        Assert.Equal(Encode(PresetTransformer.Transform(source, preset)), Encode(PresetTransformer.Transform(source, preset)));
    }

    [Fact]
    public void ZeroStrengthProducesOriginalPixelsAndHundredIsStrongerThanFifty()
    {
        var source = CreateFixture();
        var zero = PresetTransformer.Transform(source, PresetCatalog.Create(Chromassist.Core.Models.PresetKind.Protan, 0));
        var fifty = PresetTransformer.Transform(source, PresetCatalog.Create(Chromassist.Core.Models.PresetKind.Protan, 50));
        var hundred = PresetTransformer.Transform(source, PresetCatalog.Create(Chromassist.Core.Models.PresetKind.Protan, 100));

        Assert.Equal(source.Pixels, zero.Pixels);
        Assert.True(RgbDifference(source, hundred) > RgbDifference(source, fifty));
    }

    [Fact]
    public void PresetKeepsFractionalSliderStrength()
    {
        var preset = PresetCatalog.Create(Chromassist.Core.Models.PresetKind.Protan, 12.5);

        Assert.Equal(12.5, preset.StrengthPercent);
        Assert.Equal(7, preset.PrimaryHueShiftDegrees);
        Assert.Equal(6.5, preset.SecondaryHueShiftDegrees);
    }

    [Fact]
    public void EnemyProjectileTransformationPreservesNeutralPixelsExactly()
    {
        var source = new RgbaImage(3, 1,
        [
            128, 128, 128, 255,
            255, 255, 255, 255,
            230, 20, 30, 255
        ]);

        var transformed = PresetTransformer.Transform(
            source,
            PresetCatalog.Create(Chromassist.Core.Models.PresetKind.Protan, 50));

        Assert.Equal(source.Pixels[0..8], transformed.Pixels[0..8]);
        Assert.NotEqual(source.Pixels[8..12], transformed.Pixels[8..12]);
    }

    [Fact]
    public void ContextPreviewCompositesOnlyProvidedEnemyProjectileRegions()
    {
        var background = SolidImage(100, 80, 20, 30, 40, 255);
        var atlas = SolidImage(32, 16, 0, 0, 0, 0);
        FillRectangle(atlas, 0, 0, 16, 16, 230, 30, 40, 255);
        FillRectangle(atlas, 16, 0, 16, 16, 30, 220, 70, 255);

        var result = ContextPreviewComposer.Compose(
            background,
            atlas,
            [new SpriteRegion(0, 0, 16, 16), new SpriteRegion(16, 0, 16, 16)]);

        Assert.Equal(2, result.SpriteCount);
        Assert.Equal(background.Width, result.Image.Width);
        Assert.Equal(background.Height, result.Image.Height);
        Assert.NotEqual(background.Pixels, result.Image.Pixels);
        Assert.All(result.Image.Pixels.Where((_, index) => index % 4 == 3), alpha => Assert.Equal(255, alpha));
        Assert.Equal(background.Pixels[0..4], result.Image.Pixels[0..4]);
    }

    [Fact]
    public void DecoderRejectsUnsupportedInput()
    {
        using var stream = new MemoryStream([1, 2, 3, 4, 5, 6, 7, 8]);
        Assert.Throws<InvalidDataException>(() => PngCodec.Read(stream));
    }

    private static RgbaImage CreateFixture() => new(3, 2,
    [
        12, 34, 56, 0,
        230, 20, 30, 255,
        20, 220, 40, 180,
        30, 50, 230, 255,
        240, 210, 30, 64,
        180, 40, 190, 255
    ]);

    private static byte[] Encode(RgbaImage image)
    {
        using var stream = new MemoryStream();
        PngCodec.Write(stream, image);
        return stream.ToArray();
    }

    private static int RgbDifference(RgbaImage first, RgbaImage second) =>
        first.Pixels.Select((value, index) => index % 4 == 3 ? 0 : Math.Abs(value - second.Pixels[index])).Sum();

    private static RgbaImage SolidImage(int width, int height, byte red, byte green, byte blue, byte alpha)
    {
        var pixels = new byte[width * height * 4];
        for (var index = 0; index < pixels.Length; index += 4)
        {
            pixels[index] = red;
            pixels[index + 1] = green;
            pixels[index + 2] = blue;
            pixels[index + 3] = alpha;
        }

        return new RgbaImage(width, height, pixels);
    }

    private static void FillRectangle(
        RgbaImage image,
        int x,
        int y,
        int width,
        int height,
        byte red,
        byte green,
        byte blue,
        byte alpha)
    {
        for (var row = y; row < y + height; row++)
        {
            for (var column = x; column < x + width; column++)
            {
                var offset = (row * image.Width + column) * 4;
                image.Pixels[offset] = red;
                image.Pixels[offset + 1] = green;
                image.Pixels[offset + 2] = blue;
                image.Pixels[offset + 3] = alpha;
            }
        }
    }
}
