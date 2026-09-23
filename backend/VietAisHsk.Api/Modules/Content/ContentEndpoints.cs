using VietAisHsk.Api.Infrastructure;

namespace VietAisHsk.Api.Modules.Content;

public static class ContentEndpoints
{
    public static IEndpointRouteBuilder MapContentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/content");

        group.MapGet("/questions", (string? type, string? hsk, string? skill, IQuestionBank questionBank) =>
        {
            var questions = questionBank.GetPublishedQuestions(type)
                .Where(question => Matches(question.HskLevel, hsk))
                .Where(question => Matches(question.Skill, skill))
                .Select(ToQuestionView);
            return Results.Ok(questions);
        });

        group.MapGet("/stories", (string? hsk, string? topic, IExtendedContentStore store) => Results.Ok(store.GetStories(hsk, topic)));
        group.MapGet("/stories/{id}", (string id, IExtendedContentStore store) => ToResult(store.GetStory(id)));
        group.MapGet("/videos", (string? hsk, string? topic, IExtendedContentStore store) => Results.Ok(store.GetVideos(hsk, topic)));
        group.MapGet("/videos/{id}", (string id, IExtendedContentStore store) => ToResult(store.GetVideo(id)));
        group.MapGet("/resources", (string? hsk, string? topic, IExtendedContentStore store) => Results.Ok(store.GetResources(hsk, topic)));
        group.MapGet("/resources/{id}", (string id, IExtendedContentStore store) => ToResult(store.GetResource(id)));
        group.MapGet("/tools", (IExtendedContentStore store) => Results.Ok(store.GetTools()));
        group.MapGet("/tools/{id}", (string id, IExtendedContentStore store) => ToResult(store.GetTool(id)));
        group.MapGet("/audio/{id}", (string id, IAudioAssetStore store) => ToResult(store.GetReady(id)));

