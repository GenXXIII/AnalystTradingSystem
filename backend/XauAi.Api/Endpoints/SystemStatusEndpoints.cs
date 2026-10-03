using XauAi.Api.Models;
using XauAi.Application.SystemStatus;

namespace XauAi.Api.Endpoints;

public static class SystemStatusEndpoints
{
    public static IEndpointRouteBuilder MapSystemStatusEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/system/status", (
                IPlatformStatusService statusService,
                HttpContext context) =>
            Results.Ok(ApiSuccessResponse<PlatformStatus>.Create(
                statusService.GetCurrent(),
                context.TraceIdentifier)))
            .WithName("GetSystemStatus")
            .WithSummary("Returns the current API foundation status.")
            .Produces<ApiSuccessResponse<PlatformStatus>>(StatusCodes.Status200OK);

        return endpoints;
    }
}
