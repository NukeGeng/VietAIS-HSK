using Wolverine;
using Microsoft.Extensions.DependencyInjection;
using VietAisHsk.Api.Modules.Content;
using VietAisHsk.Api.Modules.Exam;

namespace VietAisHsk.Api.Infrastructure;

/// <summary>
/// Production queue adapter for audio generation. The local implementation remains the
/// default when Wolverine/RabbitMQ is not explicitly enabled.
/// </summary>
public sealed class WolverineAudioGenerationQueue(IServiceScopeFactory scopeFactory) : IAudioGenerationQueue
{
    public IReadOnlyList<AudioGenerationRequested> Messages => Array.Empty<AudioGenerationRequested>();

    public bool Enqueue(AudioGenerationRequested message)
    {
        using var scope = scopeFactory.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMessageBus>().SendAsync(message).GetAwaiter().GetResult();
        return true;
    }
}

/// <summary>
/// Production queue adapter for subjective exam grading. Workers consume the same
/// message contract and call the authenticated grading-result boundary afterwards.
/// </summary>
public sealed class WolverineSubjectiveGradingQueue(IServiceScopeFactory scopeFactory) : IExamSubjectiveGradingQueue
{
    public IReadOnlyList<SubjectiveGradingRequestedMessage> Messages => Array.Empty<SubjectiveGradingRequestedMessage>();

    public bool Enqueue(SubjectiveGradingRequestedMessage message)
    {
        using var scope = scopeFactory.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMessageBus>().SendAsync(message).GetAwaiter().GetResult();
        return true;
    }
}
