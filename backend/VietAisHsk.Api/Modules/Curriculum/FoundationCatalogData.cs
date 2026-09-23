namespace VietAisHsk.Api.Modules.Curriculum;

public static class FoundationCatalogData
{
    private static readonly IReadOnlyList<FoundationSource> Sources = Array.AsReadOnly(
    [
        new FoundationSource(
            "汉语拼音方案 · Bộ Giáo dục Trung Quốc",
            "https://www.moe.gov.cn/jyb_sjzl/ziliao/A19/195802/t19580201_186000.html",
            "Bảng chữ cái, âm đầu, vận mẫu và ký hiệu thanh điệu."),
        new FoundationSource(
            "GB/T 16159-2012 · Quy tắc chính tả Pinyin",
            "https://openstd.samr.gov.cn/bzgk/std/newGbInfo?hcno=5645BD8DB9D8D73053AD3A2397E15E74",
            "Chuẩn chính tả Pinyin hiện hành; dùng để đối chiếu quy tắc ghi âm."),
    ]);

    private static readonly IReadOnlyList<FoundationItem> PinyinItems = Array.AsReadOnly(
    [
        Initial("b", "Âm môi", "bā", 1, "八", "tám", "Khép hai môi rồi mở nhẹ; không bật hơi mạnh."),
        Initial("p", "Âm môi", "pā", 1, "趴", "nằm sấp", "Khép hai môi rồi bật hơi rõ."),
        Initial("m", "Âm môi", "mā", 1, "妈", "mẹ", "Khép môi, cho luồng hơi thoát qua mũi."),
        Initial("f", "Âm môi-răng", "fēi", 1, "飞", "bay", "Răng trên chạm nhẹ môi dưới, tạo âm xát."),
        Initial("d", "Âm đầu lưỡi", "dà", 4, "大", "lớn", "Đầu lưỡi chạm vùng lợi trên rồi bật nhẹ."),
        Initial("t", "Âm đầu lưỡi", "tā", 1, "他", "anh ấy", "Giống vị trí d nhưng bật hơi rõ hơn."),
        Initial("n", "Âm đầu lưỡi", "nǐ", 3, "你", "bạn", "Đầu lưỡi chạm lợi trên, hơi thoát qua mũi."),
        Initial("l", "Âm đầu lưỡi", "lái", 2, "来", "đến", "Đầu lưỡi chạm lợi trên, hơi đi hai bên lưỡi."),
        Initial("g", "Âm cuống lưỡi", "gē", 1, "哥", "anh trai", "Phần sau lưỡi chạm ngạc mềm rồi mở ra."),
        Initial("k", "Âm cuống lưỡi", "kāi", 1, "开", "mở", "Cùng vị trí g, có luồng hơi bật ra rõ hơn."),
        Initial("h", "Âm cuống lưỡi", "hǎo", 3, "好", "tốt", "Tạo âm xát nhẹ ở phía sau khoang miệng."),
        Initial("j", "Âm mặt lưỡi", "jiā", 1, "家", "nhà", "Đầu lưỡi hướng về ngạc cứng; môi không tròn."),
        Initial("q", "Âm mặt lưỡi", "qī", 1, "七", "bảy", "Cùng vị trí j, bật hơi rõ."),
        Initial("x", "Âm mặt lưỡi", "xī", 1, "西", "phía tây", "Âm xát nhẹ, đầu lưỡi gần ngạc cứng."),
        Initial("zh", "Âm cong lưỡi", "zhōng", 1, "中", "ở giữa", "Đầu lưỡi hơi cong về phía sau."),
        Initial("ch", "Âm cong lưỡi", "chī", 1, "吃", "ăn", "Cùng vị trí zh nhưng bật hơi."),
        Initial("sh", "Âm cong lưỡi", "shū", 1, "书", "sách", "Đầu lưỡi hơi cong, tạo âm xát."),
        Initial("r", "Âm cong lưỡi", "rén", 2, "人", "người", "Đầu lưỡi hơi cong; âm có độ rung/xát nhẹ."),
        Initial("z", "Âm đầu lưỡi trước", "zì", 4, "字", "chữ", "Đầu lưỡi gần răng, bật nhẹ và không bật hơi."),
        Initial("c", "Âm đầu lưỡi trước", "cì", 4, "次", "lần", "Cùng vị trí z nhưng bật hơi."),
        Initial("s", "Âm đầu lưỡi trước", "sān", 1, "三", "ba", "Đầu lưỡi gần răng, tạo âm xát."),

        Final("a", "Vần đơn", "ā", 1, "啊", "thán từ"),
        Final("o", "Vần đơn", "ó", 2, "哦", "ồ / à"),
        Final("e", "Vần đơn", "è", 4, "饿", "đói"),
        Final("i", "Vần đơn", "yī", 1, "一", "một", "Khi đứng độc lập thường viết yi."),
        Final("u", "Vần đơn", "wū", 1, "屋", "nhà", "Khi đứng độc lập thường viết wu."),
        Final("ü", "Vần đơn", "yú", 2, "鱼", "cá", "Khi đứng độc lập viết yu; dấu hai chấm được lược."),
        Final("ai", "Vần kép", "ài", 4, "爱", "yêu"),
        Final("ei", "Vần kép", "lèi", 4, "累", "mệt"),
        Final("ao", "Vần kép", "hǎo", 3, "好", "tốt"),
        Final("ou", "Vần kép", "kǒu", 3, "口", "miệng"),
        Final("ia", "Vần kép", "jiā", 1, "家", "nhà"),
        Final("ie", "Vần kép", "xiě", 3, "写", "viết"),
        Final("iao", "Vần kép", "xiǎo", 3, "小", "nhỏ"),
        Final("iou (iu)", "Vần kép", "liù", 4, "六", "sáu", "Khi ghép với âm đầu thường viết gọn thành iu."),
        Final("ua", "Vần kép", "huā", 1, "花", "hoa"),
        Final("uo", "Vần kép", "guó", 2, "国", "nước"),
        Final("uai", "Vần kép", "kuài", 4, "快", "nhanh"),
        Final("uei (ui)", "Vần kép", "shuǐ", 3, "水", "nước", "Khi ghép với âm đầu thường viết gọn thành ui."),
        Final("üe", "Vần ü", "yuè", 4, "月", "trăng", "Sau j, q, x viết u; sau n, l giữ ü."),
        Final("üan", "Vần ü", "yuán", 2, "圆", "tròn", "Sau j, q, x viết u; sau n, l giữ ü."),
        Final("ün", "Vần ü", "yún", 2, "云", "mây", "Sau j, q, x viết u; sau n, l giữ ü."),
        Final("an", "Vần mũi", "ān", 1, "安", "yên"),
        Final("en", "Vần mũi", "mén", 2, "门", "cửa"),
        Final("ang", "Vần mũi", "máng", 2, "忙", "bận"),
        Final("eng", "Vần mũi", "lěng", 3, "冷", "lạnh"),
        Final("ong", "Vần mũi", "zhōng", 1, "中", "ở giữa"),
        Final("ian", "Vần mũi", "tiān", 1, "天", "trời"),
        Final("in", "Vần mũi", "xīn", 1, "心", "tim / lòng"),
        Final("iang", "Vần mũi", "xiǎng", 3, "想", "nghĩ"),
        Final("ing", "Vần mũi", "míng", 2, "明", "sáng"),
        Final("iong", "Vần mũi", "qióng", 2, "穷", "nghèo"),
        Final("uan", "Vần mũi", "guān", 1, "关", "đóng / cửa ải"),
        Final("uen (un)", "Vần mũi", "chūn", 1, "春", "mùa xuân", "Khi ghép với âm đầu thường viết gọn thành un."),
        Final("uang", "Vần mũi", "huáng", 2, "黄", "màu vàng"),
        Final("ueng", "Vần mũi", "wēng", 1, "翁", "ông lão", "Thường gặp trong âm tiết độc lập weng."),
    ]);

