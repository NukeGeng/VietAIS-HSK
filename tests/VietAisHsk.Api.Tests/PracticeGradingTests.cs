using VietAisHsk.Api.Modules.Practice;

namespace VietAisHsk.Api.Tests;

public sealed class PracticeGradingTests
{
    [Fact]
    public void Grade_accepts_case_and_whitespace_variants()
    {
        var question = new PracticeQuestion("q1", "Vocabulary", "你好", ["ni hao"]);

        var result = PracticeGrading.Grade(question, "  NI   HAO ");

        Assert.Equal(PracticeResult.Correct, result);
    }

    [Fact]
    public void Grade_rejects_answer_outside_accepted_set()
    {
        var question = new PracticeQuestion("q1", "Vocabulary", "你好", ["ni hao"]);

        var result = PracticeGrading.Grade(question, "再见");

        Assert.Equal(PracticeResult.Incorrect, result);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("thanh 1")]
    public void Tone_options_use_the_same_deterministic_accepted_answer_rule(string answer)
    {
        var question = new PracticeQuestion("q-tone", "tone", "mā", ["1", "thanh 1"], Options: ["1", "2", "3", "4"]);

        Assert.Equal(PracticeResult.Correct, PracticeGrading.Grade(question, answer));
    }
}
