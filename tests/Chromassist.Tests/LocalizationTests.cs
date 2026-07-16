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

    [Fact]
    public void UserFacingVisionTypeNamesFollowEachLanguage()
    {
        var catalog = new TextCatalog { Language = "ko" };
        Assert.Equal("적색 계열 색각이상", catalog["Protan"]);
        Assert.Equal("녹색 계열 색각이상", catalog["Deutan"]);
        Assert.Equal("청황색 계열 색각이상", catalog["Tritan"]);

        catalog.Language = "ja";
        Assert.StartsWith("1型色覚", catalog["Protan"]);
        Assert.StartsWith("2型色覚", catalog["Deutan"]);
        Assert.StartsWith("3型色覚", catalog["Tritan"]);

        catalog.Language = "en";
        Assert.Contains("Protan", catalog["Protan"]);
        Assert.Contains("Deutan", catalog["Deutan"]);
        Assert.Contains("Tritan", catalog["Tritan"]);
    }
}
