namespace VietAisHsk.Api.Modules.Content;

public sealed record ContentQuestion(
    string Id,
    string Type,
    string Prompt,
    IReadOnlyList<string> AcceptedAnswers,
    string Status,
    string SourceType,
    string SourceVersion,
    string LicenseRef,
    IReadOnlyList<string>? Options = null,
    string? HskLevel = null,
    string? Skill = null,
    string? KnowledgeId = null,
    string? Explanation = null,
    int? Difficulty = null,
    int ContentVersion = 1);

public sealed record SaveQuestionDraftRequest(
    string? Type,
    string? Prompt,
    IReadOnlyList<string>? AcceptedAnswers,
    IReadOnlyList<string>? Options,
    string? HskLevel,
    string? Skill,
    string? KnowledgeId,
    string? Explanation,
    int? Difficulty,
    string? SourceType,
    string? SourceVersion,
    string? LicenseRef);

public sealed record ContentQuestionView(
    string Id,
    string Type,
    string Prompt,
    string Status,
    string? HskLevel,
    string? Skill,
    string? KnowledgeId,
    string? Explanation,
    int? Difficulty,
    int ContentVersion,
    IReadOnlyList<string>? Options = null);

public interface IQuestionBank
{
    IReadOnlyList<ContentQuestion> GetPublishedQuestions(string? type = null);
    IReadOnlyList<ContentQuestion> GetPublishedQuestions(IReadOnlyList<string> questionIds);
}

public interface IQuestionBankAdmin
{
    IReadOnlyList<ContentQuestion> GetAllQuestions(string? type = null);
    ContentQuestion? GetQuestion(string id);
    ContentQuestion? SaveDraft(string id, SaveQuestionDraftRequest request);
    ContentQuestion? Publish(string id);
}

/// <summary>
/// Platform-authored contract fixture until an approved HSK question dataset is imported.
/// The catalog is internal to Content; learner responses are mapped by Practice without answer keys.
/// </summary>
public sealed class BootstrapQuestionBank : IQuestionBank, IQuestionBankAdmin
{
    private const string Source = "PlatformAuthoredReferenceFixture";
    private const string Version = "question-reference-v1";
    private const string License = "platform-authored";

