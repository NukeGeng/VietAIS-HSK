namespace VietAisHsk.Api.Modules.Curriculum;

/// <summary>
/// Small, platform-authored reference fixture used until the approved HSK/Hanzi
/// dataset and its stroke-license decision are imported through Curriculum admin.
/// It is deliberately not presented as an official HSK dataset.
/// </summary>
public sealed class BootstrapHanziCatalog : IHanziCatalog
{
    private const string Source = "PlatformAuthoredReferenceFixture";
    private const string Version = "hanzi-reference-v1";
    private const string License = "platform-authored";

    private static readonly IReadOnlyList<HanziCharacter> Characters =
    [
        new("hanzi-yi", "一", "yī", "một", "一", 1, "Nền tảng", ["一天", "第一"], Source, Version, License),
        new("hanzi-ren", "人", "rén", "người", "人", 2, "Nền tảng", ["人民", "学生"], Source, Version, License),
        new("hanzi-da", "大", "dà", "lớn", "大", 3, "HSK 1", ["大家", "大学"], Source, Version, License),
    ];

    private static readonly IReadOnlyDictionary<string, HanziStrokeSet> StrokeSets =
        new Dictionary<string, HanziStrokeSet>(StringComparer.OrdinalIgnoreCase)
        {
            ["hanzi-yi"] = Set("hanzi-yi", 1, [Stroke("yi-1", 1, "Ngang", "M 16 50 Q 50 48 84 50")]),
            ["hanzi-ren"] = Set("hanzi-ren", 2, [
                Stroke("ren-1", 1, "Phẩy", "M 51 16 Q 46 43 20 80"),
                Stroke("ren-2", 2, "Mác", "M 50 18 Q 55 49 84 81"),
            ]),
            ["hanzi-da"] = Set("hanzi-da", 3, [
                Stroke("da-1", 1, "Ngang", "M 16 44 Q 50 42 84 44"),
                Stroke("da-2", 2, "Phẩy", "M 52 16 Q 45 50 19 82"),
                Stroke("da-3", 3, "Mác", "M 50 45 Q 57 61 84 82"),
            ]),
        };

    public IReadOnlyList<HanziCharacter> GetAll() => Characters;

    public HanziCharacter? Get(string id) => Characters.FirstOrDefault(item =>
        string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase)
        || string.Equals(item.Character, id, StringComparison.Ordinal));

    public HanziStrokeSet? GetStrokes(string id)
    {
        var character = Get(id);
        return character is null || !StrokeSets.TryGetValue(character.Id, out var set) ? null : set;
    }

    private static HanziStroke Stroke(string id, int order, string description, string path) =>
        new(id, order, description, path);

    private static HanziStrokeSet Set(string hanziId, int strokeCount, IReadOnlyList<HanziStroke> strokes) =>
        new(hanziId, strokeCount, strokes, Source, Version, License);
}
