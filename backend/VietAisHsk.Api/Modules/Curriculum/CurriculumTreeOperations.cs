namespace VietAisHsk.Api.Modules.Curriculum;

internal static class CurriculumTreeOperations
{
    public static List<TopicNode> ToDraftTopics(HskLevelImportItem item) =>
        (item.Topics ?? Array.Empty<TopicImportItem>())
            .Select(topic => new TopicNode(
                topic.Id!.Trim(),
                topic.Name!.Trim(),
                (topic.Units ?? Array.Empty<UnitImportItem>())
                    .Select(unit => new UnitNode(
                        unit.Id!.Trim(),
                        unit.Name!.Trim(),
                        (unit.Lessons ?? Array.Empty<LessonImportItem>())
                            .Select(lesson => new LessonNode(lesson.Id!.Trim(), lesson.Name!.Trim(), ContentStatus.Draft))
                            .ToArray()))
                    .ToArray()))
            .ToList();

    public static IReadOnlyList<TopicNode> PublishedOnly(IReadOnlyList<TopicNode> topics) =>
        topics
            .Select(topic => topic with
            {
                Units = topic.Units
                    .Select(unit => unit with
                    {
                        Lessons = unit.Lessons.Where(lesson => lesson.Status == ContentStatus.Published).ToArray()
                    })
                    .Where(unit => unit.Lessons.Count > 0)
                    .ToArray()
            })
            .Where(topic => topic.Units.Count > 0)
            .ToArray();

    public static bool ContainsPublishedLesson(IReadOnlyList<TopicNode> topics, string lessonId) =>
        topics.Any(topic => topic.Units.Any(unit => unit.Lessons.Any(lesson =>
            lesson.Status == ContentStatus.Published
            && string.Equals(lesson.Id, lessonId, StringComparison.OrdinalIgnoreCase))));

    public static bool TryPublishLesson(
        IReadOnlyList<TopicNode> topics,
        string lessonId,
        out List<TopicNode> updatedTopics,
        out LessonNode? publishedLesson)
    {
        var found = false;
        publishedLesson = null;
        updatedTopics = [];

        foreach (var topic in topics)
        {
            var units = new List<UnitNode>(topic.Units.Count);
            foreach (var unit in topic.Units)
            {
                var lessons = new List<LessonNode>(unit.Lessons.Count);
                foreach (var lesson in unit.Lessons)
                {
                    if (!found && string.Equals(lesson.Id, lessonId, StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        publishedLesson = lesson with { Status = ContentStatus.Published };
                        lessons.Add(publishedLesson);
                    }
                    else
                    {
                        lessons.Add(lesson);
                    }
                }

                units.Add(unit with { Lessons = lessons });
            }

            updatedTopics.Add(topic with { Units = units });
        }

        return found;
    }

    public static bool MatchesImport(HskLevelImportItem item, HskLevel existing, IReadOnlyList<TopicNode> existingTopics, string versionId)
    {
        if (!string.Equals(existing.Id, item.Id?.Trim(), StringComparison.OrdinalIgnoreCase)
            || !string.Equals(existing.SyllabusVersionId, versionId, StringComparison.OrdinalIgnoreCase)
            || existing.LevelNumber != item.LevelNumber
            || !string.Equals(existing.DisplayName, item.DisplayName?.Trim(), StringComparison.Ordinal))
        {
            return false;
        }

        var importedTopics = ToDraftTopics(item);
        return SameTopics(importedTopics, existingTopics);
    }

    private static bool SameTopics(IReadOnlyList<TopicNode> left, IReadOnlyList<TopicNode> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (var topicIndex = 0; topicIndex < left.Count; topicIndex++)
        {
            var leftTopic = left[topicIndex];
            var rightTopic = right[topicIndex];
            if (!string.Equals(leftTopic.Id, rightTopic.Id, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(leftTopic.Name, rightTopic.Name, StringComparison.Ordinal)
                || leftTopic.Units.Count != rightTopic.Units.Count)
            {
                return false;
            }

            for (var unitIndex = 0; unitIndex < leftTopic.Units.Count; unitIndex++)
            {
                var leftUnit = leftTopic.Units[unitIndex];
                var rightUnit = rightTopic.Units[unitIndex];
                if (!string.Equals(leftUnit.Id, rightUnit.Id, StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(leftUnit.Name, rightUnit.Name, StringComparison.Ordinal)
                    || leftUnit.Lessons.Count != rightUnit.Lessons.Count)
                {
                    return false;
                }

                for (var lessonIndex = 0; lessonIndex < leftUnit.Lessons.Count; lessonIndex++)
                {
                    var leftLesson = leftUnit.Lessons[lessonIndex];
                    var rightLesson = rightUnit.Lessons[lessonIndex];
                    if (!string.Equals(leftLesson.Id, rightLesson.Id, StringComparison.OrdinalIgnoreCase)
                        || !string.Equals(leftLesson.Name, rightLesson.Name, StringComparison.Ordinal))
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }
}
