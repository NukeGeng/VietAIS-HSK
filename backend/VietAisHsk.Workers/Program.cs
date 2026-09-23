using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VietAisHsk.Workers;
using Wolverine;
using Wolverine.Persistence.Durability;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

var builder = Host.CreateApplicationBuilder(args);
var rabbitMqConnection = builder.Configuration.GetConnectionString("RabbitMq");
var postgresConnection = builder.Configuration.GetConnectionString("Postgres");
var callbackSecret = builder.Configuration["Workers:Callback:SharedSecret"];
var apiBaseUrl = builder.Configuration["Workers:ApiBaseUrl"] ?? "http://api:8080";

if (!Uri.TryCreate(rabbitMqConnection, UriKind.Absolute, out var rabbitMqUri)
    || !rabbitMqUri.Scheme.Equals("amqp", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException("Workers cần ConnectionStrings:RabbitMq dạng amqp://.");
}

if (string.IsNullOrWhiteSpace(postgresConnection))
{
    throw new InvalidOperationException("Workers cần ConnectionStrings:Postgres để bật durable inbox.");
}

if (string.IsNullOrWhiteSpace(callbackSecret))
{
    throw new InvalidOperationException("Workers cần Workers:Callback:SharedSecret; không khởi động worker callback khi thiếu secret.");
}

if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var parsedApiBaseUrl))
{
    throw new InvalidOperationException("Workers:ApiBaseUrl phải là absolute URL.");
}

builder.Services.AddSingleton(new HttpClient
{
    BaseAddress = parsedApiBaseUrl,
    Timeout = TimeSpan.FromSeconds(30),
    DefaultRequestHeaders = { Accept = { new MediaTypeWithQualityHeaderValue("application/json") } },
});
builder.Services.AddSingleton<ISubjectiveGradingProvider, UnconfiguredSubjectiveGradingProvider>();
builder.Services.AddSingleton<WorkerCallbackClient>();
builder.Services.AddSingleton<SubjectiveGradingHandler>();

builder.UseWolverine(options =>
{
    options.UseRabbitMq(rabbitMqUri).AutoProvision();
    options.PersistMessagesWithPostgresql(postgresConnection, "wolverine", MessageStoreRole.Main);
    options.ListenToRabbitQueue("vietais.exam.subjective-grading");
    options.Policies.UseDurableInboxOnAllListeners();
    options.Discovery.IncludeType<SubjectiveGradingHandler>();
});

await builder.Build().RunAsync();
