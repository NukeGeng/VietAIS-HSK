using VietAisHsk.Api.Modules.Curriculum;

namespace VietAisHsk.Api.Tests;

public sealed class HanziCatalogTests
{
    [Fact]
    public void Bootstrap_catalog_exposes_reference_and_ordered_strokes_without_claiming_official_source()
    {
        var catalog = new BootstrapHanziCatalog();

        var character = catalog.Get("hanzi-da");
        var strokes = catalog.GetStrokes("大");

        Assert.NotNull(character);
        Assert.Equal("dà", character.Pinyin);
        Assert.Equal(3, character.StrokeCount);
        Assert.NotNull(strokes);
        Assert.Equal(character.Id, strokes.HanziId);
        Assert.Equal(new[] { 1, 2, 3 }, strokes.Strokes.Select(stroke => stroke.Order));
        Assert.Equal("PlatformAuthoredReferenceFixture", strokes.Source);
        Assert.False(string.IsNullOrWhiteSpace(strokes.SourceVersion));
        Assert.Equal("platform-authored", strokes.LicenseRef);
        Assert.All(strokes.Strokes, stroke =>
        {
            Assert.False(string.IsNullOrWhiteSpace(stroke.Path));
            Assert.False(string.IsNullOrWhiteSpace(stroke.Description));
        });
    }

    [Fact]
    public void Unknown_hanzi_has_no_stroke_reference()
    {
        var catalog = new BootstrapHanziCatalog();

        Assert.Null(catalog.Get("does-not-exist"));
        Assert.Null(catalog.GetStrokes("does-not-exist"));
    }
}
