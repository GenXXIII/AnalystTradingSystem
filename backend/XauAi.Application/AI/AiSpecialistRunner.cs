using System.Collections.Concurrent;

namespace XauAi.Application.AI;

internal sealed class ConfiguredAiSpecialist(
    AiSpecialist name,
    AiSpecialistCatalog catalog,
    IAiProviderFactory providerFactory,
    IAiRequestGate requestGate) : IAiSpecialist
{
    public AiSpecialist Name { get; } = name;

    public AiSpecialistConfiguration Configuration => catalog.Get(Name);

    public async Task<AiProviderCompletion> AnalyzeAsync(
        AiProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        var configuration = Configuration;
        if (!configuration.Enabled)
        {
            throw new AiInterpretationException(
                AiInterpretationErrorCodes.SpecialistDisabled,
                $"The {Name} AI specialist is disabled.");
        }

        await requestGate.WaitAsync(Name, configuration.RequestsPerMinute, cancellationToken);
        var provider = providerFactory.Create(configuration.Adapter);
        return await provider.InterpretAsync(request, configuration, cancellationToken);
    }
}

internal sealed class AiSpecialistRunner(IEnumerable<IAiSpecialist> specialists) : IAiSpecialistRunner
{
    private readonly IReadOnlyDictionary<AiSpecialist, IAiSpecialist> _specialists =
        specialists.ToDictionary(specialist => specialist.Name);

    public async Task<(AiProviderCompletion Completion, AiSpecialistConfiguration Configuration)> RunAsync(
        AiProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_specialists.TryGetValue(request.Specialist, out var specialist))
        {
            throw new AiInterpretationException(
                AiInterpretationErrorCodes.SpecialistNotConfigured,
                $"The {request.Specialist} AI specialist is not registered.");
        }

        var completion = await specialist.AnalyzeAsync(request, cancellationToken);
        return (completion, specialist.Configuration);
    }
}

internal sealed class AiRequestGate(TimeProvider timeProvider) : IAiRequestGate
{
    private readonly ConcurrentDictionary<AiSpecialist, GateState> _states = new();

    public async Task WaitAsync(
        AiSpecialist specialist,
        int requestsPerMinute,
        CancellationToken cancellationToken = default)
    {
        if (requestsPerMinute <= 0)
        {
            return;
        }

        var state = _states.GetOrAdd(specialist, static _ => new GateState());
        await state.Lock.WaitAsync(cancellationToken);
        try
        {
            var interval = TimeSpan.FromMinutes(1d / requestsPerMinute);
            var now = timeProvider.GetUtcNow();
            var wait = state.LastRequestUtc + interval - now;
            if (wait > TimeSpan.Zero)
            {
                await Task.Delay(wait, timeProvider, cancellationToken);
            }

            state.LastRequestUtc = timeProvider.GetUtcNow();
        }
        finally
        {
            state.Lock.Release();
        }
    }

    private sealed class GateState
    {
        public SemaphoreSlim Lock { get; } = new(1, 1);

        public DateTimeOffset LastRequestUtc { get; set; } = DateTimeOffset.MinValue;
    }
}

internal sealed class AiInterpretationExecutionGate : IAiInterpretationExecutionGate
{
    private readonly ConcurrentDictionary<string, GateEntry> _entries = new(StringComparer.Ordinal);

    public async Task<IAsyncDisposable> AcquireAsync(
        string cacheKey,
        CancellationToken cancellationToken = default)
    {
        GateEntry entry;
        while (true)
        {
            entry = _entries.GetOrAdd(cacheKey, static _ => new GateEntry());
            Interlocked.Increment(ref entry.ReferenceCount);
            if (_entries.TryGetValue(cacheKey, out var current) && ReferenceEquals(entry, current))
            {
                break;
            }

            Interlocked.Decrement(ref entry.ReferenceCount);
        }

        try
        {
            await entry.Semaphore.WaitAsync(cancellationToken);
            return new Lease(this, cacheKey, entry);
        }
        catch
        {
            ReleaseReference(cacheKey, entry, releaseSemaphore: false);
            throw;
        }
    }

    private void ReleaseReference(string cacheKey, GateEntry entry, bool releaseSemaphore)
    {
        if (releaseSemaphore)
        {
            entry.Semaphore.Release();
        }

        if (Interlocked.Decrement(ref entry.ReferenceCount) == 0)
        {
            _entries.TryRemove(new KeyValuePair<string, GateEntry>(cacheKey, entry));
        }
    }

    private sealed class GateEntry
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);

        public int ReferenceCount;
    }

    private sealed class Lease(
        AiInterpretationExecutionGate owner,
        string cacheKey,
        GateEntry entry) : IAsyncDisposable
    {
        private int _released;

        public ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
            {
                owner.ReleaseReference(cacheKey, entry, releaseSemaphore: true);
            }

            return ValueTask.CompletedTask;
        }
    }
}
