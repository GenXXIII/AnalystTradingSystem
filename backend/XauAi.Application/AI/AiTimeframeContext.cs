namespace XauAi.Application.AI;

internal static class AiTimeframeContext
{
    private static readonly string[] Ordered = ["M1", "M5", "M15", "M30", "H1", "H4", "D1"];

    public static bool IsRelevant(string? requestedTimeframe, string? evidenceTimeframe)
    {
        if (requestedTimeframe is null || evidenceTimeframe is null)
        {
            return true;
        }

        var requestedIndex = Array.IndexOf(Ordered, requestedTimeframe);
        var evidenceIndex = Array.IndexOf(Ordered, evidenceTimeframe);
        if (requestedIndex < 0 || evidenceIndex < 0)
        {
            return string.Equals(requestedTimeframe, evidenceTimeframe, StringComparison.Ordinal);
        }

        return evidenceIndex == requestedIndex
            || evidenceIndex == requestedIndex - 1
            || evidenceIndex > requestedIndex;
    }

    public static string Role(string? requestedTimeframe, string? evidenceTimeframe)
    {
        if (requestedTimeframe is null || evidenceTimeframe is null)
        {
            return "None";
        }

        if (string.Equals(requestedTimeframe, evidenceTimeframe, StringComparison.Ordinal))
        {
            return "Primary";
        }

        var requestedIndex = Array.IndexOf(Ordered, requestedTimeframe);
        var evidenceIndex = Array.IndexOf(Ordered, evidenceTimeframe);
        return requestedIndex > 0 && evidenceIndex == requestedIndex - 1
            ? "Confirmation"
            : evidenceIndex > requestedIndex && requestedIndex >= 0
                ? "Context"
                : "Excluded";
    }
}
