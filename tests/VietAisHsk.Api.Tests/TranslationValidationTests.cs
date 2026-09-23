using VietAisHsk.Api.Modules.Translation;

namespace VietAisHsk.Api.Tests;

public sealed class TranslationValidationTests
{
    [Fact]
    public void Feedback_accepts_complete_structured_response()
    {
        var result = TranslationValidation.ValidateFeedback(new TranslationFeedback(
            "Đúng ý chính.",
            "Cấu trúc phù hợp.",
            "Từ dùng đúng ngữ cảnh.",
            "Tự nhiên.",
            "我喜欢学习中文。",
            "ready"));

        Assert.Null(result);
    }

    [Theory]
    [InlineData("", "Cấu trúc", "Từ", "Tự nhiên", "Sửa", "ready")]
    [InlineData("Ý", "", "Từ", "Tự nhiên", "Sửa", "ready")]
    [InlineData("Ý", "Cấu trúc", "", "Tự nhiên", "Sửa", "ready")]
    [InlineData("Ý", "Cấu trúc", "Từ", "", "Sửa", "ready")]
    [InlineData("Ý", "Cấu trúc", "Từ", "Tự nhiên", "", "ready")]
    [InlineData("Ý", "Cấu trúc", "Từ", "Tự nhiên", "Sửa", "")]
    public void Feedback_rejects_missing_required_field(
        string meaning,
        string grammar,
        string wordChoice,
        string naturalness,
        string suggestedRevision,
        string status)
    {
        var result = TranslationValidation.ValidateFeedback(new TranslationFeedback(
            meaning,
            grammar,
            wordChoice,
            naturalness,
            suggestedRevision,
            status));

        Assert.NotNull(result);
    }

    [Fact]
    public void Feedback_rejects_oversized_field()
    {
        var result = TranslationValidation.ValidateFeedback(new TranslationFeedback(
            new string('x', 4_001),
            "Ngữ pháp",
            "Dùng từ",
            "Tự nhiên",
            "Gợi ý",
            "ready"));

        Assert.NotNull(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Answer_requires_non_empty_text(string? answer)
    {
        Assert.NotNull(TranslationValidation.ValidateAnswer(answer));
    }
}
