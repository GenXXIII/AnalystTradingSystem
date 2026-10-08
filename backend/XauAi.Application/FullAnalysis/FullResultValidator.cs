namespace XauAi.Application.FullAnalysis;

internal sealed class FullResultValidator(FullAnalystSettings settings) : IFullResultValidator
{
    public string? Validate(
        FullMasterOutput master,
        FullSpecialistOutput risk,
        FullAnalysisSnapshot snapshot,
        IReadOnlyList<FullWorkspaceRunResult> workspaceResults)
    {
        if (master.Decision == FullDecision.Wait)
        {
            return null;
        }

        if (master.Confidence < settings.MinimumConfidence)
        {
            return "CONFIDENCE_BELOW_THRESHOLD";
        }

        if (risk.Workspace != FullWorkspace.Risk || risk.InsufficientEvidence)
        {
            return "RISK_REJECTED";
        }

        var completed = workspaceResults
            .Where(run => run.Status is FullWorkspaceExecutionStatus.Completed or FullWorkspaceExecutionStatus.Cached
                && run.SpecialistOutput is not null)
            .ToDictionary(run => run.Workspace, run => run.SpecialistOutput!);
        var support = master.SupportingWorkspaces.Distinct().ToArray();
        if (support.Length < 2 || support.Any(workspace => !completed.ContainsKey(workspace)))
        {
            return "INSUFFICIENT_INDEPENDENT_SUPPORT";
        }

        if (support.Any(workspace => completed[workspace].Direction != master.Decision
                || !completed[workspace].EvidenceIds.Intersect(master.KeyEvidenceIds).Any()))
        {
            return "UNVERIFIED_DIRECTIONAL_SUPPORT";
        }

        if (!master.ValidUntilUtc.HasValue
            || master.ValidUntilUtc <= snapshot.AnalysisTimeUtc
            || master.ValidUntilUtc > snapshot.AnalysisTimeUtc.AddMinutes(settings.MaximumValidityMinutes))
        {
            return "INVALID_VALIDITY_WINDOW";
        }

        if (master.Invalidation.Price is not > 0m)
        {
            return "INVALID_INVALIDATION";
        }

        if (master.Decision == FullDecision.Buy
            && (master.Invalidation.Condition != FullInvalidationCondition.AtOrBelow
                || master.Invalidation.Price >= snapshot.CurrentPrice))
        {
            return "INVALID_BUY_INVALIDATION";
        }

        if (master.Decision == FullDecision.Sell
            && (master.Invalidation.Condition != FullInvalidationCondition.AtOrAbove
                || master.Invalidation.Price <= snapshot.CurrentPrice))
        {
            return "INVALID_SELL_INVALIDATION";
        }

        return null;
    }
}
