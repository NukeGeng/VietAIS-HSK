using VietAisHsk.Api.Modules.Content;
using VietAisHsk.Api.Modules.Practice;

namespace VietAisHsk.Api.Tests;

public sealed class PracticeQuestionCatalogTests
{
    [Theory]
    [InlineData("listening", "bootstrap-listening-classroom")]
    [InlineData("reading", "bootstrap-reading-schedule")]
    [InlineData("writing", "bootstrap-writing-routine")]
    [InlineData("hanzi", "bootstrap-hanzi-xue")]
    [InlineData("pinyin", "bootstrap-pinyin-nihao")]
    public void Skill_filter_returns_only_questions_for_requested_type(string type, string expectedId)
    {
        var reader = new ContentPracticeQuestionReader(new BootstrapQuestionBank());

        var questions = reader.GetPublishedQuestions(type);

        var question = Assert.Single(questions);
        Assert.Equal(expectedId, question.Id);
        Assert.Equal(type, question.Type);
    }

    [Fact]
    public void Empty_or_unknown_filter_has_explicit_catalog_behavior()
    {
        var reader = new ContentPracticeQuestionReader(new BootstrapQuestionBank());

        Assert.Equal(8, reader.GetPublishedQuestions().Count);
        Assert.Empty(reader.GetPublishedQuestions("not-a-skill"));
    }

    [Fact]
    public void Practice_question_keeps_multiple_choice_options()
    {
        var reader = new ContentPracticeQuestionReader(new BootstrapQuestionBank());

        var question = Assert.Single(reader.GetPublishedQuestions("hanzi"));

        Assert.Equal(["学", "校", "字", "语"], question.Options);
    }
}
