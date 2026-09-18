using VietAisHsk.Api.Modules.Curriculum;

namespace VietAisHsk.Api.Tests;

public sealed class CurriculumValidationTests
{
    [Fact]
    public void ValidateImport_requires_provenance_and_level_data()
    {
        var result = CurriculumValidation.ValidateImport(new CurriculumImportRequest("", "", "", []));

        Assert.Equal("SyllabusVersionId là bắt buộc.", result);
    }

    [Fact]
    public void ValidateImport_rejects_duplicate_level_numbers()
    {
        var result = CurriculumValidation.ValidateImport(new CurriculumImportRequest(
            "source-v1",
            "HSK 3.0",
            "official-import",
            [
                new HskLevelImportItem("hsk3-a", 3, "HSK 3"),
                new HskLevelImportItem("hsk3-b", 3, "HSK 3 duplicate")
            ]));

        Assert.Equal("Trùng HSK level number: 3.", result);
    }
}
