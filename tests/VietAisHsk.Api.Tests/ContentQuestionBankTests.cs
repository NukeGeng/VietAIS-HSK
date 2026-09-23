using VietAisHsk.Api.Modules.Content;

namespace VietAisHsk.Api.Tests;

public sealed class ContentQuestionBankTests
{
    [Fact]
    public void Published_question_query_filters_by_type_and_preserves_provenance()
    {
        var bank = new BootstrapQuestionBank();

        var question = Assert.Single(bank.GetPublishedQuestions("reading"));

        Assert.Equal("bootstrap-reading-schedule", question.Id);
        Assert.Equal("PlatformAuthoredReferenceFixture", question.SourceType);
        Assert.Equal("question-reference-v1", question.SourceVersion);
        Assert.Equal("platform-authored", question.LicenseRef);
    }

    [Fact]
    public void Id_query_is_published_only_and_deduplicated()
    {
        var bank = new BootstrapQuestionBank();

        var questions = bank.GetPublishedQuestions(["bootstrap-vocab-hello", "BOOTSTRAP-VOCAB-HELLO"]);

        Assert.Single(questions);
        Assert.Equal("bootstrap-vocab-hello", questions[0].Id);
    }

    [Fact]
    public void Multiple_choice_question_exposes_options_without_changing_provenance()
    {
        var bank = new BootstrapQuestionBank();

        var question = Assert.Single(bank.GetPublishedQuestions("tone"));

        Assert.Equal(["1", "2", "3", "4"], question.Options);
        Assert.Equal("PlatformAuthoredReferenceFixture", question.SourceType);
    }

    [Fact]
    public void Draft_question_is_admin_visible_but_not_learner_visible_until_publish()
    {
        var bank = new BootstrapQuestionBank();

        Assert.Contains(bank.GetAllQuestions(), question => question.Id == "bootstrap-listening-draft");
        Assert.DoesNotContain(bank.GetPublishedQuestions(), question => question.Id == "bootstrap-listening-draft");

        var published = bank.Publish("bootstrap-listening-draft");

        Assert.Equal("Published", published?.Status);
        Assert.Contains(bank.GetPublishedQuestions("listening"), question => question.Id == "bootstrap-listening-draft");
    }

    [Fact]
    public void Saving_a_draft_increments_content_version_and_published_question_is_immutable()
    {
        var bank = new BootstrapQuestionBank();
        var request = new SaveQuestionDraftRequest(
            "reading",
            "Đọc: 你好吗? Có nghĩa là gì?",
            ["Bạn khỏe không?"],
            null,
            "HSK 3",
            "Đọc",
            "vocab-nihao",
            "Câu hỏi chào hỏi cơ bản.",
            1,
            "PlatformAuthoredReferenceFixture",
            "question-reference-v1",
            "platform-authored");

        var first = bank.SaveDraft("question-admin-draft", request);
        var second = bank.SaveDraft("question-admin-draft", request);

        Assert.Equal(1, first?.ContentVersion);
        Assert.Equal(2, second?.ContentVersion);
        Assert.Null(bank.SaveDraft("bootstrap-reading-schedule", request));
    }
}
