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
}
