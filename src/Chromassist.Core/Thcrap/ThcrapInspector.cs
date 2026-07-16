using System.Globalization;
using System.Text.Json;
using Chromassist.Core.Models;

namespace Chromassist.Core.Thcrap;

public sealed class ThcrapInspector
{
    public ThcrapInspection Inspect(GameInstallation installation, string minimumVersion)
    {
        if (installation.ThcrapDirectory is null)
        {
            return ThcrapInspection.Missing;
        }

        var executable = Path.Combine(installation.ThcrapDirectory, "bin", "thcrap_loader.exe");
        var config = Path.Combine(installation.ThcrapDirectory, "config", "thpatch-ko.js");
        if (!File.Exists(executable) || !File.Exists(config))
        {
            return new ThcrapInspection(true, false, null, null, [], "thcrap loader 또는 한국어 실행 설정이 없습니다.");
        }

        var diagnostics = new List<string>();
        var archives = ReadPatchArchives(config, diagnostics);
        var expected = new[] { "base_tsa", "base_tasofro", "script_latin", "lang_ko" };
        var stackValid = expected.All(expectedId => archives.Any(archive => archive.Contains(expectedId, StringComparison.OrdinalIgnoreCase)));

        var version = ReadVersionFromLogs(installation.ThcrapDirectory);
        var versionValid = version is not null && CompareDateVersion(version, minimumVersion) >= 0;
        var compatible = stackValid && versionValid;
        var summary = compatible
            ? $"thcrap {version}, 한국어 patch stack 확인됨"
            : version is null
                ? "thcrap version을 로그에서 확인할 수 없습니다. 게임을 기존 한국어 설정으로 한 번 실행한 뒤 다시 확인하십시오."
                : !versionValid
                    ? $"thcrap {version}은 최소 지원 version {minimumVersion}보다 오래되었습니다."
                    : "한국어 patch stack이 예상 구조와 다릅니다.";

        return new ThcrapInspection(true, compatible, version, config, archives, summary);
    }

    private static IReadOnlyList<string> ReadPatchArchives(string configPath, ICollection<string> diagnostics)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(configPath), new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            });

            if (!document.RootElement.TryGetProperty("patches", out var patches) || patches.ValueKind != JsonValueKind.Array)
            {
                diagnostics.Add("run configuration에 patches 배열이 없습니다.");
                return [];
            }

            return patches.EnumerateArray()
                .Where(static item => item.TryGetProperty("archive", out _))
                .Select(static item => item.GetProperty("archive").GetString())
                .Where(static archive => !string.IsNullOrWhiteSpace(archive))
                .Cast<string>()
                .ToArray();
        }
        catch (JsonException exception)
        {
            diagnostics.Add($"run configuration JSON 해석 실패: {exception.Message}");
            return [];
        }
    }

    private static string? ReadVersionFromLogs(string thcrapDirectory)
    {
        var logs = Path.Combine(thcrapDirectory, "logs");
        if (!Directory.Exists(logs))
        {
            return null;
        }

        foreach (var path in Directory.EnumerateFiles(logs, "thcrap_log*.txt").OrderByDescending(File.GetLastWriteTimeUtc))
        {
            foreach (var line in File.ReadLines(path).Take(20))
            {
                const string prefix = "Version:";
                if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return line[prefix.Length..].Trim();
                }
            }
        }

        return null;
    }

    private static int CompareDateVersion(string left, string right)
    {
        const string format = "yyyy-MM-dd";
        if (DateTime.TryParseExact(left, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var leftDate)
            && DateTime.TryParseExact(right, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var rightDate))
        {
            return leftDate.CompareTo(rightDate);
        }

        return string.Compare(left, right, StringComparison.Ordinal);
    }
}
