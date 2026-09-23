namespace VietAisHsk.Api.Modules.Curriculum;

/// <summary>
/// Small platform-authored reference catalog for the first production contracts.
/// It is deliberately not labelled as the official CTI/MOE HSK 3.0 dataset.
/// </summary>
public sealed class BootstrapKnowledgeCatalog : IVocabularyCatalog, IGrammarCatalog
{
    private const string Source = "PlatformAuthoredReferenceFixture";
    private const string Version = "knowledge-reference-v1";
    private const string License = "platform-authored";

    private static readonly IReadOnlyList<VocabularyEntry> Vocabulary =
    [
        new("vocab-xuexi", "学习", "xuéxí", "học tập", "Động từ", "HSK 1", "Học tập", ["学", "习"],
            [new("我每天学习汉语。", "Wǒ měitiān xuéxí Hànyǔ.", "Mỗi ngày tôi học tiếng Trung.")], "Published", Source, Version, License),
        new("vocab-gongzuo", "工作", "gōngzuò", "công việc; làm việc", "Danh từ · Động từ", "HSK 1", "Sinh hoạt", ["工", "作"],
            [new("我在学校工作。", "Wǒ zài xuéxiào gōngzuò.", "Tôi làm việc ở trường.")], "Published", Source, Version, License),
        new("vocab-anpai", "安排", "ānpái", "sắp xếp", "Động từ", "HSK 3", "Công việc", ["安", "排"],
            [new("请安排一下时间。", "Qǐng ānpái yíxià shíjiān.", "Hãy sắp xếp thời gian một chút.")], "Published", Source, Version, License),
        new("vocab-jingyan", "经验", "jīngyàn", "kinh nghiệm", "Danh từ", "HSK 3", "Công việc", ["经", "验"],
            [new("他有很多工作经验。", "Tā yǒu hěn duō gōngzuò jīngyàn.", "Anh ấy có nhiều kinh nghiệm làm việc.")], "Published", Source, Version, License),
    ];

    private static readonly IReadOnlyList<GrammarPoint> Grammar =
    [
        new("grammar-zhengzai", "正在 + V", "Hành động đang diễn ra", "Đặt 正在 trước động từ để nói một hành động đang xảy ra tại thời điểm nói.", "HSK 3", "Thời gian", ["Đặt 正在 sau động từ chính.", "Dùng 正在 cho hành động đã kết thúc."],
            [new("我正在学习汉语。", "Wǒ zhèngzài xuéxí Hànyǔ.", "Tôi đang học tiếng Trung.")], "lesson-02", "Published", Source, Version, License),
        new("grammar-yijing-le", "已经 + V + 了", "Hành động đã hoàn thành", "Dùng 已经 kết hợp với 了 để nhấn mạnh một hành động đã hoàn thành.", "HSK 3", "Thời gian", ["Bỏ 了 khi cần nhấn mạnh kết quả đã xảy ra.", "Nhầm 已经 với 正在 trong cùng ngữ cảnh."],
            [new("我已经吃饭了。", "Wǒ yǐjīng chīfàn le.", "Tôi đã ăn cơm rồi.")], "lesson-02", "Published", Source, Version, License),
        new("grammar-yibian", "一边…一边…", "Hai hành động song song", "Dùng để nói hai hành động diễn ra đồng thời và thường có cùng chủ thể.", "HSK 3", "Hành động", ["Dùng hai chủ thể khác nhau mà không thêm cấu trúc phù hợp.", "Thiếu 一边 ở vế thứ hai."],
            [new("她一边听音乐，一边学习。", "Tā yìbiān tīng yīnyuè, yìbiān xuéxí.", "Cô ấy vừa nghe nhạc vừa học.")], "lesson-03", "Published", Source, Version, License),
        new("grammar-ba", "把 + O + V", "Đưa tân ngữ lên trước động từ", "Cấu trúc 把 nhấn mạnh cách chủ thể xử lý hoặc thay đổi một đối tượng cụ thể.", "HSK 3", "Câu có tân ngữ", ["Dùng 把 với tân ngữ không xác định.", "Quên thành phần kết quả hoặc vị trí sau động từ."],
            [new("请把书放在桌子上。", "Qǐng bǎ shū fàng zài zhuōzi shàng.", "Hãy đặt sách lên trên bàn.")], "lesson-04", "Published", Source, Version, License),
    ];

    public IReadOnlyList<VocabularyEntry> SearchVocabulary(string? search, string? hskLevel, string? topic) =>
        Vocabulary.Where(item => Matches(item.Simplified, item.Pinyin, item.Meaning, item.Topic, search))
            .Where(item => string.IsNullOrWhiteSpace(hskLevel) || item.HskLevel.Equals(hskLevel.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(item => string.IsNullOrWhiteSpace(topic) || item.Topic.Contains(topic.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToArray();

    public VocabularyEntry? GetVocabulary(string id) => Vocabulary.FirstOrDefault(item =>
        item.Id.Equals(id, StringComparison.OrdinalIgnoreCase) || item.Simplified.Equals(id, StringComparison.Ordinal));

    public IReadOnlyList<GrammarPoint> SearchGrammar(string? search, string? hskLevel, string? topic) =>
        Grammar.Where(item => Matches(item.Pattern, item.Title, item.Explanation, item.Topic, search))
            .Where(item => string.IsNullOrWhiteSpace(hskLevel) || item.HskLevel.Equals(hskLevel.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(item => string.IsNullOrWhiteSpace(topic) || item.Topic.Contains(topic.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToArray();

    public GrammarPoint? GetGrammar(string id) => Grammar.FirstOrDefault(item =>
        item.Id.Equals(id, StringComparison.OrdinalIgnoreCase) || item.Pattern.Equals(id, StringComparison.Ordinal));

    private static bool Matches(string first, string second, string third, string fourth, string? query) =>
        string.IsNullOrWhiteSpace(query)
        || first.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase)
        || second.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase)
        || third.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase)
        || fourth.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase);
}
