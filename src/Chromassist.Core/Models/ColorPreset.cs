namespace Chromassist.Core.Models;

public sealed record ColorPreset(
    string Id,
    string DisplayName,
    string Description,
    PresetKind Kind,
    double PrimaryHueShiftDegrees,
    double SecondaryHueShiftDegrees,
    double ChromaScale,
    double StrengthPercent,
    string ValidationStatus,
    string AlgorithmVersion)
{
    public bool IsOriginal => Kind == PresetKind.Original;
}

public enum PresetKind
{
    Original,
    Protan,
    Deutan,
    Tritan
}
