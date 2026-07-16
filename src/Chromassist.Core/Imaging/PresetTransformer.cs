using Chromassist.Core.Color;
using Chromassist.Core.Models;

namespace Chromassist.Core.Imaging;

public static class PresetTransformer
{
    // Experimental guardrail: neutral cores, highlights, shadows, and grey UI-like pixels
    // are not palette roles and must remain byte-identical.
    public const double MinimumRoleChroma = 0.02;
    private const double MinimumHueShiftDegrees = 0.01;

    public static RgbaImage Transform(RgbaImage source, ColorPreset preset)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(preset);

        var output = source.Clone();
        if (preset.IsOriginal)
        {
            return output;
        }

        for (var index = 0; index < output.Pixels.Length; index += 4)
        {
            if (source.Pixels[index + 3] == 0)
            {
                continue;
            }

            var lab = OklabColor.FromSrgb(
                source.Pixels[index],
                source.Pixels[index + 1],
                source.Pixels[index + 2]);
            var (lightness, chroma, hue) = lab.ToOklch();
            if (chroma < MinimumRoleChroma)
            {
                continue;
            }

            var hueDegrees = NormalizeDegrees(hue * 180 / Math.PI);
            var shift = SelectHueShift(hueDegrees, preset);
            if (Math.Abs(shift) < MinimumHueShiftDegrees)
            {
                continue;
            }

            var adjusted = OklabColor.FromOklch(
                lightness,
                chroma * preset.ChromaScale,
                hue + shift * Math.PI / 180);
            var (red, green, blue) = adjusted.ToSrgb();

            output.Pixels[index] = red;
            output.Pixels[index + 1] = green;
            output.Pixels[index + 2] = blue;
        }

        return output;
    }

    private static double SelectHueShift(double hueDegrees, ColorPreset preset)
    {
        // Broad role groups are intentionally simple in the prototype. Preset values are
        // experimental and must be replaced by measured role mappings before a stable release.
        var warmWeight = CircularWeight(hueDegrees, 30, 75);
        var coolWeight = CircularWeight(hueDegrees, 210, 95);
        return preset.PrimaryHueShiftDegrees * warmWeight + preset.SecondaryHueShiftDegrees * coolWeight;
    }

    private static double CircularWeight(double value, double center, double radius)
    {
        var distance = Math.Abs(NormalizeDegrees(value - center));
        if (distance > 180)
        {
            distance = 360 - distance;
        }

        return distance >= radius ? 0 : 0.5 * (1 + Math.Cos(Math.PI * distance / radius));
    }

    private static double NormalizeDegrees(double degrees)
    {
        var normalized = degrees % 360;
        return normalized < 0 ? normalized + 360 : normalized;
    }
}
