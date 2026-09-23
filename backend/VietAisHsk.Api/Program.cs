using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using VietAisHsk.Api.Infrastructure;
using VietAisHsk.Api.Modules.Curriculum;
using VietAisHsk.Api.Modules.Content;
using VietAisHsk.Api.Modules.Exam;
using VietAisHsk.Api.Modules.Identity;
using VietAisHsk.Api.Modules.Learning;
using VietAisHsk.Api.Modules.Practice;
using VietAisHsk.Api.Modules.Progress;
using VietAisHsk.Api.Modules.Review;
using VietAisHsk.Api.Modules.Speaking;
using VietAisHsk.Api.Modules.Translation;
using Wolverine;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

var postgresConnection = builder.Configuration.GetConnectionString("Postgres");
var rabbitMqConnection = builder.Configuration.GetConnectionString("RabbitMq");
var rabbitMqUri = Uri.TryCreate(rabbitMqConnection, UriKind.Absolute, out var parsedRabbitMqUri)
    ? parsedRabbitMqUri
    : null;
var jwtEnabled = builder.Configuration.GetValue<bool>("Authentication:Jwt:Enabled");
var jwtAuthority = builder.Configuration["Authentication:Jwt:Authority"];
var jwtAudience = builder.Configuration["Authentication:Jwt:Audience"];
var jwtRequireHttpsMetadata = builder.Configuration.GetValue("Authentication:Jwt:RequireHttpsMetadata", true);
if (jwtEnabled && (string.IsNullOrWhiteSpace(jwtAuthority) || string.IsNullOrWhiteSpace(jwtAudience)))
{
    throw new InvalidOperationException(
        "Authentication:Jwt:Authority và Authentication:Jwt:Audience phải được cấu hình khi JWT authentication được bật.");
}

var enableWolverineMessaging = builder.Configuration.GetValue<bool>("Messaging:EnableWolverine")
    && !string.IsNullOrWhiteSpace(postgresConnection)
    && rabbitMqUri?.Scheme.Equals("amqp", StringComparison.OrdinalIgnoreCase) == true;

if (enableWolverineMessaging)
{
    builder.Host.UseWolverine(options =>
    {
        options.UseRabbitMq(rabbitMqUri!).AutoProvision();
        options.PublishMessage<AudioGenerationRequested>().ToRabbitQueue("vietais.audio.generate");
        options.PublishMessage<SubjectiveGradingRequestedMessage>().ToRabbitQueue("vietais.exam.subjective-grading");
        options.Policies.UseDurableInboxOnAllListeners();
        options.Policies.UseDurableOutboxOnAllSendingEndpoints();
    });
}

builder.Services.AddProblemDetails();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IWorkerCallbackAuthenticator>(_ =>
    new HmacWorkerCallbackAuthenticator(
        builder.Configuration["Workers:Callback:SharedSecret"],
        builder.Configuration.GetValue("Workers:Callback:MaxClockSkewSeconds", 300)));
if (jwtEnabled)
{
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = jwtAuthority;
            options.Audience = jwtAudience;
            options.RequireHttpsMetadata = jwtRequireHttpsMetadata;
            options.MapInboundClaims = true;
        });
}
else
{
    // Local development deliberately keeps the header adapter available without silently
    // pretending that an external provider has been selected.
    builder.Services.AddAuthentication();
}
builder.Services.AddAuthorization();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSingleton<IIdentityStore, InMemoryIdentityStore>();
builder.Services.AddSingleton<ICurriculumStore, InMemoryCurriculumStore>();
builder.Services.AddSingleton<IHanziCatalog, BootstrapHanziCatalog>();
builder.Services.AddSingleton<BootstrapKnowledgeCatalog>();
builder.Services.AddSingleton<IVocabularyCatalog>(services => services.GetRequiredService<BootstrapKnowledgeCatalog>());
builder.Services.AddSingleton<IGrammarCatalog>(services => services.GetRequiredService<BootstrapKnowledgeCatalog>());
builder.Services.AddSingleton<BootstrapQuestionBank>();
builder.Services.AddSingleton<IQuestionBank>(services => services.GetRequiredService<BootstrapQuestionBank>());
builder.Services.AddSingleton<IQuestionBankAdmin>(services => services.GetRequiredService<BootstrapQuestionBank>());
builder.Services.AddSingleton<IExtendedContentStore, InMemoryExtendedContentStore>();
if (enableWolverineMessaging)
{
    builder.Services.AddSingleton<IAudioGenerationQueue, WolverineAudioGenerationQueue>();
    builder.Services.AddSingleton<IExamSubjectiveGradingQueue, WolverineSubjectiveGradingQueue>();
}
else
{
    builder.Services.AddSingleton<IAudioGenerationQueue, InMemoryAudioGenerationQueue>();
    builder.Services.AddSingleton<IExamSubjectiveGradingQueue, InMemoryExamSubjectiveGradingQueue>();
}
builder.Services.AddSingleton<IAudioAssetStore, InMemoryAudioAssetStore>();
builder.Services.AddSingleton<IExamCatalog, BootstrapExamCatalog>();
builder.Services.AddSingleton<IExamStore>(services =>
    new InMemoryExamStore(services.GetRequiredService<IExamSubjectiveGradingQueue>()));
builder.Services.AddSingleton<ILearningStore, InMemoryLearningStore>();
builder.Services.AddScoped<IPracticeQuestionReader, ContentPracticeQuestionReader>();
builder.Services.AddSingleton<IPracticeStore, InMemoryPracticeStore>();
builder.Services.AddSingleton<IHanziWritingStore>(services => new InMemoryHanziWritingStore(services.GetRequiredService<IHanziCatalog>()));
builder.Services.AddSingleton<IProgressStore, InMemoryProgressStore>();
builder.Services.AddSingleton<IProgressSignalSink>(services => services.GetRequiredService<IProgressStore>());
builder.Services.AddSingleton<IReviewStore, InMemoryReviewStore>();
builder.Services.AddSingleton<IReviewSignalSink>(services => services.GetRequiredService<IReviewStore>());
builder.Services.AddSingleton<IReviewClock, SystemReviewClock>();
builder.Services.AddSingleton<ITranslationCatalog, BootstrapTranslationCatalog>();
builder.Services.AddSingleton<ITranslationFeedbackGateway, UnconfiguredTranslationFeedbackGateway>();
builder.Services.AddSingleton<ITranslationStore, InMemoryTranslationStore>();
builder.Services.AddSingleton<ISpeakingProvider, UnconfiguredSpeakingProvider>();
builder.Services.AddSingleton<ISpeakingStore, InMemorySpeakingStore>();
builder.Services.AddScoped<IUserContextAccessor, HttpUserContextAccessor>();
builder.Services.AddVietAisMarten(postgresConnection, enableWolverineMessaging);

var app = builder.Build();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
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
app.MapContentEndpoints();

app.Run();

public partial class Program;
