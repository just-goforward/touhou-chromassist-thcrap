namespace Chromassist.Core.Models;

public sealed record PatchBuildResult(
    bool Success,
    string Summary,
    string? PatchDirectory,
    string? RunConfigurationPath,
    IReadOnlyList<GeneratedFileRecord> Files,
    IReadOnlyList<string> Diagnostics);

public sealed record GeneratedFileRecord(
    string RelativePath,
    string SourceSha256,
    string OutputSha256,
    int Width,
    int Height,
    bool AlphaPreserved,
    bool TransparentPixelsPreserved,
    bool NeutralPixelsPreserved,
    int ChangedOpaquePixelCount,
    int OpaquePixelCount,
    double MeanOklabDelta,
    double MaximumOklabDelta);
