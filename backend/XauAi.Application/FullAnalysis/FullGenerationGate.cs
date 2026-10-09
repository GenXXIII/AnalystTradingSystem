namespace XauAi.Application.FullAnalysis;

internal static class FullGenerationGate
{
    public static bool HasDeterministicCandidate(FullAnalysisSnapshot snapshot)
    {
        if (!string.Equals(snapshot.CurrentCandleState, "Closed", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var buy = snapshot.MarketFrames.Count(frame => IsAligned(frame, "Bullish"));
        var sell = snapshot.MarketFrames.Count(frame => IsAligned(frame, "Bearish"));
        return Math.Max(buy, sell) >= 1;
    }

    public static FullDecision? CandidateDirection(
        IEnumerable<FullWorkspaceRunResult> runs,
        decimal minimumConfidence,
        int minimumSupport)
    {
        var directional = runs
            .Where(run => run.Status is FullWorkspaceExecutionStatus.Completed or FullWorkspaceExecutionStatus.Cached)
            .Select(run => run.SpecialistOutput)
            .Where(output => output is
            {
                InsufficientEvidence: false,
                Direction: FullDecision.Buy or FullDecision.Sell
            } && output.Confidence >= minimumConfidence)
            .GroupBy(output => output!.Direction)
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

    private static bool IsAligned(FullMarketFrameSnapshot frame, string direction) =>
        string.Equals(frame.Trend, direction, StringComparison.OrdinalIgnoreCase)
        && frame.Structure.StartsWith(direction, StringComparison.OrdinalIgnoreCase);
}
