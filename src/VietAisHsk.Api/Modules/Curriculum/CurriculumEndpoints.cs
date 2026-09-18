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

        group.MapGet("/curriculum/beginner", (ICurriculumStore store) =>
            Results.Ok(store.GetBeginnerTrack()));

        group.MapGet("/foundation/pinyin", (ICurriculumStore store) =>
            Results.Ok(store.GetPinyinCatalog()));

        group.MapGet("/foundation/tones", (ICurriculumStore store) =>
            Results.Ok(store.GetToneCatalog()));

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

            return Results.Ok(store.Import(request));
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

        return endpoints;
    }

    private static bool HasPermission(UserContext? context, string permission) =>
        context?.Permissions.Contains(permission) == true;

    private static IResult Forbidden() => Results.Problem(
        "Bạn không có permission cần thiết cho thao tác này.",
        statusCode: StatusCodes.Status403Forbidden,
        title: "Forbidden");
}
