using Chromassist.Core.Games.Th18;
using Chromassist.Core.Imaging;
using Chromassist.Core.Presets;
using Chromassist.Core.Services;
using Chromassist.Core.Thcrap;
using Chromassist.Core.Tools;

namespace Chromassist.Tests;

public sealed class LocalInstallationSmokeTests
{
    [Fact]
    public async Task ValidateAndExtractConfiguredLocalInstallation()
    {
        var gameExecutable = Environment.GetEnvironmentVariable("CHROMASSIST_TH18_EXE");
        var thtkDirectory = Environment.GetEnvironmentVariable("CHROMASSIST_THTK_DIR");
        if (string.IsNullOrWhiteSpace(gameExecutable) || string.IsNullOrWhiteSpace(thtkDirectory))
        {
            return;
        }

        var locator = new Th18GameLocator();
        var installation = locator.FromExecutable(gameExecutable);
        Assert.NotNull(installation);
        var validation = await new Th18GameValidator(new ThcrapInspector()).ValidateAsync(installation);
        Assert.True(validation.CanGeneratePatch, string.Join(Environment.NewLine, validation.Diagnostics.Prepend(validation.Summary)));

        var work = Path.Combine(Path.GetTempPath(), $"chromassist-local-smoke-{Guid.NewGuid():N}");
        var extractor = new ThtkResourceExtractor(thtkDirectory, work);
        await using var extraction = await extractor.ExtractBulletTexturesAsync(validation);
        Assert.True(extraction.Success, string.Join(Environment.NewLine, extraction.Diagnostics));
        Assert.Equal(6, extraction.Textures.Count);
        foreach (var texture in extraction.Textures)
        {
            var image = PngCodec.Read(texture.FilePath);
            Assert.True(image.Width > 0);
            Assert.True(image.Height > 0);
        }

        if (string.Equals(Environment.GetEnvironmentVariable("CHROMASSIST_SMOKE_APPLY"), "1", StringComparison.Ordinal))
        {
            var result = await new LocalPatchBuilder().BuildAsync(
                validation,
                extraction,
                PresetCatalog.Create(Chromassist.Core.Models.PresetKind.Deutan, 50));
            Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
            Assert.NotNull(result.PatchDirectory);
            Assert.NotNull(result.RunConfigurationPath);
        }
    }
}
