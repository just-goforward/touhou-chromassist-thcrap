using Chromassist.Presentation.Localization;

namespace Chromassist.Tests;

public sealed class LocalizationTests
{
    [Fact]
    public void KoreanJapaneseAndEnglishExposeTheSameKeys()
    {
        var korean = TextCatalog.GetKeys("ko").Order(StringComparer.Ordinal).ToArray();

        Assert.Equal(korean, TextCatalog.GetKeys("ja").Order(StringComparer.Ordinal));
        Assert.Equal(korean, TextCatalog.GetKeys("en").Order(StringComparer.Ordinal));
    }
}