    private static readonly IReadOnlyList<FoundationItem> ToneItems = Array.AsReadOnly(
    [
        Tone("tone-1", "Thanh 1 · cao và ngang", "mā", 1, "妈", "mẹ", "Giữ giọng cao và đều, không lên hay xuống ở cuối."),
        Tone("tone-2", "Thanh 2 · đi lên", "má", 2, "麻", "cây gai", "Bắt đầu ở trung độ rồi đưa giọng lên."),
        Tone("tone-3", "Thanh 3 · xuống rồi lên", "mǎ", 3, "马", "ngựa", "Đường nét đi xuống rồi nhấc lên; khi nói liền thường nghe thấp và ngắn hơn."),
        Tone("tone-4", "Thanh 4 · đi xuống", "mà", 4, "骂", "mắng", "Hạ giọng nhanh, rõ và dứt khoát."),
        Tone("tone-neutral", "Thanh nhẹ · ngắn và nhẹ", "ma", 0, "吗", "trợ từ nghi vấn", "Đọc nhẹ, ngắn; cao độ phụ thuộc vào âm tiết đứng trước."),
        new FoundationItem(
            "third-tone-sandhi",
            "Quy tắc",
            "Hai thanh 3 đứng liền nhau",
            "Trong lời nói liền mạch, thanh 3 thứ nhất thường chuyển gần thành thanh 2. Chính tả vẫn ghi nguyên thanh: nǐ hǎo; cách đọc gần với ní hǎo.",
            "nǐ hǎo → ní hǎo",
            null,
            "你好",
            "xin chào"),
    ]);

    public static FoundationCatalog Pinyin { get; } = new(
        "Pinyin",
        "PlatformAuthored",
        "foundation-v1",
        "Danh mục âm đầu/vần và giải thích tiếng Việt do VietAIS biên soạn, đối chiếu chính tả với nguồn chuẩn bên dưới. Ví dụ chữ Hán chỉ minh họa âm đọc, không phải dữ liệu syllabus HSK/CTI.",
        Sources,
        PinyinItems);

    public static FoundationCatalog Tones { get; } = new(
        "Thanh điệu",
        "PlatformAuthored",
        "foundation-v1",
        "Mô tả và ví dụ do VietAIS biên soạn, đối chiếu ký hiệu thanh điệu với nguồn chuẩn bên dưới. Đây là nội dung nền tảng, không phải dữ liệu syllabus HSK/CTI.",
        Sources,
        ToneItems);

    private static FoundationItem Initial(
        string label,
        string category,
        string examplePinyin,
        int tone,
        string example,
        string exampleMeaning,
        string description) =>
        new($"initial-{label}", category, label, description, examplePinyin, tone, example, exampleMeaning);

    private static FoundationItem Final(
        string label,
        string category,
        string examplePinyin,
        int tone,
        string example,
        string exampleMeaning,
        string description = "Vần minh họa trong một âm tiết phổ biến.") =>
        new($"final-{label.Replace(' ', '-')}", category, label, description, examplePinyin, tone, example, exampleMeaning);

    private static FoundationItem Tone(
        string id,
        string label,
        string pinyin,
        int tone,
        string example,
        string exampleMeaning,
        string description) =>
        new(id, "Thanh điệu", label, description, pinyin, tone, example, exampleMeaning);
}
