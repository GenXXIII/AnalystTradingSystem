using XauAi.Application.LocalAnalysis;

namespace XauAi.Application.TargetAnalysis;

internal static class TargetGenerationGate
{
    public static bool HasDeterministicCandidate(TargetAnalysisSnapshot snapshot)
    {
        var upward = snapshot.MarketFrames.Count(frame => IsAligned(frame, "Bullish"));
        var downward = snapshot.MarketFrames.Count(frame => IsAligned(frame, "Bearish"));
        if (Math.Max(upward, downward) < 2 || upward == downward)
        {
            return false;
        }

        var local = snapshot.LocalSignal;
        if (local?.State is not (LocalSignalState.Buy or LocalSignalState.Sell))
        {
            return true;
        }

        return upward > downward
            ? local.State == LocalSignalState.Buy
            : local.State == LocalSignalState.Sell;
    }

    public static TargetDirectionContext? CandidateDirection(
        IEnumerable<TargetWorkspaceRunResult> runs,
        decimal minimumConfidence,
        int minimumSupport)
    {
        var directional = runs
            .Where(run => run.Status == TargetWorkspaceExecutionStatus.Completed)
            .Select(run => run.SpecialistOutput)
            .Where(output => output is
            {
                HasCandidate: true,
                RiskAcceptable: true,
                DirectionContext: TargetDirectionContext.Upward or TargetDirectionContext.Downward
            } && output.Confidence >= minimumConfidence)
            .GroupBy(output => output!.DirectionContext)
            .Select(group => new { Direction = group.Key, Count = group.Count() })
            .OrderByDescending(group => group.Count)
            .ToArray();

        if (directional.Length == 0
            || directional[0].Count < minimumSupport
            || directional.Length > 1 && directional[1].Count >= directional[0].Count)
        {
            return null;
        }

        return directional[0].Direction;
    }

    private static bool IsAligned(TargetMarketFrameSnapshot frame, string direction) =>
        string.Equals(frame.Trend, direction, StringComparison.OrdinalIgnoreCase)
        && frame.Structure.StartsWith(direction, StringComparison.OrdinalIgnoreCase);
}
