using Chromassist.Core.Models;

namespace Chromassist.Core.Presets;

public static class PresetCatalog
{
    public static IReadOnlyList<ColorPreset> All { get; } =
    [
        Create(PresetKind.Protan, 50),
        Create(PresetKind.Deutan, 50),
        Create(PresetKind.Tritan, 50)
    ];

    public static ColorPreset Create(PresetKind kind, double strengthPercent)
    {
        if (kind is PresetKind.Original)
        {
            return new("original", "Original", "원본 색상을 유지합니다.", kind, 0, 0, 1, 0, "baseline", "0.2.0");
        }

        var strength = Math.Clamp(strengthPercent, 0, 100);
        var factor = strength / 50d;
        var definition = kind switch
        {
            PresetKind.Protan => new Definition("protan-custom", "Protan", "적색·녹색 역할군의 hue를 제한적으로 분리합니다.", 28, 26, 1.02),
            PresetKind.Deutan => new Definition("deutan-custom", "Deutan", "녹색·적색 역할군의 hue를 제한적으로 분리합니다.", -24, 31, 1.02),
            PresetKind.Tritan => new Definition("tritan-custom", "Tritan", "청색·황색 역할군을 위한 검증 전 변환입니다.", -28, 24, 1.02),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

        return new ColorPreset(
            definition.Id,
            definition.DisplayName,
            definition.Description,
            kind,
            definition.PrimaryHueShiftDegrees * factor,
            definition.SecondaryHueShiftDegrees * factor,
            1 + (definition.ChromaScale - 1) * factor,
            strength,
            "experimental_unvalidated",
            "0.2.0");
    }

    private sealed record Definition(
        string Id,
        string DisplayName,
        string Description,
        double PrimaryHueShiftDegrees,
        double SecondaryHueShiftDegrees,
        double ChromaScale);
}
