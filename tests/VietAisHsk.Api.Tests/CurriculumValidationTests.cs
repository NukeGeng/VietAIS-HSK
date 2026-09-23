using VietAisHsk.Api.Modules.Curriculum;

namespace VietAisHsk.Api.Tests;

public sealed class CurriculumValidationTests
{
    [Fact]
    public void ValidateImport_requires_provenance_and_level_data()
    {
        var result = CurriculumValidation.ValidateImport(new CurriculumImportRequest("", "", "", []));

        Assert.Equal("SyllabusVersionId là bắt buộc.", result);
    }

    [Fact]
    public void ValidateImport_rejects_duplicate_level_numbers()
    {
        var result = CurriculumValidation.ValidateImport(new CurriculumImportRequest(
            "source-v1",
            "HSK 3.0",
            "official-import",
            [
                new HskLevelImportItem("hsk3-a", 3, "HSK 3"),
                new HskLevelImportItem("hsk3-b", 3, "HSK 3 duplicate")
            ]));

        Assert.Equal("Trùng HSK level number: 3.", result);
    }

    [Fact]
    public void ValidateImport_rejects_duplicate_lesson_ids_across_the_syllabus()
    {
        var result = CurriculumValidation.ValidateImport(new CurriculumImportRequest(
            "source-v1",
            "HSK 3.0",
            "approved-import-source",
            [
                new HskLevelImportItem("hsk3", 3, "HSK 3", [
                    new TopicImportItem("topic-1", "Chào hỏi", [
                        new UnitImportItem("unit-1", "Làm quen", [new LessonImportItem("lesson-1", "Xin chào")])
                    ]),
                    new TopicImportItem("topic-2", "Sinh hoạt", [
                        new UnitImportItem("unit-2", "Ở nhà", [new LessonImportItem("lesson-1", "Đồ vật")])
                    ])
                ])
            ]));

        Assert.Equal("Trùng Lesson Id: lesson-1.", result);
    }

    [Fact]
    public void Pinyin_catalog_exposes_the_initial_and_final_inventory_with_provenance()
    {
        var catalog = new InMemoryCurriculumStore().GetPinyinCatalog();

        Assert.Equal("PlatformAuthored", catalog.SourceType);
        Assert.Equal("foundation-v1", catalog.Version);
        Assert.Equal(21, catalog.Items.Count(item => item.Id.StartsWith("initial-", StringComparison.Ordinal)));
        Assert.Equal(35, catalog.Items.Count(item => item.Id.StartsWith("final-", StringComparison.Ordinal)));
        Assert.Contains(catalog.Sources, source => source.Url.Contains("moe.gov.cn", StringComparison.Ordinal));
        Assert.Contains(catalog.Sources, source => source.Url.Contains("openstd.samr.gov.cn", StringComparison.Ordinal));
        Assert.Contains("không phải dữ liệu syllabus HSK/CTI", catalog.EditorialNote, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Tone_catalog_has_four_tones_neutral_tone_and_a_connected_speech_rule()
    {
        var catalog = new InMemoryCurriculumStore().GetToneCatalog();
        var toneItems = catalog.Items.Where(item => item.Category == "Thanh điệu").ToArray();

        Assert.Equal([0, 1, 2, 3, 4], toneItems.Select(item => item.Tone ?? -1).Order().ToArray());
        Assert.Equal(["mā", "má", "mǎ", "mà", "ma"], toneItems.Select(item => item.Pinyin!).ToArray());
        Assert.Contains(catalog.Items, item => item.Category == "Quy tắc" && item.Pinyin == "nǐ hǎo → ní hǎo");
    }

    [Fact]
    public void Beginner_track_only_marks_available_foundation_pages_as_published()
    {
        var stages = new InMemoryCurriculumStore().GetBeginnerTrack().Stages;

        Assert.Equal(7, stages.Count);
        Assert.Equal(["pinyin", "tones"], stages.Where(stage => stage.Status == ContentStatus.Published).Select(stage => stage.Id).ToArray());
        Assert.Equal(
            ["initials-finals", "syllable-blending", "basic-hanzi", "basic-strokes", "familiarization"],
            stages.Where(stage => stage.Status == ContentStatus.Draft).Select(stage => stage.Id).ToArray());
    }
}
