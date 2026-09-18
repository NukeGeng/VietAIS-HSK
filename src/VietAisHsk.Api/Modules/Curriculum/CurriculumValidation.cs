namespace VietAisHsk.Api.Modules.Curriculum;

public static class CurriculumValidation
{
    public static string? ValidateImport(CurriculumImportRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SyllabusVersionId))
        {
            return "SyllabusVersionId là bắt buộc.";
        }

        if (string.IsNullOrWhiteSpace(request.SyllabusName))
        {
            return "SyllabusName là bắt buộc.";
        }

        if (string.IsNullOrWhiteSpace(request.SourceType))
        {
            return "SourceType là bắt buộc để lưu provenance.";
        }

        if (request.Levels is null || request.Levels.Count == 0)
        {
            return "Import phải có ít nhất một HSK level.";
        }

        var levelIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var levelNumbers = new HashSet<int>();
        foreach (var level in request.Levels)
        {
            if (string.IsNullOrWhiteSpace(level.Id))
            {
                return "Mỗi HSK level phải có Id ổn định.";
            }

            if (!levelIds.Add(level.Id.Trim()))
            {
                return $"Trùng HSK level Id: {level.Id}.";
            }

            if (level.LevelNumber is < 1 or > 9)
            {
                return $"HSK level phải nằm trong khoảng 1 đến 9: {level.LevelNumber}.";
            }

            if (!levelNumbers.Add(level.LevelNumber))
            {
                return $"Trùng HSK level number: {level.LevelNumber}.";
            }

            if (string.IsNullOrWhiteSpace(level.DisplayName))
            {
                return $"HSK level {level.LevelNumber} thiếu DisplayName.";
            }
        }

        return null;
    }
}
