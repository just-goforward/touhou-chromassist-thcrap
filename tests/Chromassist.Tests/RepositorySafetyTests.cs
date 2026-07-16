namespace Chromassist.Tests;

public sealed class RepositorySafetyTests
{
    [Fact]
    public void SourceTreeContainsNoBlockedTouhouAssetExtensions()
    {
        var repositoryRoot = FindRepositoryRoot();
        var blocked = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".dat", ".anm", ".ecl", ".wav"
        };
        var roots = new[] { "src", "tests", "docs", "schemas" }
            .Select(name => Path.Combine(repositoryRoot, name))
            .Where(Directory.Exists);

        var violations = roots
            .SelectMany(root => Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            .Where(path => blocked.Contains(Path.GetExtension(path)))
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void StrengthSliderUpdatesContinuouslyAndJumpsToClickedPoint()
    {
        var xaml = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "Chromassist.App", "MainWindow.xaml"));

        Assert.Contains("IsMoveToPointEnabled=\"True\"", xaml, StringComparison.Ordinal);
        Assert.Contains("UpdateSourceTrigger=PropertyChanged", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Delay=", xaml, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "ThChromassist.sln")))
        {
            current = current.Parent;
        }

        return current?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
