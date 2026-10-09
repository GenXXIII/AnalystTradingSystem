using System.Text.Json;
using System.Text.Json.Serialization;
using XauAi.Application.AI;
using XauAi.Application.TargetAnalysis;

namespace XauAi.UnitTests.TargetAnalysis;

public sealed class TargetWorkspaceRunnerTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Workspace_runner_replaces_model_after_invalid_output_or_token_limit(
        bool primaryHitsTokenLimit)
    {
        var provider = new RecoveringStubProvider(
            JsonSerializer.Serialize(ValidKtrOutput(), Options),
            primaryHitsTokenLimit);
        var configuration = Configuration() with
        {
            Model = "primary-model",
            FallbackModels = ["fallback-model"]
        };
        var runner = new TargetWorkspaceRunner(
            new TargetWorkspaceCatalog([configuration]),
            new StubFactory(provider),
            new TargetAiResponseValidator(),
            new ScopedAiProviderRequestGate(TimeProvider.System),
            TimeProvider.System);
        var request = new TargetAiRequest(
            TargetWorkspace.Ktr,
            "XAUUSD",
            "M5",
            DateTimeOffset.UtcNow,
            configuration.PromptVersion,
            "{}",
            []);

        var result = await runner.RunSpecialistAsync(request);

        Assert.Equal(TargetWorkspaceExecutionStatus.Completed, result.Status);
        Assert.Equal("fallback-model", result.Configuration.Model);
        Assert.Equal(new[] { "primary-model", "fallback-model" }, provider.Models);
    }

    private static TargetSpecialistOutput ValidKtrOutput() => new()
    {
        Workspace = TargetWorkspace.Ktr,
        HasCandidate = false,
        CandidateTargetPrice = null,
        CandidateInvalidationPrice = null,
        DirectionContext = TargetDirectionContext.Unknown,
        Confidence = 0.25m,
        RiskAcceptable = false,
        Summary = "KTR evidence does not support a candidate.",
        Reasoning = [],
        Obstacles = ["The bounded snapshot is inconclusive."],
        Uncertainty = "A later closed candle may clarify the level reaction.",
        EvidenceIds = []
    };

    private static TargetWorkspaceConfiguration Configuration() => new(
        TargetWorkspace.Ktr,
        true,
        "Test",
        "Stub",
        false,
        string.Empty,
        "primary-model",
        ["fallback-model"],
        "https://example.test/v1",
        0.1,
        30,
        450,
        true,
        0,
        0,
        "phase13-test",
        "phase13-test");

    private sealed class StubFactory(ITargetAiProvider provider) : ITargetAiProviderFactory
    {
        public ITargetAiProvider Create(string adapter) => provider;
    }

    private sealed class RecoveringStubProvider(
        string fallbackJson,
        bool primaryHitsTokenLimit) : ITargetAiProvider
    {
        public string Adapter => "Stub";
        public List<string> Models { get; } = [];

        public Task<TargetAiCompletion> AnalyzeAsync(
            TargetAiRequest request,
            TargetWorkspaceConfiguration configuration,
            CancellationToken cancellationToken = default)
        {
            Models.Add(configuration.Model);
            if (Models.Count == 1 && primaryHitsTokenLimit)
            {
                throw new TargetAnalysisException(
                    TargetAnalysisErrorCodes.TokenLimit,
                    "The primary model reached its output token limit.");
            }

            var json = Models.Count == 1 ? "{}" : fallbackJson;
            return Task.FromResult(new TargetAiCompletion(json, 10, 20, 5, "test", configuration.Model));
        }
    }
}
