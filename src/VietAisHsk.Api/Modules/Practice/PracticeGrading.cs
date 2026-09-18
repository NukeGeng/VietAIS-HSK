namespace VietAisHsk.Api.Modules.Practice;

public static class PracticeGrading
{
    public static PracticeResult Grade(PracticeQuestion question, string answer)
    {
        var normalizedAnswer = Normalize(answer);
        var accepted = question.AcceptedAnswers
            .Select(Normalize)
            .Where(value => value.Length > 0);

        return accepted.Contains(normalizedAnswer, StringComparer.OrdinalIgnoreCase)
            ? PracticeResult.Correct
            : PracticeResult.Incorrect;
    }

    private static string Normalize(string value) =>
        string.Join(' ', value.Normalize().Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
