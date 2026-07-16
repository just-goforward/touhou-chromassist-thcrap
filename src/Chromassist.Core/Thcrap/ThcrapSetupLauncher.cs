using System.Diagnostics;
using Chromassist.Core.Services;

namespace Chromassist.Core.Thcrap;

public sealed class ThcrapSetupLauncher : IThcrapSetupLauncher
{
    public Task<SetupLaunchResult> LaunchAsync(
        string thcrapDirectory,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var executable = Path.Combine(thcrapDirectory, "thcrap.exe");
        if (!File.Exists(executable))
        {
            throw new FileNotFoundException("thcrap.exe 설정 도구를 찾을 수 없습니다.", executable);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            WorkingDirectory = thcrapDirectory,
            UseShellExecute = false
        };
        var startedAt = DateTimeOffset.UtcNow;
        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("thcrap 설정 도구를 시작하지 못했습니다.");
        return Task.FromResult(new SetupLaunchResult(process.Id, startedAt, executable));
    }
}