    private readonly object gate = new();
    private readonly Dictionary<string, ContentQuestion> questions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["bootstrap-vocab-hello"] = Question("bootstrap-vocab-hello", "vocabulary", "Dịch 你好 sang tiếng Việt.", ["xin chào", "chào bạn"], hskLevel: "HSK 3", skill: "Từ vựng", knowledgeId: "vocab-hello"),
            ["bootstrap-pinyin-nihao"] = Question("bootstrap-pinyin-nihao", "pinyin", "Viết Pinyin của 你好 (không cần dấu thanh).", ["ni hao", "nihao"], hskLevel: "HSK 3", skill: "Pinyin"),
            ["bootstrap-tone-ma"] = Question("bootstrap-tone-ma", "tone", "Chọn âm đúng cho mā.", ["1", "thanh 1"], ["1", "2", "3", "4"], hskLevel: "HSK 3", skill: "Thanh điệu"),
            ["bootstrap-hanzi-xue"] = Question("bootstrap-hanzi-xue", "hanzi", "Chọn chữ Hán có nghĩa là học tập.", ["学"], ["学", "校", "字", "语"], hskLevel: "HSK 3", skill: "Chữ Hán", knowledgeId: "hanzi-xue"),
            ["bootstrap-grammar-zhengzai"] = Question("bootstrap-grammar-zhengzai", "grammar", "Chọn cấu trúc diễn tả hành động đang diễn ra.", ["正在"], ["正在", "已经", "把", "一边…一边…"], hskLevel: "HSK 3", skill: "Ngữ pháp", knowledgeId: "grammar-zhengzai"),
            ["bootstrap-listening-classroom"] = Question("bootstrap-listening-classroom", "listening", "Nghe: 他们在哪儿上课? Trả lời địa điểm bằng tiếng Việt.", ["ở lớp học", "trong lớp học"], hskLevel: "HSK 3", skill: "Nghe"),
            ["bootstrap-reading-schedule"] = Question("bootstrap-reading-schedule", "reading", "Đọc: 他每天七点起床。Anh ấy dậy lúc mấy giờ?", ["bảy giờ", "7 giờ", "7"], hskLevel: "HSK 3", skill: "Đọc"),
            ["bootstrap-writing-routine"] = Question("bootstrap-writing-routine", "writing", "Sắp xếp câu: 每天 / 我 / 学习 / 汉语", ["我每天学习汉语", "我每天学习汉语。"], hskLevel: "HSK 3", skill: "Viết"),
            ["bootstrap-listening-draft"] = Question("bootstrap-listening-draft", "listening", "Nghe đoạn hội thoại ở lớp học và chọn địa điểm.", ["ở lớp học"], ["ở lớp học", "ở nhà"], "Draft", hskLevel: "HSK 3", skill: "Nghe")
        };

    public IReadOnlyList<ContentQuestion> GetPublishedQuestions(string? type = null) =>
        GetAllQuestions(type)
            .Where(question => question.Status == "Published")
            .ToArray();

    public IReadOnlyList<ContentQuestion> GetPublishedQuestions(IReadOnlyList<string> questionIds) =>
        GetAllQuestions()
            .Where(question => question.Status == "Published")
            .Where(questionIds.ContainsById)
            .OrderBy(question => questionIds.FindIndex(id => id.Equals(question.Id, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

    public IReadOnlyList<ContentQuestion> GetAllQuestions(string? type = null)
    {
        lock (gate)
        {
            return questions.Values
                .Where(question => string.IsNullOrWhiteSpace(type)
                    || question.Type.Equals(type.Trim(), StringComparison.OrdinalIgnoreCase))
                .OrderBy(question => question.Id, StringComparer.Ordinal)
                .ToArray();
        }
    }

    public ContentQuestion? GetQuestion(string id)
    {
        lock (gate)
        {
            return questions.TryGetValue(id.Trim(), out var question) ? question : null;
        }
    }

    public ContentQuestion? SaveDraft(string id, SaveQuestionDraftRequest request)
    {
        lock (gate)
        {
            if (questions.TryGetValue(id, out var existing) && existing.Status == "Published")
            {
                return null;
            }

            var nextVersion = existing?.ContentVersion + 1 ?? 1;
            var sourceType = string.IsNullOrWhiteSpace(request.SourceType) ? Source : request.SourceType.Trim();
            var sourceVersion = string.IsNullOrWhiteSpace(request.SourceVersion) ? Version : request.SourceVersion.Trim();
            var license = string.IsNullOrWhiteSpace(request.LicenseRef) ? License : request.LicenseRef.Trim();
            var question = new ContentQuestion(
                id.Trim(),
                request.Type!.Trim(),
                request.Prompt!.Trim(),
                request.AcceptedAnswers!.Select(answer => answer.Trim()).Where(answer => answer.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                "Draft",
                sourceType,
                sourceVersion,
                license,
                request.Options?.Select(option => option.Trim()).Where(option => option.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                request.HskLevel?.Trim(),
                request.Skill?.Trim(),
                request.KnowledgeId?.Trim(),
                request.Explanation?.Trim(),
                request.Difficulty,
                nextVersion);
            questions[question.Id] = question;
            return question;
        }
    }

    public ContentQuestion? Publish(string id)
    {
        lock (gate)
        {
            if (!questions.TryGetValue(id.Trim(), out var current)) return null;
            questions[current.Id] = current with { Status = "Published" };
            return questions[current.Id];
        }
    }

    private static ContentQuestion Question(
        string id,
        string type,
        string prompt,
        IReadOnlyList<string> acceptedAnswers,
        IReadOnlyList<string>? options = null,
        string status = "Published",
        string? hskLevel = null,
        string? skill = null,
        string? knowledgeId = null,
        string? explanation = null,
        int? difficulty = null,
        int contentVersion = 1) =>
        new(id, type, prompt, acceptedAnswers, status, Source, Version, License, options, hskLevel, skill, knowledgeId, explanation, difficulty, contentVersion);
}

internal static class QuestionIdCollectionExtensions
{
    public static bool ContainsById(this IReadOnlyList<string> ids, ContentQuestion question) =>
        ids.Any(id => id.Equals(question.Id, StringComparison.OrdinalIgnoreCase));

    public static int FindIndex(this IReadOnlyList<string> ids, Func<string, bool> predicate)
    {
        for (var index = 0; index < ids.Count; index++)
        {
            if (predicate(ids[index])) return index;
        }

        return int.MaxValue;
    }
}
