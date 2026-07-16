using System.Diagnostics;
using Chromassist.Core.Services;

namespace Chromassist.Core.Thcrap;

public sealed class ThcrapGameLauncher : IGameLauncher
{
    public Task<GameLaunchResult> LaunchAsync(
        string thcrapDirectory,
        string runConfigurationPath,
        string gameId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var executable = Path.Combine(thcrapDirectory, "bin", "thcrap_loader.exe");
        if (!File.Exists(executable))
        {
            throw new FileNotFoundException("bin\\thcrap_loader.exe를 찾을 수 없습니다.", executable);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            WorkingDirectory = thcrapDirectory,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(Path.GetFileName(runConfigurationPath));
        startInfo.ArgumentList.Add(gameId);
        var startedAt = DateTimeOffset.UtcNow;
        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("thcrap loader를 시작하지 못했습니다.");
        return Task.FromResult(new GameLaunchResult(process.Id, startedAt, executable));
    }
}
