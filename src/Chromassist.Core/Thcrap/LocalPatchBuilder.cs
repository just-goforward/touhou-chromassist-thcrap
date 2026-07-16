using System.Text.Json;
using System.Text.Json.Nodes;
using Chromassist.Core.Color;
using Chromassist.Core.Imaging;
using Chromassist.Core.Infrastructure;
using Chromassist.Core.Models;
using Chromassist.Core.Services;

namespace Chromassist.Core.Thcrap;

public sealed class LocalPatchBuilder : IPatchBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public async Task<PatchBuildResult> BuildAsync(
        GameValidationResult validation,
        ExtractionResult extraction,
        ColorPreset preset,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(validation);
        ArgumentNullException.ThrowIfNull(extraction);
        ArgumentNullException.ThrowIfNull(preset);

        if (!validation.CanGeneratePatch || !extraction.Success || validation.Installation.ThcrapDirectory is null)
        {
            return Failure("검증된 게임, thcrap, 추출 결과가 모두 필요합니다.");
        }

        var thcrapDirectory = Path.GetFullPath(validation.Installation.ThcrapDirectory);
        var repositoryDirectory = Path.Combine(thcrapDirectory, "repos", "chromassist");
        var patchId = $"th18-{SanitizeId(preset.Id)}";
        var patchDirectory = Path.Combine(repositoryDirectory, patchId);
        var temporaryDirectory = patchDirectory + $".tmp-{Guid.NewGuid():N}";
        var backupDirectory = patchDirectory + $".backup-{Guid.NewGuid():N}";
        var diagnostics = new List<string>();
        var records = new List<GeneratedFileRecord>();
        string? runConfigurationPath = null;

        try
        {
            Directory.CreateDirectory(temporaryDirectory);
            foreach (var texture in extraction.Textures.OrderBy(static texture => texture.VirtualPath, StringComparer.Ordinal))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var destination = ResolveContainedPath(temporaryDirectory, texture.VirtualPath);
                var sourceImage = PngCodec.Read(texture.FilePath);
                var outputImage = PresetTransformer.Transform(sourceImage, preset);
                var invariant = ImageInvariantChecker.Compare(sourceImage, outputImage);
                if (!invariant.Success)
                {
                    return Failure($"보호된 이미지 불변 조건 위반: {texture.VirtualPath}", invariant.Summary);
                }

                PngCodec.Write(destination, outputImage);
                var difference = MeasureDifference(sourceImage, outputImage);
                records.Add(new GeneratedFileRecord(
                    texture.VirtualPath.Replace('\\', '/'),
                    await FileHash.Sha256Async(texture.FilePath, cancellationToken).ConfigureAwait(false),
                    await FileHash.Sha256Async(destination, cancellationToken).ConfigureAwait(false),
                    sourceImage.Width,
                    sourceImage.Height,
                    invariant.AlphaPreserved,
                    invariant.TransparentPixelsPreserved,
                    difference.Mean,
                    difference.Maximum));
            }

            await WriteMetadataAsync(temporaryDirectory, patchId, validation, preset, records, cancellationToken).ConfigureAwait(false);
            CommitDirectory(temporaryDirectory, patchDirectory, backupDirectory);
            WriteRepositoryMetadata(repositoryDirectory);
            runConfigurationPath = WriteRunConfiguration(validation, patchId, preset.Id);
            diagnostics.Add($"{records.Count}개 texture를 변환하고 로컬 thcrap patch를 생성했습니다.");
            diagnostics.Add("원본 thpatch-ko.js는 변경하지 않았습니다.");
            return new PatchBuildResult(true, "패치 생성 완료", patchDirectory, runConfigurationPath, records, diagnostics);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException or NotSupportedException)
        {
            diagnostics.Add(exception.Message);
            return new PatchBuildResult(false, "패치 생성 실패", null, runConfigurationPath, records, diagnostics);
        }
        finally
        {
            TryDeleteDirectory(temporaryDirectory);
            TryDeleteDirectory(backupDirectory);
        }
    }

    private static async Task WriteMetadataAsync(
        string directory,
        string patchId,
        GameValidationResult validation,
        ColorPreset preset,
        IReadOnlyList<GeneratedFileRecord> records,
        CancellationToken cancellationToken)
    {
        var patch = new
        {
            id = patchId,
            title = $"TH Chromassist: {UserFacingEnglishName(preset.Kind)} ({preset.StrengthPercent:0.0}%)",
            update = false,
            dependencies = new[] { "nmlgc/base_tsa" },
            supported_games = new[] { "th18" }
        };
        var manifest = new
        {
            schema_version = "1.0.0",
            game_id = "th18",
            game_version = validation.AssetSet?.VersionLabel,
            asset_set_id = validation.AssetSet?.Id,
            executable_sha256 = validation.ExecutableSha256,
            data_archive_sha256 = validation.DataArchiveSha256,
            preset = new
            {
                preset.Id,
                preset.Kind,
                preset.ValidationStatus,
                preset.AlgorithmVersion,
                preset.PrimaryHueShiftDegrees,
                preset.SecondaryHueShiftDegrees,
                preset.ChromaScale,
                preset.StrengthPercent,
                user_modified = false
            },
            fairness = new
            {
                status = "experimental_unvalidated",
                geometry_changed = false,
                alpha_changed = false,
                contrast_limit_evaluated = false,
                note = "Preset values are experimental and are not yet supported by user-study evidence."
            },
            generated_at_utc = DateTimeOffset.UtcNow,
            files = records
        };

        await File.WriteAllTextAsync(Path.Combine(directory, "patch.js"), JsonSerializer.Serialize(patch, JsonOptions), cancellationToken)
            .ConfigureAwait(false);
        await File.WriteAllTextAsync(Path.Combine(directory, "manifest.json"), JsonSerializer.Serialize(manifest, JsonOptions), cancellationToken)
            .ConfigureAwait(false);
    }

    private static string WriteRunConfiguration(GameValidationResult validation, string patchId, string presetId)
    {
        var sourcePath = validation.Thcrap.RunConfigurationPath
            ?? throw new InvalidOperationException("thcrap run configuration path is missing.");
        var root = JsonNode.Parse(File.ReadAllText(sourcePath), documentOptions: new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        })?.AsObject() ?? throw new InvalidDataException("thcrap run configuration is not a JSON object.");
        var patches = root["patches"]?.AsArray()
            ?? throw new InvalidDataException("thcrap run configuration has no patches array.");
        patches.Add(new JsonObject
        {
            ["archive"] = $"repos/chromassist/{patchId}/",
            ["update"] = false
        });

        var configDirectory = Path.GetDirectoryName(sourcePath)!;
        var outputPath = Path.Combine(configDirectory, $"thpatch-ko-chromassist-{SanitizeId(presetId)}.js");
        var temporaryPath = outputPath + $".tmp-{Guid.NewGuid():N}";
        File.WriteAllText(temporaryPath, root.ToJsonString(JsonOptions));
        File.Move(temporaryPath, outputPath, overwrite: true);
        return outputPath;
    }

    private static void WriteRepositoryMetadata(string repositoryDirectory)
    {
        Directory.CreateDirectory(repositoryDirectory);
        var patches = new JsonObject();
        foreach (var directory in Directory.EnumerateDirectories(repositoryDirectory).OrderBy(static path => path, StringComparer.Ordinal))
        {
            var patchMetadataPath = Path.Combine(directory, "patch.js");
            if (!File.Exists(patchMetadataPath))
            {
                continue;
            }

            var metadata = JsonNode.Parse(File.ReadAllText(patchMetadataPath))?.AsObject();
            var id = metadata?["id"]?.GetValue<string>();
            var title = metadata?["title"]?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(title))
            {
                patches[id] = title;
            }
        }

        var repository = new JsonObject
        {
            ["contact"] = "https://github.com/just-goforward/th-chromassist/issues",
            ["id"] = "chromassist",
            ["patches"] = patches,
            ["servers"] = new JsonArray(),
            ["title"] = "TH Chromassist (local)"
        };
        var outputPath = Path.Combine(repositoryDirectory, "repo.js");
        var temporaryPath = outputPath + $".tmp-{Guid.NewGuid():N}";
        File.WriteAllText(temporaryPath, repository.ToJsonString(JsonOptions));
        File.Move(temporaryPath, outputPath, overwrite: true);
    }

    private static void CommitDirectory(string temporaryDirectory, string patchDirectory, string backupDirectory)
    {
        if (Directory.Exists(patchDirectory))
        {
            Directory.Move(patchDirectory, backupDirectory);
        }

        try
        {
            Directory.Move(temporaryDirectory, patchDirectory);
            TryDeleteDirectory(backupDirectory);
        }
        catch
        {
            if (!Directory.Exists(patchDirectory) && Directory.Exists(backupDirectory))
            {
                Directory.Move(backupDirectory, patchDirectory);
            }

            throw;
        }
    }

    private static string ResolveContainedPath(string root, string relativePath)
    {
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var rootPath = Path.GetFullPath(root) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(rootPath, normalized));
        if (!fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Patch output path escaped the generated patch directory.");
        }

        return fullPath;
    }

    private static (double Mean, double Maximum) MeasureDifference(RgbaImage source, RgbaImage output)
    {
        double total = 0;
        double maximum = 0;
        var count = 0;
        for (var index = 0; index < source.Pixels.Length; index += 4)
        {
            if (source.Pixels[index + 3] == 0)
            {
                continue;
            }

            var first = OklabColor.FromSrgb(source.Pixels[index], source.Pixels[index + 1], source.Pixels[index + 2]);
            var second = OklabColor.FromSrgb(output.Pixels[index], output.Pixels[index + 1], output.Pixels[index + 2]);
            var delta = Math.Sqrt(
                Math.Pow(first.L - second.L, 2) +
                Math.Pow(first.A - second.A, 2) +
                Math.Pow(first.B - second.B, 2));
            total += delta;
            maximum = Math.Max(maximum, delta);
            count++;
        }

        return count == 0 ? (0, 0) : (total / count, maximum);
    }

    private static string SanitizeId(string value)
    {
        var sanitized = new string(value.ToLowerInvariant().Select(character =>
            char.IsAsciiLetterOrDigit(character) || character == '-' ? character : '-').ToArray());
        return sanitized.Trim('-');
    }

    private static string UserFacingEnglishName(PresetKind kind) => kind switch
    {
        PresetKind.Protan => "Protan red-green CVD",
        PresetKind.Deutan => "Deutan red-green CVD",
        PresetKind.Tritan => "Tritan blue-yellow CVD",
        _ => "Original colours"
    };

    private static void TryDeleteDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            try
            {
                Directory.Delete(path, recursive: true);
            }
            catch (IOException)
            {
                // Best-effort cleanup. The next run uses a new unique directory.
            }
            catch (UnauthorizedAccessException)
            {
                // Best-effort cleanup. The next run uses a new unique directory.
            }
        }
    }

    private static PatchBuildResult Failure(string summary, params string[] diagnostics) =>
        new(false, summary, null, null, [], diagnostics);
}
