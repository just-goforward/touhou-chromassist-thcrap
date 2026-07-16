using Chromassist.Core.Infrastructure;
using Chromassist.Core.Models;
using Chromassist.Core.Services;

namespace Chromassist.Core.Tools;

public sealed class ThtkResourceExtractor : IResourceExtractor
{
    private const string VerifiedThdatSha256 = "EF494069A048238948E4B8769A955935DAAD6061870A99B8CB230B64BD84AF9B";
    private const string VerifiedThanmSha256 = "3182BE234BFBCC8480A883A52E84D7218F1AB7C28D5E75A2D98A4E498F6B9B8C";
    private readonly string _toolDirectory;
    private readonly string _workRoot;
    private readonly ProcessRunner _processRunner;

    public ThtkResourceExtractor(string toolDirectory, string workRoot, ProcessRunner? processRunner = null)
    {
        _toolDirectory = Path.GetFullPath(toolDirectory);
        _workRoot = Path.GetFullPath(workRoot);
        _processRunner = processRunner ?? new ProcessRunner();
    }

    public bool IsAvailable =>
        File.Exists(Path.Combine(_toolDirectory, "thdat.exe")) &&
        File.Exists(Path.Combine(_toolDirectory, "thanm.exe")) &&
        File.Exists(Path.Combine(_toolDirectory, "thtk.dll"));

    public async Task<ExtractionResult> ExtractBulletTexturesAsync(
        GameValidationResult validation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(validation);
        if (!validation.CanGeneratePatch)
        {
            return Failure("게임과 thcrap 검증이 완료되지 않아 추출을 시작하지 않았습니다.");
        }

        if (!IsAvailable)
        {
            return Failure("검증된 THTK 도구(thdat.exe, thanm.exe, thtk.dll)를 찾을 수 없습니다.");
        }

        var diagnostics = new List<string>();
        var thdat = Path.Combine(_toolDirectory, "thdat.exe");
        var thanm = Path.Combine(_toolDirectory, "thanm.exe");
        var thdatHash = await FileHash.Sha256Async(thdat, cancellationToken).ConfigureAwait(false);
        var thanmHash = await FileHash.Sha256Async(thanm, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(thdatHash, VerifiedThdatSha256, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(thanmHash, VerifiedThanmSha256, StringComparison.OrdinalIgnoreCase))
        {
            return Failure("THTK 실행 파일 해시가 검증된 2025-11-12 nightly 빌드와 다릅니다.", thdatHash, thanmHash);
        }

        Directory.CreateDirectory(_workRoot);
        var stagingDirectory = Path.Combine(_workRoot, $"extract-{Guid.NewGuid():N}");
        Directory.CreateDirectory(stagingDirectory);

        try
        {
            var archiveResult = await _processRunner.RunAsync(
                thdat,
                ["-x18", "-C", stagingDirectory, validation.Installation.DataArchivePath, "bullet.anm"],
                stagingDirectory,
                TimeSpan.FromSeconds(30),
                cancellationToken).ConfigureAwait(false);
            diagnostics.AddRange(CollectDiagnostics("thdat", archiveResult));
            var animationArchive = Path.Combine(stagingDirectory, "bullet.anm");
            if (!archiveResult.Success || !File.Exists(animationArchive))
            {
                return new ExtractionResult(false, stagingDirectory, [], diagnostics);
            }

            var textureResult = await _processRunner.RunAsync(
                thanm,
                ["-u", "-x18", "bullet.anm"],
                stagingDirectory,
                TimeSpan.FromSeconds(30),
                cancellationToken).ConfigureAwait(false);
            diagnostics.AddRange(CollectDiagnostics("thanm", textureResult));
            if (!textureResult.Success)
            {
                return new ExtractionResult(false, stagingDirectory, [], diagnostics);
            }

            var textures = TextureMappings
                .Select(mapping => new ExtractedTexture(
                    mapping.VirtualPath,
                    Path.Combine(stagingDirectory, mapping.OutputFile),
                    GameVisualRole.EnemyProjectile))
                .Where(texture => File.Exists(texture.FilePath))
                .ToArray();
            if (textures.Length != TextureMappings.Length)
            {
                var discovered = Directory.EnumerateFiles(stagingDirectory, "*.png", SearchOption.AllDirectories)
                    .Select(path => Path.GetRelativePath(stagingDirectory, path))
                    .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                diagnostics.Add($"예상한 bullet texture {TextureMappings.Length}개 중 {textures.Length}개만 추출되었습니다.");
                diagnostics.Add(discovered.Length == 0
                    ? "staging 폴더에서 PNG를 찾지 못했습니다."
                    : $"발견된 PNG: {string.Join(", ", discovered)}");
                return new ExtractionResult(false, stagingDirectory, textures, diagnostics);
            }

            diagnostics.Add("사용자 소유 th18.dat에서 bullet.anm과 6개 탄막 texture를 임시 폴더로 추출했습니다.");
            return new ExtractionResult(true, stagingDirectory, textures, diagnostics);
        }
        catch
        {
            if (Directory.Exists(stagingDirectory))
            {
                Directory.Delete(stagingDirectory, recursive: true);
            }

            throw;
        }
    }

    private static ExtractionResult Failure(string message, params string[] details) =>
        new(false, string.Empty, [], [message, .. details]);

    private static IEnumerable<string> CollectDiagnostics(string tool, ProcessResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            yield return $"{tool}: {result.StandardOutput.Trim()}";
        }

        if (!string.IsNullOrWhiteSpace(result.StandardError))
        {
            yield return $"{tool} stderr: {result.StandardError.Trim()}";
        }
    }

    private static readonly TextureMapping[] TextureMappings =
    [
        new("bullet/bullet1@bullet@0.png", "th18/bullet/bullet1@bullet@0.png"),
        new("bullet/bullet2@bullet@1.png", "th18/bullet/bullet2@bullet@1.png"),
        new("bullet/bullet3@bullet@2.png", "th18/bullet/bullet3@bullet@2.png"),
        new("bullet/bullet4@bullet@6.png", "th18/bullet/bullet4@bullet@6.png"),
        new("bullet/bullet5@bullet@7.png", "th18/bullet/bullet5@bullet@7.png"),
        new("bullet/bullet6@bullet@9.png", "th18/bullet/bullet6@bullet@9.png")
    ];

    private sealed record TextureMapping(string OutputFile, string VirtualPath);
}