        var admin = endpoints.MapGroup("/api/admin/content");
        admin.MapGet("/questions", (string? type, IUserContextAccessor contextAccessor, IQuestionBankAdmin questionBank) =>
            HasPermission(contextAccessor.Current, "content.manage")
                ? Results.Ok(questionBank.GetAllQuestions(type))
                : Forbidden());
        admin.MapPost("/questions/{id}/draft", (string id, SaveQuestionDraftRequest request, IUserContextAccessor contextAccessor, IQuestionBankAdmin questionBank) =>
        {
            if (!HasPermission(contextAccessor.Current, "content.manage")) return Forbidden();
            var validationError = ValidateDraft(id, request);
            if (validationError is not null)
            {
                return Results.Problem(validationError, statusCode: StatusCodes.Status400BadRequest, title: "Validation error");
            }

            if (questionBank.GetQuestion(id) is { Status: "Published" })
            {
                return Results.Problem(
                    "Câu hỏi đã Published; hãy tạo id/version mới thay vì sửa trực tiếp.",
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Published question is immutable");
            }

            var saved = questionBank.SaveDraft(id, request);
            return saved is null ? Results.NotFound() : Results.Ok(saved);
        });
        admin.MapPost("/questions/{id}/publish", (string id, IUserContextAccessor contextAccessor, IQuestionBankAdmin questionBank) =>
        {
            if (!HasPermission(contextAccessor.Current, "content.manage")) return Forbidden();
            var published = questionBank.Publish(id);
            return published is null ? Results.NotFound() : Results.Ok(published);
        });
        admin.MapGet("/{kind}", (string kind, IUserContextAccessor contextAccessor, IExtendedContentStore store) =>
            HasPermission(contextAccessor.Current, "content.manage")
                ? (store.GetAdminItems(kind) is { } items ? Results.Ok(items) : Results.NotFound())
                : Forbidden());
        admin.MapPost("/{kind}/{id}/publish", (string kind, string id, IUserContextAccessor contextAccessor, IExtendedContentStore store) =>
        {
            if (!HasPermission(contextAccessor.Current, "content.manage")) return Forbidden();
            var published = store.Publish(kind, id);
            return published is null ? Results.NotFound() : Results.Ok(published);
        });
        admin.MapGet("/audio", (IUserContextAccessor contextAccessor, IAudioAssetStore store) =>
            HasPermission(contextAccessor.Current, "content.manage")
                ? Results.Ok(store.GetAll())
                : Forbidden());
        admin.MapPost("/audio", (RequestAudioGenerationRequest request, IUserContextAccessor contextAccessor, IAudioAssetStore store) =>
        {
            if (!HasPermission(contextAccessor.Current, "content.manage")) return Forbidden();
            var validationError = ValidateAudioRequest(request);
            if (validationError is not null)
            {
                return Results.Problem(validationError, statusCode: StatusCodes.Status400BadRequest, title: "Validation error");
            }

            var outcome = store.Request(request);
            if (outcome.Conflict)
            {
                return Results.Problem(
                    "IdempotencyKey đã được dùng cho nội dung audio khác.",
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Audio request conflict");
            }

            return outcome.Created
                ? Results.Created($"/api/admin/content/audio/{outcome.Asset.Id}", outcome.Asset)
                : Results.Ok(outcome.Asset);
        });
        admin.MapPost("/audio/{id}/retry", (string id, IUserContextAccessor contextAccessor, IAudioAssetStore store) =>
        {
            if (!HasPermission(contextAccessor.Current, "content.manage")) return Forbidden();
            var outcome = store.Retry(id);
            if (outcome is null) return Results.NotFound();
            return outcome.Conflict
                ? Results.Problem(
                    "Chỉ audio ở trạng thái Failed mới có thể retry.",
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Audio is not retryable")
                : Results.Ok(outcome.Asset);
        });

        return endpoints;
    }

    private static IResult ToResult(object? value) => value is null ? Results.NotFound() : Results.Ok(value);

    private static ContentQuestionView ToQuestionView(ContentQuestion question) =>
        new(
            question.Id,
            question.Type,
            question.Prompt,
            question.Status,
            question.HskLevel,
            question.Skill,
            question.KnowledgeId,
            question.Explanation,
            question.Difficulty,
            question.ContentVersion,
            question.Options);

    private static string? ValidateDraft(string id, SaveQuestionDraftRequest request)
    {
        if (string.IsNullOrWhiteSpace(id)) return "Id câu hỏi là bắt buộc.";
        if (string.IsNullOrWhiteSpace(request.Type)) return "Type câu hỏi là bắt buộc.";
        if (string.IsNullOrWhiteSpace(request.Prompt)) return "Prompt câu hỏi là bắt buộc.";
        if (request.AcceptedAnswers is null || request.AcceptedAnswers.Count == 0 || request.AcceptedAnswers.All(string.IsNullOrWhiteSpace))
            return "Câu hỏi phải có ít nhất một đáp án hợp lệ.";
        if (request.Options is not null && request.Options.Any(string.IsNullOrWhiteSpace))
            return "Options không được chứa giá trị rỗng.";
        if (request.Difficulty is < 1 or > 5)
            return "Difficulty phải nằm trong khoảng từ 1 đến 5.";
        if (string.IsNullOrWhiteSpace(request.SourceType)) return "SourceType là bắt buộc để lưu provenance.";
        if (string.IsNullOrWhiteSpace(request.SourceVersion)) return "SourceVersion là bắt buộc để giữ version nguồn.";
        if (string.IsNullOrWhiteSpace(request.LicenseRef)) return "LicenseRef là bắt buộc để lưu license.";
        return null;
    }

    private static string? ValidateAudioRequest(RequestAudioGenerationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ContentId)) return "ContentId audio là bắt buộc.";
        if (string.IsNullOrWhiteSpace(request.Text)) return "Text audio là bắt buộc.";
        if (string.IsNullOrWhiteSpace(request.Voice)) return "Voice audio là bắt buộc.";
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey)) return "IdempotencyKey audio là bắt buộc.";
        if (request.Text.Trim().Length > 2000) return "Text audio không được dài quá 2000 ký tự.";
        return null;
    }

    private static bool Matches(string? value, string? filter) =>
        string.IsNullOrWhiteSpace(filter)
        || (!string.IsNullOrWhiteSpace(value) && string.Equals(value, filter.Trim(), StringComparison.OrdinalIgnoreCase));

    private static bool HasPermission(UserContext? context, string permission) =>
        context?.Permissions.Contains(permission) == true;

    private static IResult Forbidden() => Results.Problem(
        "Bạn không có permission cần thiết cho thao tác này.",
        statusCode: StatusCodes.Status403Forbidden,
        title: "Forbidden");
}
