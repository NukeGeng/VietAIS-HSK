namespace VietAisHsk.Api.Modules.Content;

public sealed record StoryContent(
    string Id,
    string Title,
    string Chinese,
    string Vietnamese,
    string? Pinyin,
    string HskLevel,
    string Topic,
    string Status,
    string SourceType,
    string SourceVersion,
    string LicenseRef);

public sealed record VideoContent(
    string Id,
    string Title,
    string Description,
    string SourceUrl,
    string Transcript,
    string HskLevel,
    string Topic,
    string Status,
    string SourceType,
    string SourceVersion,
    string LicenseRef);

public sealed record LearningResource(
    string Id,
    string Title,
    string Description,
    string ResourceType,
    string Url,
    string HskLevel,
    string Topic,
    string Status,
    string SourceType,
    string SourceVersion,
    string LicenseRef);

public sealed record ToolDefinition(
    string Id,
    string Title,
    string Description,
    string Route,
    string Status,
    string SourceType,
    string SourceVersion,
    string LicenseRef);

public interface IExtendedContentStore
{
    IReadOnlyList<StoryContent> GetStories(string? hsk, string? topic);
    StoryContent? GetStory(string id);
    IReadOnlyList<VideoContent> GetVideos(string? hsk, string? topic);
    VideoContent? GetVideo(string id);
    IReadOnlyList<LearningResource> GetResources(string? hsk, string? topic);
    LearningResource? GetResource(string id);
    IReadOnlyList<ToolDefinition> GetTools();
    ToolDefinition? GetTool(string id);
    object? GetAdminItems(string kind);
    object? Publish(string kind, string id);
}

public sealed class InMemoryExtendedContentStore : IExtendedContentStore
{
    private const string SourceType = "PlatformAuthoredReferenceFixture";
    private const string SourceVersion = "extended-content-v1";
    private const string LicenseRef = "platform-authored";

    private readonly object gate = new();
    private readonly List<StoryContent> stories =
    [
        new("story-classroom", "Một ngày ở lớp học", "今天我在教室学习汉语。", "Hôm nay tôi học tiếng Trung trong lớp.", "Jīntiān wǒ zài jiàoshì xuéxí Hànyǔ.", "HSK 3", "Lớp học", "Published", SourceType, SourceVersion, LicenseRef),
        new("story-weekend-draft", "Cuối tuần của tôi", "周末我和朋友去公园。", "Cuối tuần tôi đi công viên với bạn.", "Zhōumò wǒ hé péngyou qù gōngyuán.", "HSK 3", "Sinh hoạt", "Draft", SourceType, SourceVersion, LicenseRef),
    ];
    private readonly List<VideoContent> videos =
    [
        new("video-classroom", "Hội thoại HSK 3", "Một đoạn hội thoại ngắn trong lớp học.", "/assets/content/video-classroom.mp4", "他们在教室学习汉语。", "HSK 3", "Lớp học", "Published", SourceType, SourceVersion, LicenseRef),
        new("video-pronunciation-draft", "Phát âm thanh 3", "Video hướng dẫn phát âm.", "/assets/content/video-pronunciation.mp4", "第三声先降后升。", "HSK 3", "Phát âm", "Draft", SourceType, SourceVersion, LicenseRef),
    ];
    private readonly List<LearningResource> resources =
    [
        new("resource-hsk3-grammar", "Tài liệu HSK 3", "Tổng hợp từ vựng và ngữ pháp HSK 3.", "PDF", "/assets/content/hsk3-guide.pdf", "HSK 3", "Tổng hợp", "Published", SourceType, SourceVersion, LicenseRef),
        new("resource-hanzi-draft", "Bảng tổng hợp chữ Hán", "Bảng tham khảo HSK 1–3.", "PDF", "/assets/content/hanzi-guide.pdf", "HSK 3", "Chữ Hán", "Draft", SourceType, SourceVersion, LicenseRef),
    ];
    private readonly List<ToolDefinition> tools =
    [
        new("tool-quick-lookup", "Tra cứu nhanh", "Từ, Pinyin và ví dụ trong catalog đã publish.", "/app/vocabulary", "Published", SourceType, SourceVersion, LicenseRef),
        new("tool-focus-timer-draft", "Bộ hẹn giờ học", "Giữ một phiên học tập trung.", "/app/tools/focus-timer", "Draft", SourceType, SourceVersion, LicenseRef),
    ];

