using Marten;
using VietAisHsk.Api.Modules.Curriculum;

namespace VietAisHsk.Api.Infrastructure;

public sealed record CurriculumDocument(
    string Id,
    List<SyllabusVersion> SyllabusVersions,
    List<HskLevel> Levels);

public sealed class MartenCurriculumStore(IDocumentStore documentStore) : ICurriculumStore
{
    private const string RootId = "curriculum-root";

    public IReadOnlyList<HskLevel> GetPublishedLevels()
    {
        var document = Read();
        var publishedVersions = document.SyllabusVersions
            .Where(version => version.Status == ContentStatus.Published)
            .Select(version => version.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return document.Levels
            .Where(level => level.Status == ContentStatus.Published && publishedVersions.Contains(level.SyllabusVersionId))
            .OrderBy(level => level.LevelNumber)
            .ToArray();
    }

    public HskLevelTree? GetPublishedTree(string level)
    {
        var published = GetPublishedLevels().FirstOrDefault(item =>
            string.Equals(item.Id, level, StringComparison.OrdinalIgnoreCase)
            || item.LevelNumber.ToString() == level);
        return published is null ? null : new HskLevelTree(published, Array.Empty<TopicNode>());
    }

    public BeginnerTrack GetBeginnerTrack() => new InMemoryCurriculumStore().GetBeginnerTrack();

    public FoundationCatalog GetPinyinCatalog() => new("Pinyin", Array.Empty<FoundationItem>());

    public FoundationCatalog GetToneCatalog() => new("Thanh điệu", Array.Empty<FoundationItem>());

    public bool IsPublishedLesson(string lessonId) => false;

    public CurriculumImportResult Import(CurriculumImportRequest request)
    {
        var document = Read();
        var versionId = request.SyllabusVersionId!.Trim();
        var now = DateTimeOffset.UtcNow;
        var wasAlreadyPresent = document.SyllabusVersions.Any(version => version.Id == versionId)
            && request.Levels!.All(level => document.Levels.Any(existing => existing.Id == level.Id!.Trim()));

        var versionIndex = document.SyllabusVersions.FindIndex(version => version.Id == versionId);
        var version = new SyllabusVersion(versionId, request.SyllabusName!.Trim(), request.SourceType!.Trim(), ContentStatus.Draft, now);
        if (versionIndex >= 0)
        {
            var current = document.SyllabusVersions[versionIndex];
            document.SyllabusVersions[versionIndex] = current with { Name = version.Name, SourceType = version.SourceType, UpdatedAt = now };
        }
        else
        {
            document.SyllabusVersions.Add(version);
        }

        foreach (var item in request.Levels!)
        {
            var levelId = item.Id!.Trim();
            var levelIndex = document.Levels.FindIndex(level => level.Id == levelId);
            var level = new HskLevel(levelId, versionId, item.LevelNumber, item.DisplayName!.Trim(), ContentStatus.Draft, now);
            if (levelIndex >= 0)
            {
                var current = document.Levels[levelIndex];
                document.Levels[levelIndex] = level with { Status = current.Status };
            }
            else
            {
                document.Levels.Add(level);
            }
        }

        Write(document);
        return new CurriculumImportResult(versionId, request.Levels.Count, wasAlreadyPresent);
    }

    public HskLevel? PublishLevel(string levelId)
    {
        var document = Read();
        var index = document.Levels.FindIndex(level => level.Id == levelId);
        if (index < 0)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var published = document.Levels[index] with { Status = ContentStatus.Published, UpdatedAt = now };
        document.Levels[index] = published;
        var versionIndex = document.SyllabusVersions.FindIndex(version => version.Id == published.SyllabusVersionId);
        if (versionIndex < 0)
        {
            return null;
        }
        document.SyllabusVersions[versionIndex] = document.SyllabusVersions[versionIndex] with { Status = ContentStatus.Published, UpdatedAt = now };
        Write(document);
        return published;
    }

    private CurriculumDocument Read()
    {
        using var session = documentStore.QuerySession();
        return session.LoadAsync<CurriculumDocument>(RootId).GetAwaiter().GetResult()
            ?? new CurriculumDocument(RootId, [], []);
    }

    private void Write(CurriculumDocument document)
    {
        using var session = documentStore.LightweightSession();
        session.Store(document);
        session.SaveChangesAsync().GetAwaiter().GetResult();
    }
}
