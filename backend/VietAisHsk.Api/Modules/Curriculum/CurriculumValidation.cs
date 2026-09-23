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
        var topicIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var unitIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lessonIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var level in request.Levels)
        {
            if (level is null)
            {
                return "HSK level không được để trống.";
            }

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

            foreach (var topic in level.Topics ?? Array.Empty<TopicImportItem>())
            {
                if (topic is null || string.IsNullOrWhiteSpace(topic.Id))
                {
                    return $"HSK level {level.LevelNumber} có Topic thiếu Id ổn định.";
                }

                if (!topicIds.Add(topic.Id.Trim()))
                {
                    return $"Trùng Topic Id: {topic.Id}.";
                }

                if (string.IsNullOrWhiteSpace(topic.Name))
                {
                    return $"Topic {topic.Id} thiếu Name.";
                }

                foreach (var unit in topic.Units ?? Array.Empty<UnitImportItem>())
                {
                    if (unit is null || string.IsNullOrWhiteSpace(unit.Id))
                    {
                        return $"Topic {topic.Id} có Unit thiếu Id ổn định.";
                    }

                    if (!unitIds.Add(unit.Id.Trim()))
                    {
                        return $"Trùng Unit Id: {unit.Id}.";
                    }

                    if (string.IsNullOrWhiteSpace(unit.Name))
                    {
                        return $"Unit {unit.Id} thiếu Name.";
                    }

                    foreach (var lesson in unit.Lessons ?? Array.Empty<LessonImportItem>())
                    {
                        if (lesson is null || string.IsNullOrWhiteSpace(lesson.Id))
                        {
                            return $"Unit {unit.Id} có Lesson thiếu Id ổn định.";
                        }

                        if (!lessonIds.Add(lesson.Id.Trim()))
                        {
                            return $"Trùng Lesson Id: {lesson.Id}.";
                        }

                        if (string.IsNullOrWhiteSpace(lesson.Name))
                        {
                            return $"Lesson {lesson.Id} thiếu Name.";
                        }
                    }
                }
            }
        }

        return null;
    }
}
