using VietAisHsk.Api.Modules.Curriculum;

namespace VietAisHsk.Api.Tests;

public sealed class KnowledgeCatalogTests
{
    [Fact]
    public void Vocabulary_search_filters_by_hsk_and_keeps_provenance()
    {
        var catalog = new BootstrapKnowledgeCatalog();

        var entries = catalog.SearchVocabulary("安排", "HSK 3", null);

        var entry = Assert.Single(entries);
        Assert.Equal("安排", entry.Simplified);
        Assert.Equal("PlatformAuthoredReferenceFixture", entry.SourceType);
        Assert.Equal("platform-authored", entry.LicenseRef);
    }

    [Fact]
    public void Grammar_detail_contains_examples_and_related_lesson()
    {
        var catalog = new BootstrapKnowledgeCatalog();

        var grammar = catalog.GetGrammar("grammar-zhengzai");

        Assert.NotNull(grammar);
        Assert.Equal("正在 + V", grammar.Pattern);
        Assert.Equal("lesson-02", grammar.RelatedLessonId);
        Assert.NotEmpty(grammar.Examples);
    }
}
