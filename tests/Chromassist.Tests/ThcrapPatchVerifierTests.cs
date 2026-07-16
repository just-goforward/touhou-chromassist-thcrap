using Chromassist.Core.Services;
using Chromassist.Core.Thcrap;

namespace Chromassist.Tests;

public sealed class ThcrapPatchVerifierTests
{
    [Fact]
    public async Task DetectsGeneratedConfigurationAndEveryResolvedTexture()
    {
        using var fixture = new TemporaryDirectory();
        var logs = Path.Combine(fixture.Path, "logs");
        var patch = Path.Combine(fixture.Path, "repos", "local", "chromassist-th18-protan-custom");
        Directory.CreateDirectory(logs);
        Directory.CreateDirectory(patch);
        var expected = new[]
        {
            "th18/bullet/bullet1@bullet@0.png",
            "th18/bullet/bullet2@bullet@1.png"
        };
        File.WriteAllText(Path.Combine(logs, "thcrap_log.1.txt"),
            "Loading run configuration C:/thcrap/config/thpatch-ko-chromassist-protan-custom.js... found\n");
        File.WriteAllText(Path.Combine(logs, "thcrap_log.txt"), string.Join('\n', expected.Select(path =>
            $"(PNG) Resolving {path}...\n + {patch.Replace('\\', '/')}/{path}")));

        var result = await new ThcrapPatchVerifier().VerifyAsync(new PatchVerificationRequest(
            fixture.Path,
            Path.Combine(fixture.Path, "config", "thpatch-ko-chromassist-protan-custom.js"),
            patch,
            expected,
            DateTimeOffset.UtcNow.AddSeconds(-1),
            TimeSpan.FromSeconds(1)));

        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        Assert.True(result.RunConfigurationLoaded);
        Assert.True(result.PatchStackLoaded);
        Assert.True(result.AllExpectedFilesResolved);
        Assert.Equal(2, result.ResolvedFileCount);
        Assert.Empty(result.MissingVirtualPaths);
    }

    [Fact]
    public async Task ReportsPatchStackBeforeGameplayRequestsTextures()
    {
        using var fixture = new TemporaryDirectory();
        var logs = Path.Combine(fixture.Path, "logs");
        var patch = Path.Combine(fixture.Path, "repos", "local", "chromassist-th18-deutan-custom");
        Directory.CreateDirectory(logs);
        Directory.CreateDirectory(patch);
        File.WriteAllText(Path.Combine(logs, "thcrap_log.1.txt"),
            "Loading run configuration C:/thcrap/config/thpatch-ko-chromassist-deutan-custom.js... found\n");
        File.WriteAllText(Path.Combine(logs, "thcrap_log.txt"),
            $"Patches in the stack: thpatch, lang_ko, chromassist-th18-deutan-custom\nArchive: {patch.Replace('\\', '/')}\n");

        var result = await new ThcrapPatchVerifier().VerifyAsync(new PatchVerificationRequest(
            fixture.Path,
            Path.Combine(fixture.Path, "config", "thpatch-ko-chromassist-deutan-custom.js"),
            patch,
            ["th18/bullet/bullet1@bullet@0.png"],
            DateTimeOffset.UtcNow.AddSeconds(-1),
            TimeSpan.FromMilliseconds(50)));

        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        Assert.True(result.RunConfigurationLoaded);
        Assert.True(result.PatchStackLoaded);
        Assert.False(result.AllExpectedFilesResolved);
        Assert.Equal(0, result.ResolvedFileCount);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"chromassist-verifier-{Guid.NewGuid():N}");
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
