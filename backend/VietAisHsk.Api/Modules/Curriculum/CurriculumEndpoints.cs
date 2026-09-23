using VietAisHsk.Api.Infrastructure;

namespace VietAisHsk.Api.Modules.Curriculum;

public static class CurriculumEndpoints
{
    public static IEndpointRouteBuilder MapCurriculumEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api");

        group.MapGet("/curriculum/hsk-levels", (ICurriculumStore store) =>
            Results.Ok(store.GetPublishedLevels()));

        group.MapGet("/curriculum/hsk/{level}/tree", (string level, ICurriculumStore store) =>
        {
            var tree = store.GetPublishedTree(level);
            return tree is null ? Results.NotFound() : Results.Ok(tree);
        });

        group.MapGet("/curriculum/lessons/{id}", (string id, ICurriculumStore store) =>
        {
            var lesson = store.GetPublishedLessonDetail(id);
            return lesson is null ? Results.NotFound() : Results.Ok(lesson);
        });

        group.MapGet("/curriculum/beginner", (ICurriculumStore store) =>
            Results.Ok(store.GetBeginnerTrack()));

        group.MapGet("/foundation/pinyin", (ICurriculumStore store) =>
            Results.Ok(store.GetPinyinCatalog()));

        group.MapGet("/foundation/tones", (ICurriculumStore store) =>
            Results.Ok(store.GetToneCatalog()));

        group.MapGet("/admin/curriculum/hsk-levels", (IUserContextAccessor contextAccessor, ICurriculumStore store) =>
        {
            if (!HasPermission(contextAccessor.Current, "curriculum.manage"))
            {
                return Forbidden();
            }

            return Results.Ok(store.GetAdminLevels());
        });

        group.MapGet("/hanzi", (IHanziCatalog catalog) => Results.Ok(catalog.GetAll()));

        group.MapGet("/hanzi/{id}", (string id, IHanziCatalog catalog) =>
        {
            var character = catalog.Get(id);
            return character is null ? Results.NotFound() : Results.Ok(character);
        });

        group.MapGet("/hanzi/{id}/strokes", (string id, IHanziCatalog catalog) =>
        {
            var strokes = catalog.GetStrokes(id);
            return strokes is null ? Results.NotFound() : Results.Ok(strokes);
        });

        group.MapGet("/vocabulary", (string? search, string? hsk, string? topic, IVocabularyCatalog catalog) =>
            Results.Ok(catalog.SearchVocabulary(search, hsk, topic)));

        group.MapGet("/vocabulary/{id}", (string id, IVocabularyCatalog catalog) =>
        {
            var vocabulary = catalog.GetVocabulary(id);
            return vocabulary is null ? Results.NotFound() : Results.Ok(vocabulary);
        });

        group.MapGet("/grammar", (string? search, string? hsk, string? topic, IGrammarCatalog catalog) =>
            Results.Ok(catalog.SearchGrammar(search, hsk, topic)));

        group.MapGet("/grammar/{id}", (string id, IGrammarCatalog catalog) =>
        {
            var grammar = catalog.GetGrammar(id);
            return grammar is null ? Results.NotFound() : Results.Ok(grammar);
        });

        group.MapPost("/admin/curriculum/import", (CurriculumImportRequest request, IUserContextAccessor contextAccessor, ICurriculumStore store) =>
        {
            if (!HasPermission(contextAccessor.Current, "curriculum.manage"))
            {
                return Forbidden();
            }

            var validationError = CurriculumValidation.ValidateImport(request);
            if (validationError is not null)
            {
                return Results.Problem(validationError, statusCode: StatusCodes.Status400BadRequest, title: "Validation error");
            }

            try
            {
                return Results.Ok(store.Import(request));
            }
            catch (PublishedCurriculumVersionException exception)
            {
                return Results.Problem(
                    exception.Message,
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Published curriculum is immutable");
            }
            catch (CurriculumLevelIdConflictException exception)
            {
                return Results.Problem(
                    exception.Message,
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Curriculum level Id đã được dùng ở version khác");
            }
        });

        group.MapPost("/admin/curriculum/hsk-levels/{id}/publish", (string id, IUserContextAccessor contextAccessor, ICurriculumStore store) =>
        {
            if (!HasPermission(contextAccessor.Current, "curriculum.manage"))
            {
                return Forbidden();
            }

            var published = store.PublishLevel(id);
            return published is null ? Results.NotFound() : Results.Ok(published);
        });

        group.MapPost("/admin/curriculum/hsk-levels/{levelId}/lessons/{lessonId}/publish", (
            string levelId,
            string lessonId,
            IUserContextAccessor contextAccessor,
            ICurriculumStore store) =>
        {
            if (!HasPermission(contextAccessor.Current, "curriculum.manage"))
            {
                return Forbidden();
            }

            if (!store.GetPublishedLevels().Any(level =>
                    string.Equals(level.Id, levelId, StringComparison.OrdinalIgnoreCase)))
            {
                return Results.Conflict("Hãy publish HSK level trước khi publish lesson.");
            }

            var published = store.PublishLesson(levelId, lessonId);
            return published is null ? Results.NotFound() : Results.Ok(published);
        });

        return endpoints;
    }

    private static bool HasPermission(UserContext? context, string permission) =>
        context?.Permissions.Contains(permission) == true;

    private static IResult Forbidden() => Results.Problem(
        "Bạn không có permission cần thiết cho thao tác này.",
        statusCode: StatusCodes.Status403Forbidden,
        title: "Forbidden");
}
