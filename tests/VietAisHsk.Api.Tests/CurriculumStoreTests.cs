using VietAisHsk.Api.Modules.Curriculum;

namespace VietAisHsk.Api.Tests;

public sealed class CurriculumStoreTests
{
    [Fact]
    public void Published_tree_contains_only_published_lessons_and_learning_guard_matches_it()
    {
        var store = new InMemoryCurriculumStore();
        var request = CreateImport();
        store.Import(request);

        Assert.False(store.IsPublishedLesson("lesson-ready"));
        Assert.Null(store.GetPublishedTree("hsk-3"));
        Assert.Null(store.PublishLesson("hsk-3", "lesson-ready"));

        store.PublishLevel("hsk-3");
        var treeBeforeLessonPublish = store.GetPublishedTree("hsk-3");
        Assert.NotNull(treeBeforeLessonPublish);
        Assert.Empty(treeBeforeLessonPublish.Topics);
        Assert.False(store.IsPublishedLesson("lesson-ready"));

        var publishedLesson = store.PublishLesson("hsk-3", "lesson-ready");

        Assert.Equal(ContentStatus.Published, publishedLesson?.Status);
        Assert.True(store.IsPublishedLesson("lesson-ready"));
        Assert.False(store.IsPublishedLesson("lesson-draft"));
        Assert.Equal("Chào hỏi", store.GetPublishedLesson("lesson-ready")?.Name);
        Assert.Null(store.GetPublishedLesson("lesson-draft"));
        var detail = store.GetPublishedLessonDetail("lesson-ready");
        Assert.Equal("HSK 3", detail?.Level.DisplayName);
        Assert.Equal("Giao tiếp cơ bản", detail?.TopicName);
        Assert.Equal("Chào hỏi", detail?.UnitName);
        var visibleLessons = Assert.Single(Assert.Single(Assert.Single(store.GetPublishedTree("hsk-3")!.Topics).Units).Lessons);
        Assert.Equal("lesson-ready", visibleLessons.Id);
    }

    [Fact]
    public void Published_version_accepts_identical_retry_but_rejects_mutation()
    {
        var store = new InMemoryCurriculumStore();
        var request = CreateImport();
        store.Import(request);
        store.PublishLevel("hsk-3");

        var retry = store.Import(request);

        Assert.True(retry.Idempotent);
        Assert.Throws<PublishedCurriculumVersionException>(() => store.Import(request with
        {
            Levels = [request.Levels![0] with { Topics = [new TopicImportItem("topic-1", "Nội dung đã đổi")] }]
        }));
    }

    [Fact]
    public void New_version_cannot_reuse_a_level_id_from_another_version()
    {
        var store = new InMemoryCurriculumStore();
        var first = CreateImport();
        store.Import(first);
        store.PublishLevel("hsk-3");

        var second = first with
        {
            SyllabusVersionId = "syllabus-v2",
            SyllabusName = "HSK 3.0 revision",
            SourceType = "approved-import-source-v2"
        };

        var exception = Assert.Throws<CurriculumLevelIdConflictException>(() => store.Import(second));

        Assert.Contains("hsk-3", exception.Message, StringComparison.Ordinal);
        Assert.Contains("syllabus-v1", exception.Message, StringComparison.Ordinal);
        Assert.Contains("syllabus-v2", exception.Message, StringComparison.Ordinal);
        Assert.Equal("syllabus-v1", Assert.Single(store.GetAdminLevels()).SyllabusVersionId);
        Assert.Equal("syllabus-v1", Assert.Single(store.GetPublishedLevels()).SyllabusVersionId);
    }

    [Fact]
    public void Published_levels_expose_only_the_latest_version_for_a_level_number()
    {
        var store = new InMemoryCurriculumStore();
        var first = CreateImport();
        store.Import(first);
        store.PublishLevel("hsk-3");

        var second = first with
        {
            SyllabusVersionId = "syllabus-v2",
            Levels = [first.Levels![0] with { Id = "hsk-3-v2" }]
        };
        store.Import(second);
        store.PublishLevel("hsk-3-v2");

        var visible = Assert.Single(store.GetPublishedLevels());

        Assert.Equal("hsk-3-v2", visible.Id);
        Assert.Null(store.GetPublishedTree("hsk-3"));
        Assert.NotNull(store.GetPublishedTree("hsk-3-v2"));
    }

    [Fact]
    public void Beginner_stages_reference_curriculum_master_data_in_order()
    {
        var stages = new InMemoryCurriculumStore().GetBeginnerTrack().Stages;

        Assert.Equal(["foundation:pinyin"], stages[0].MasterDataRefs);
        Assert.Equal(["foundation:tones"], stages[1].MasterDataRefs);
        Assert.Equal(["hanzi:catalog"], stages[4].MasterDataRefs);
        Assert.Equal(["hanzi:strokes"], stages[5].MasterDataRefs);
        Assert.All(stages, stage => Assert.NotEmpty(stage.MasterDataRefs ?? []));
    }

    [Fact]
    public void Admin_level_query_includes_draft_levels_while_public_query_does_not()
    {
        var store = new InMemoryCurriculumStore();
        store.Import(CreateImport());

        var adminLevels = store.GetAdminLevels();

        Assert.Contains(adminLevels, level => level.Id == "hsk-3" && level.Status == ContentStatus.Draft);
        Assert.Empty(store.GetPublishedLevels());
    }

    private static CurriculumImportRequest CreateImport() => new(
        "syllabus-v1",
        "HSK 3.0",
        "approved-import-source",
        [
            new HskLevelImportItem(
                "hsk-3",
                3,
                "HSK 3",
                [
                    new TopicImportItem(
                        "topic-1",
                        "Giao tiếp cơ bản",
                        [
                            new UnitImportItem(
                                "unit-1",
                                "Chào hỏi",
                                [
                                    new LessonImportItem("lesson-ready", "Chào hỏi"),
                                    new LessonImportItem("lesson-draft", "Giới thiệu bản thân")
                                ])
                        ])
                ])
        ]);
}
