using XauAi.Application.AI;

namespace XauAi.Application.TargetAnalysis;

internal sealed class TargetResultValidator(TargetAnalystSettings settings) : ITargetResultValidator
{
    public string? Validate(
        TargetMasterOutput master,
        TargetSpecialistOutput risk,
        TargetAnalysisSnapshot snapshot,
        IReadOnlyDictionary<Guid, AiEvidenceCandidate> selectedEvidence,
        decimal? requestedTimeframeAtr,
        IReadOnlyList<TargetWorkspaceRunResult> workspaceResults)
    {
        if (!master.ValidTarget || !master.TargetPrice.HasValue || !master.InvalidationPrice.HasValue)
        {
            return "TARGET_MASTER_REJECTED";
        }

        if (master.Confidence < settings.MinimumConfidence)
        {
            return "CONFIDENCE_BELOW_THRESHOLD";
        }

        if (risk.Workspace != TargetWorkspace.Risk || !risk.RiskAcceptable || !risk.HasCandidate)
        {
            return "RISK_REJECTED";
        }

        var completedCandidates = workspaceResults
            .Where(run => run.Status == TargetWorkspaceExecutionStatus.Completed
                && run.SpecialistOutput is { HasCandidate: true })
            .ToDictionary(run => run.Workspace, run => run.SpecialistOutput!);
        var support = master.SupportingWorkspaces.Distinct().ToArray();
        if (support.Length < 2)
        {
            return "INSUFFICIENT_INDEPENDENT_SUPPORT";
        }

        if (support.Any(workspace => !completedCandidates.ContainsKey(workspace)))
        {
            return "UNVERIFIED_SUPPORTING_WORKSPACE";
        }

        if (support.Any(workspace =>
                !completedCandidates[workspace].EvidenceIds.Intersect(master.EvidenceIds).Any()))
        {
            return "SUPPORT_WITHOUT_SHARED_EVIDENCE_TRACE";
        }

        var target = master.TargetPrice.Value;
        var invalidation = master.InvalidationPrice.Value;
        var current = snapshot.CurrentPrice;
        var minimumDistance = requestedTimeframeAtr is > 0m
            ? requestedTimeframeAtr.Value * settings.MinimumTargetDistanceAtr
            : current * 0.0005m;
        if (Math.Abs(target - current) < minimumDistance)
        {
            return "TARGET_NOT_MEANINGFULLY_DIFFERENT";
        }

        var upward = target > current;
        if (target == current
            || upward && invalidation >= current
            || !upward && invalidation <= current)
        {
            return "INVALID_INVALIDATION";
        }

        if (master.DirectionContext != (upward ? TargetDirectionContext.Upward : TargetDirectionContext.Downward))
        {
            return "DIRECTION_CONTEXT_MISMATCH";
        }

        if (!master.ValidUntilUtc.HasValue
            || master.ValidUntilUtc <= snapshot.AnalysisTimeUtc
            || master.ValidUntilUtc > snapshot.AnalysisTimeUtc.AddMinutes(settings.MaximumValidityMinutes))
        {
            return "INVALID_VALIDITY_WINDOW";
        }

        if (master.EvidenceIds.Any(id =>
                !selectedEvidence.TryGetValue(id, out var evidence)
                || evidence.AvailableAtUtc > snapshot.AnalysisTimeUtc
                || evidence.ValidFromUtc.HasValue && evidence.ValidFromUtc > snapshot.AnalysisTimeUtc
                || evidence.ValidToUtc.HasValue && evidence.ValidToUtc <= snapshot.AnalysisTimeUtc))
        {
            return "STALE_OR_LOOK_AHEAD_EVIDENCE";
        }

        var comparisonTolerance = Math.Max(current * 0.0005m, (requestedTimeframeAtr ?? 0m) * 0.10m);
        if (!risk.CandidateTargetPrice.HasValue
            || Math.Abs(risk.CandidateTargetPrice.Value - target) > comparisonTolerance)
        {
            return "RISK_DID_NOT_VALIDATE_MASTER_TARGET";
        }

        return null;
    }
}
