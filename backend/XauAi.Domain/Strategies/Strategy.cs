namespace XauAi.Domain.Strategies;

public sealed class Strategy
{
    public Guid Id { get; set; }

    public string Key { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class StrategyVersion
{
    public Guid Id { get; set; }

    public Guid StrategyId { get; set; }

    public string Version { get; set; } = string.Empty;

    public string DefinitionJson { get; set; } = "{}";

    public string DefinitionHash { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset EffectiveFromUtc { get; set; }

    public DateTimeOffset? RetiredAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class StrategyEvaluation
{
    public Guid Id { get; set; }

    public Guid StrategyVersionId { get; set; }

    public Guid InstrumentId { get; set; }

    public Guid? TimeframeId { get; set; }

    public DateTimeOffset EvaluatedAtUtc { get; set; }

    public string Result { get; set; } = string.Empty;

    public decimal? Score { get; set; }

    public string? DetailsJson { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class StrategyEvaluationEvidence
{
    public Guid StrategyEvaluationId { get; set; }

    public Guid EvidenceRecordId { get; set; }

    public string Role { get; set; } = string.Empty;
}
