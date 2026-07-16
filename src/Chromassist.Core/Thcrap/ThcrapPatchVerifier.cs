using Chromassist.Core.Services;

namespace Chromassist.Core.Thcrap;

public sealed class ThcrapPatchVerifier : IPatchApplicationVerifier
{
    public async Task<PatchVerificationResult> VerifyAsync(
        PatchVerificationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var deadline = DateTimeOffset.UtcNow + request.Timeout;
        PatchVerificationResult? latest = null;
        DateTimeOffset? patchStackDetectedAt = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            latest = InspectLogs(request);
            if (latest.Success)
            {
                patchStackDetectedAt ??= DateTimeOffset.UtcNow;
                if (latest.AllExpectedFilesResolved || DateTimeOffset.UtcNow - patchStackDetectedAt >= TimeSpan.FromSeconds(5))
                {
                    return latest;
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);
        }

        return latest ?? new PatchVerificationResult(
            false,
            false,
            false,
            0,
            request.ExpectedVirtualPaths.Count,
            null,
            request.ExpectedVirtualPaths,
            ["검증 시간 안에 새 thcrap 로그를 찾지 못했습니다."]);
    }

    private static PatchVerificationResult InspectLogs(PatchVerificationRequest request)
    {
        var logDirectory = Path.Combine(request.ThcrapDirectory, "logs");
        if (!Directory.Exists(logDirectory))
        {
            return new PatchVerificationResult(
                false, false, false, 0, request.ExpectedVirtualPaths.Count, null,
                request.ExpectedVirtualPaths, ["thcrap 로그 폴더가 없습니다."]);
        }

        var minimumWriteTime = request.NotBeforeUtc.UtcDateTime.AddSeconds(-3);
        var logFiles = Directory.EnumerateFiles(logDirectory, "thcrap_log*.txt")
            .Where(path => File.GetLastWriteTimeUtc(path) >= minimumWriteTime)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .ToArray();
        var configName = Path.GetFileName(request.RunConfigurationPath);
        var patchPath = Normalize(request.PatchDirectory);
        var runConfigurationLoaded = false;
        var patchStackLoaded = false;
        var resolved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string? evidenceLog = null;
        var diagnostics = new List<string>();

        foreach (var logFile in logFiles)
        {
            string[] lines;
            try
            {
                lines = ReadLinesShared(logFile);
            }
            catch (IOException exception)
            {
                diagnostics.Add($"로그 읽기 재시도 필요: {exception.Message}");
                continue;
            }

            var normalizedLines = lines.Select(Normalize).ToArray();
            if (normalizedLines.Any(line =>
                line.Contains(Normalize(configName), StringComparison.OrdinalIgnoreCase) &&
                line.Contains("found", StringComparison.OrdinalIgnoreCase)))
            {
                runConfigurationLoaded = true;
                evidenceLog ??= logFile;
            }

            if (normalizedLines.Any(line => line.Contains(patchPath, StringComparison.OrdinalIgnoreCase)))
            {
                patchStackLoaded = true;
                evidenceLog = logFile;
            }

            foreach (var virtualPath in request.ExpectedVirtualPaths)
            {
                var expected = Normalize(virtualPath);
                for (var index = 0; index < normalizedLines.Length; index++)
                {
                    if (!normalizedLines[index].Contains($"resolving {expected}", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var upperBound = Math.Min(index + 5, normalizedLines.Length);
                    if (normalizedLines[index..upperBound].Any(line =>
                        line.Contains(patchPath, StringComparison.OrdinalIgnoreCase)))
                    {
                        resolved.Add(virtualPath);
                        evidenceLog = logFile;
                        break;
                    }
                }
            }
        }

        var missing = request.ExpectedVirtualPaths.Where(path => !resolved.Contains(path)).ToArray();
        var success = runConfigurationLoaded && patchStackLoaded;
        if (logFiles.Length > 0 && !runConfigurationLoaded)
        {
            diagnostics.Add("생성된 run configuration을 loader가 읽었다는 로그를 찾지 못했습니다.");
        }

        if (missing.Length > 0)
        {
            diagnostics.Add($"예상 texture {request.ExpectedVirtualPaths.Count}개 중 {resolved.Count}개만 로컬 patch에서 resolve되었습니다.");
        }

        if (!patchStackLoaded)
        {
            diagnostics.Add("생성된 로컬 patch archive가 runtime stack에 포함됐다는 로그를 찾지 못했습니다.");
        }

        return new PatchVerificationResult(
            success,
            runConfigurationLoaded,
            patchStackLoaded,
            resolved.Count,
            request.ExpectedVirtualPaths.Count,
            evidenceLog,
            missing,
            diagnostics);
    }

    private static string[] ReadLinesShared(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
        var lines = new List<string>();
        while (reader.ReadLine() is { } line)
        {
            lines.Add(line);
        }

        return lines.ToArray();
    }

    private static string Normalize(string value) => value.Replace('\\', '/');
}
