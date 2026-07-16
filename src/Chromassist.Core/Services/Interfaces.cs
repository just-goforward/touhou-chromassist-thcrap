using Chromassist.Core.Models;

namespace Chromassist.Core.Services;

public interface IGameLocator
{
    Task<IReadOnlyList<GameInstallation>> FindInstalledGamesAsync(CancellationToken cancellationToken = default);
    GameInstallation? FromExecutable(string executablePath);
}

public interface IGameValidator
{
    Task<GameValidationResult> ValidateAsync(GameInstallation installation, CancellationToken cancellationToken = default);
}

public interface IResourceExtractor
{
    bool IsAvailable { get; }
    Task<ExtractionResult> ExtractBulletTexturesAsync(GameValidationResult validation, CancellationToken cancellationToken = default);
}

public interface IPatchBuilder
{
    Task<PatchBuildResult> BuildAsync(
        GameValidationResult validation,
        ExtractionResult extraction,
        ColorPreset preset,
        CancellationToken cancellationToken = default);
}

public interface IUpdateCheckService
{
    Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default);
}

public interface IThcrapSetupLauncher
{
    Task<SetupLaunchResult> LaunchAsync(
        string thcrapDirectory,
        CancellationToken cancellationToken = default);
}

public interface IPatchApplicationVerifier
{
    Task<PatchVerificationResult> VerifyAsync(
        PatchVerificationRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record UpdateCheckResult(bool IsConfigured, bool IsUpdateAvailable, string Summary, Uri? ReleaseUri);

public sealed record SetupLaunchResult(int ProcessId, DateTimeOffset StartedAtUtc, string ExecutablePath);

public sealed record PatchVerificationRequest(
    string ThcrapDirectory,
    string RunConfigurationPath,
    string PatchDirectory,
    IReadOnlyList<string> ExpectedVirtualPaths,
    DateTimeOffset NotBeforeUtc,
    TimeSpan Timeout);

public sealed record PatchVerificationResult(
    bool Success,
    bool RunConfigurationLoaded,
    bool PatchStackLoaded,
    int ResolvedFileCount,
    int ExpectedFileCount,
    string? LogPath,
    IReadOnlyList<string> MissingVirtualPaths,
    IReadOnlyList<string> Diagnostics)
{
    public bool AllExpectedFilesResolved => ResolvedFileCount == ExpectedFileCount;
}

public sealed record ExtractionResult(
    bool Success,
    string StagingDirectory,
    IReadOnlyList<ExtractedTexture> Textures,
    IReadOnlyList<string> Diagnostics) : IAsyncDisposable
{
    public ValueTask DisposeAsync()
    {
        if (!string.IsNullOrWhiteSpace(StagingDirectory) && Directory.Exists(StagingDirectory))
        {
            Directory.Delete(StagingDirectory, recursive: true);
        }

        return ValueTask.CompletedTask;
    }
}

public sealed record ExtractedTexture(string VirtualPath, string FilePath, GameVisualRole Role);
