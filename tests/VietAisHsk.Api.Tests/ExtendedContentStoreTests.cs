using VietAisHsk.Api.Modules.Content;

namespace VietAisHsk.Api.Tests;

public sealed class ExtendedContentStoreTests
{
    [Fact]
    public void Learner_queries_return_published_items_and_apply_filters()
    {
        var store = new InMemoryExtendedContentStore();

        var stories = store.GetStories("HSK 3", "Lớp học");
        var draft = store.GetStory("story-weekend-draft");

        var story = Assert.Single(stories);
        Assert.Equal("story-classroom", story.Id);
        Assert.Null(draft);
    }

    [Fact]
    public void Admin_publish_moves_draft_into_public_query()
    {
        var store = new InMemoryExtendedContentStore();

        Assert.DoesNotContain(store.GetStories(null, null), item => item.Id == "story-weekend-draft");
        var published = Assert.IsType<StoryContent>(store.Publish("stories", "story-weekend-draft"));

        Assert.Equal("Published", published.Status);
        Assert.Contains(store.GetStories(null, null), item => item.Id == "story-weekend-draft");
    }

    [Fact]
    public void Unknown_content_kind_or_id_does_not_publish()
    {
        var store = new InMemoryExtendedContentStore();

        Assert.Null(store.GetAdminItems("unknown"));
        Assert.Null(store.Publish("stories", "not-found"));
    }
}
