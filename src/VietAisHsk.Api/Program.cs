using System.Text.Json.Serialization;
using VietAisHsk.Api.Infrastructure;
using VietAisHsk.Api.Modules.Curriculum;
using VietAisHsk.Api.Modules.Exam;
using VietAisHsk.Api.Modules.Identity;
using VietAisHsk.Api.Modules.Learning;
using VietAisHsk.Api.Modules.Practice;
using VietAisHsk.Api.Modules.Progress;
using VietAisHsk.Api.Modules.Review;
using VietAisHsk.Api.Modules.Speaking;
using VietAisHsk.Api.Modules.Translation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddHttpContextAccessor();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSingleton<IIdentityStore, InMemoryIdentityStore>();
builder.Services.AddSingleton<ICurriculumStore, InMemoryCurriculumStore>();
builder.Services.AddSingleton<IExamCatalog, BootstrapExamCatalog>();
builder.Services.AddSingleton<IExamStore, InMemoryExamStore>();
builder.Services.AddSingleton<ILearningStore, InMemoryLearningStore>();
builder.Services.AddSingleton<IPracticeQuestionReader, BootstrapPracticeQuestionReader>();
builder.Services.AddSingleton<IPracticeStore, InMemoryPracticeStore>();
builder.Services.AddSingleton<IProgressStore, InMemoryProgressStore>();
builder.Services.AddSingleton<IProgressSignalSink>(services => services.GetRequiredService<IProgressStore>());
builder.Services.AddSingleton<IReviewStore, InMemoryReviewStore>();
builder.Services.AddSingleton<IReviewSignalSink>(services => services.GetRequiredService<IReviewStore>());
builder.Services.AddSingleton<ITranslationCatalog, BootstrapTranslationCatalog>();
builder.Services.AddSingleton<ITranslationFeedbackGateway, UnconfiguredTranslationFeedbackGateway>();
builder.Services.AddSingleton<ITranslationStore, InMemoryTranslationStore>();
builder.Services.AddSingleton<ISpeakingProvider, UnconfiguredSpeakingProvider>();
builder.Services.AddSingleton<ISpeakingStore, InMemorySpeakingStore>();
builder.Services.AddScoped<IUserContextAccessor, HttpUserContextAccessor>();
builder.Services.AddVietAisMarten(builder.Configuration.GetConnectionString("Postgres"));

var app = builder.Build();

app.UseExceptionHandler();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapIdentityEndpoints();
app.MapCurriculumEndpoints();
app.MapExamEndpoints();
app.MapLearningEndpoints();
app.MapPracticeEndpoints();
app.MapProgressEndpoints();
app.MapReviewEndpoints();
app.MapTranslationEndpoints();
app.MapSpeakingEndpoints();

app.Run();

public partial class Program;
