namespace VietAisHsk.Api.Modules.Translation;

public static class TranslationValidation
{
    private const int MaxFeedbackFieldLength = 4_000;

    public static string? ValidateAnswer(string? answerChinese) =>
        string.IsNullOrWhiteSpace(answerChinese)
            ? "AnswerChinese là bắt buộc."
            : null;

    public static string? ValidateFeedback(TranslationFeedback? feedback)
    {
        if (feedback is null)
        {
            return "Feedback response không được rỗng.";
        }

        var fields = new (string Name, string? Value)[]
        {
            (nameof(feedback.Meaning), feedback.Meaning),
            (nameof(feedback.Grammar), feedback.Grammar),
            (nameof(feedback.WordChoice), feedback.WordChoice),
            (nameof(feedback.Naturalness), feedback.Naturalness),
            (nameof(feedback.SuggestedRevision), feedback.SuggestedRevision),
            (nameof(feedback.Status), feedback.Status),
        };

        foreach (var field in fields)
        {
            if (string.IsNullOrWhiteSpace(field.Value))
            {
                return $"Feedback field {field.Name} là bắt buộc.";
            }

            if (field.Value.Length > MaxFeedbackFieldLength)
            {
                return $"Feedback field {field.Name} vượt quá giới hạn cho phép.";
            }
        }

        return null;
    }
}
