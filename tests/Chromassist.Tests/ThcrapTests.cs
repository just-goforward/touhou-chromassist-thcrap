using System.Text.Json;
using Chromassist.Core.Imaging;
using Chromassist.Core.Models;
using Chromassist.Core.Presets;
using Chromassist.Core.Services;
using Chromassist.Core.Thcrap;

namespace Chromassist.Tests;

public sealed class ThcrapTests
{
    [Fact]
    public void InspectorAcceptsKnownVersionAndKoreanStack()
    {
        using var fixture = new TemporaryDirectory();
        var installation = CreateThcrapInstallation(fixture.Path);

        var result = new ThcrapInspector().Inspect(installation, "2025-12-02");

        Assert.True(result.IsCompatible);
        Assert.Equal("2025-12-02", result.Version);
        Assert.Equal(4, result.PatchArchives.Count);
    }

    [Fact]
    public async Task PatchBuilderWritesLocalPatchWithoutChangingSourceConfig()
    {
        using var fixture = new TemporaryDirectory();
        var installation = CreateThcrapInstallation(fixture.Path);
        var inspector = new ThcrapInspector().Inspect(installation, "2025-12-02");
        var sourceConfig = await File.ReadAllTextAsync(inspector.RunConfigurationPath!);
        var assetSet = new KnownAssetSet("fixture", "th18", "fixture", "synthetic", "exe", "dat", "2025-12-02", ["bullet1.png"]);
        var validation = new GameValidationResult(
            installation,
            ValidationStatus.Supported,
            "fixture",
            assetSet,
            "EXE",
            "DAT",
            inspector,
            []);

        var extractionRoot = Path.Combine(fixture.Path, "extraction");
        Directory.CreateDirectory(extractionRoot);
        var texturePath = Path.Combine(extractionRoot, "bullet.png");
        PngCodec.Write(texturePath, new RgbaImage(2, 1, [10, 20, 30, 0, 220, 40, 30, 255]));
        await using var extraction = new ExtractionResult(
            true,
            extractionRoot,
            [new ExtractedTexture("th18/bullet1@bullet@0.png", texturePath)],
            []);

        var result = await new LocalPatchBuilder().BuildAsync(
            validation,
            extraction,
            PresetCatalog.Create(PresetKind.Protan, 50));

        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        Assert.Equal(sourceConfig, await File.ReadAllTextAsync(inspector.RunConfigurationPath!));
        Assert.True(File.Exists(Path.Combine(result.PatchDirectory!, "patch.js")));
        Assert.True(File.Exists(Path.Combine(result.PatchDirectory!, "manifest.json")));
        Assert.True(File.Exists(Path.Combine(result.PatchDirectory!, "th18", "bullet1@bullet@0.png")));
        var repositoryPath = Path.Combine(installation.ThcrapDirectory!, "repos", "chromassist", "repo.js");
        Assert.True(File.Exists(repositoryPath));
        using var repository = JsonDocument.Parse(await File.ReadAllTextAsync(repositoryPath));
        Assert.Equal("TH Chromassist (local)", repository.RootElement.GetProperty("title").GetString());
        Assert.True(repository.RootElement.GetProperty("patches").TryGetProperty("th18-protan-custom", out _));
        using var generatedConfig = JsonDocument.Parse(await File.ReadAllTextAsync(result.RunConfigurationPath!));
        Assert.Equal(5, generatedConfig.RootElement.GetProperty("patches").GetArrayLength());
        Assert.Equal(
            "repos/chromassist/th18-protan-custom/",
            generatedConfig.RootElement.GetProperty("patches")[4].GetProperty("archive").GetString());
        Assert.True(result.Files.Single().AlphaPreserved);
        Assert.True(result.Files.Single().TransparentPixelsPreserved);
    }

    private static GameInstallation CreateThcrapInstallation(string root)
    {
        var thcrap = Path.Combine(root, "thcrap");
        Directory.CreateDirectory(Path.Combine(thcrap, "config"));
        Directory.CreateDirectory(Path.Combine(thcrap, "logs"));
        Directory.CreateDirectory(Path.Combine(thcrap, "bin"));
        File.WriteAllBytes(Path.Combine(root, "th18.exe"), [1]);
        File.WriteAllBytes(Path.Combine(root, "th18.dat"), [2]);
        File.WriteAllBytes(Path.Combine(thcrap, "thcrap.exe"), [3]);
        File.WriteAllBytes(Path.Combine(thcrap, "bin", "thcrap_loader.exe"), [3]);
        File.WriteAllText(Path.Combine(thcrap, "logs", "thcrap_log.txt"), "Branch: stable\nVersion: 2025-12-02\n");
        File.WriteAllText(Path.Combine(thcrap, "config", "thpatch-ko.js"), """
            {
              "console": false,
              "patches": [
                { "archive": "repos/nmlgc/base_tsa/" },
                { "archive": "repos/nmlgc/base_tasofro/" },
                { "archive": "repos/nmlgc/script_latin/" },
                { "archive": "repos/thpatch/lang_ko/" }
              ]
            }
            """);
        return new GameInstallation("th18", "fixture", root, Path.Combine(root, "th18.exe"), Path.Combine(root, "th18.dat"), thcrap, InstallationSource.Manual);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"chromassist-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