    public IReadOnlyList<StoryContent> GetStories(string? hsk, string? topic) => FilterStories(stories, hsk, topic);
    public StoryContent? GetStory(string id) => stories.FirstOrDefault(item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase) && IsPublished(item.Status));
    public IReadOnlyList<VideoContent> GetVideos(string? hsk, string? topic) => FilterVideos(videos, hsk, topic);
    public VideoContent? GetVideo(string id) => videos.FirstOrDefault(item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase) && IsPublished(item.Status));
    public IReadOnlyList<LearningResource> GetResources(string? hsk, string? topic) => FilterResources(resources, hsk, topic);
    public LearningResource? GetResource(string id) => resources.FirstOrDefault(item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase) && IsPublished(item.Status));
    public IReadOnlyList<ToolDefinition> GetTools() => tools.Where(item => IsPublished(item.Status)).ToArray();
    public ToolDefinition? GetTool(string id) => tools.FirstOrDefault(item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase) && IsPublished(item.Status));

    public object? GetAdminItems(string kind) => kind.ToLowerInvariant() switch
    {
        "stories" => stories.ToArray(),
        "videos" => videos.ToArray(),
        "resources" => resources.ToArray(),
        "tools" => tools.ToArray(),
        _ => null,
    };

    public object? Publish(string kind, string id)
    {
        lock (gate)
        {
            return kind.ToLowerInvariant() switch
            {
                "stories" => PublishStory(id),
                "videos" => PublishVideo(id),
                "resources" => PublishResource(id),
                "tools" => PublishTool(id),
                _ => null,
            };
        }
    }

    private static StoryContent[] FilterStories(IEnumerable<StoryContent> source, string? hsk, string? topic) =>
        source.Where(item => IsPublished(item.Status)).Where(item => Matches(item.HskLevel, hsk)).Where(item => Matches(item.Topic, topic)).ToArray();

    private static VideoContent[] FilterVideos(IEnumerable<VideoContent> source, string? hsk, string? topic) =>
        source.Where(item => IsPublished(item.Status)).Where(item => Matches(item.HskLevel, hsk)).Where(item => Matches(item.Topic, topic)).ToArray();

    private static LearningResource[] FilterResources(IEnumerable<LearningResource> source, string? hsk, string? topic) =>
        source.Where(item => IsPublished(item.Status)).Where(item => Matches(item.HskLevel, hsk)).Where(item => Matches(item.Topic, topic)).ToArray();

    private static bool Matches(string value, string? filter) =>
        string.IsNullOrWhiteSpace(filter) || string.Equals(value, filter.Trim(), StringComparison.OrdinalIgnoreCase);

    private static bool IsPublished(string status) => string.Equals(status, "Published", StringComparison.OrdinalIgnoreCase);

    private StoryContent? PublishStory(string id)
    {
        var index = stories.FindIndex(item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        if (index < 0) return null;
        stories[index] = stories[index] with { Status = "Published" };
        return stories[index];
    }

    private VideoContent? PublishVideo(string id)
    {
        var index = videos.FindIndex(item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        if (index < 0) return null;
        videos[index] = videos[index] with { Status = "Published" };
        return videos[index];
    }

    private LearningResource? PublishResource(string id)
    {
        var index = resources.FindIndex(item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        if (index < 0) return null;
        resources[index] = resources[index] with { Status = "Published" };
        return resources[index];
    }

    private ToolDefinition? PublishTool(string id)
    {
        var index = tools.FindIndex(item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        if (index < 0) return null;
        tools[index] = tools[index] with { Status = "Published" };
        return tools[index];
    }
}
