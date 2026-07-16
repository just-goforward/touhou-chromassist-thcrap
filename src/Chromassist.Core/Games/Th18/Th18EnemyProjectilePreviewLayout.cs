using Chromassist.Core.Imaging;

namespace Chromassist.Core.Games.Th18;

public static class Th18EnemyProjectilePreviewLayout
{
    // Locally observed TH18 v1.00a bullet1 atlas cells. These coordinates contain
    // enemy-projectile silhouettes in multiple palette roles; no pixels are distributed.
    public static IReadOnlyList<SpriteRegion> RepresentativeRegions { get; } =
    [
        new(16, 16, 16, 16),
        new(48, 16, 16, 16),
        new(80, 16, 16, 16),
        new(112, 16, 16, 16),
        new(144, 16, 16, 16),
        new(176, 16, 16, 16),
        new(208, 16, 16, 16),
        new(240, 16, 16, 16),
        new(16, 32, 16, 16),
        new(48, 32, 16, 16),
        new(80, 32, 16, 16),
        new(112, 32, 16, 16),
        new(144, 32, 16, 16),
        new(176, 32, 16, 16),
        new(208, 32, 16, 16),
        new(240, 32, 16, 16)
    ];
}
