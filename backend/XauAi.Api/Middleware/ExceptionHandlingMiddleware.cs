using XauAi.Api.Models;
using XauAi.Application.MarketData;
using XauAi.Application.News;
using XauAi.Application.EconomicData;
using XauAi.Application.Analysts;
using XauAi.Application.Evidence;
using XauAi.Application.TechnicalAnalysis;
using XauAi.Application.AI;
using XauAi.Application.LocalAnalysis;
using XauAi.Application.TargetAnalysis;
using XauAi.Application.FullAnalysis;

namespace XauAi.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (MarketDataException exception) when (!context.Response.HasStarted)
        {
            logger.LogWarning(
                exception,
                "Market data request failed with code {ErrorCode} for {Method} {Path}",
                exception.Code,
                context.Request.Method,
                context.Request.Path);

            context.Response.Clear();
            context.Response.StatusCode = StatusCode(exception.Code);
            await context.Response.WriteAsJsonAsync(ApiErrorResponse.Create(
                exception.Code,
                exception.SafeMessage,
                context.TraceIdentifier));
        }
        catch (TechnicalAnalysisException exception) when (!context.Response.HasStarted)
        {
            logger.LogWarning(
                exception,
                "Technical analysis request failed with code {ErrorCode} for {Method} {Path}",
                exception.Code,
                context.Request.Method,
                context.Request.Path);

            context.Response.Clear();
            context.Response.StatusCode = AnalysisStatusCode(exception.Code);
            await context.Response.WriteAsJsonAsync(ApiErrorResponse.Create(
                exception.Code,
                exception.SafeMessage,
                context.TraceIdentifier));
        }
        catch (NewsException exception) when (!context.Response.HasStarted)
        {
            logger.LogWarning(
                exception,
                "News request failed with code {ErrorCode} for {Method} {Path}",
                exception.Code,
                context.Request.Method,
                context.Request.Path);

            context.Response.Clear();
            context.Response.StatusCode = NewsStatusCode(exception.Code);
            await context.Response.WriteAsJsonAsync(ApiErrorResponse.Create(
                exception.Code,
                exception.SafeMessage,
                context.TraceIdentifier));
        }
        catch (EconomicDataException exception) when (!context.Response.HasStarted)
        {
            logger.LogWarning(
                exception,
                "Economic-data request failed with code {ErrorCode} for {Method} {Path}",
                exception.Code,
                context.Request.Method,
                context.Request.Path);

            context.Response.Clear();
            context.Response.StatusCode = EconomicDataStatusCode(exception.Code);
            await context.Response.WriteAsJsonAsync(ApiErrorResponse.Create(
                exception.Code,
                exception.SafeMessage,
                context.TraceIdentifier));
        }
        catch (AnalystException exception) when (!context.Response.HasStarted)
        {
            logger.LogWarning(
                exception,
                "Analyst-data request failed with code {ErrorCode} for {Method} {Path}",
                exception.Code,
                context.Request.Method,
                context.Request.Path);

            context.Response.Clear();
            context.Response.StatusCode = AnalystStatusCode(exception.Code);
            await context.Response.WriteAsJsonAsync(ApiErrorResponse.Create(
                exception.Code,
                exception.SafeMessage,
                context.TraceIdentifier));
        }
        catch (EvidenceException exception) when (!context.Response.HasStarted)
        {
            logger.LogWarning(
                exception,
                "Evidence request failed with code {ErrorCode} for {Method} {Path}",
                exception.Code,
                context.Request.Method,
                context.Request.Path);

            context.Response.Clear();
            context.Response.StatusCode = EvidenceStatusCode(exception.Code);
            await context.Response.WriteAsJsonAsync(ApiErrorResponse.Create(
                exception.Code,
                exception.SafeMessage,
                context.TraceIdentifier));
        }
        catch (AiInterpretationException exception) when (!context.Response.HasStarted)
        {
            logger.LogWarning(
                exception,
                "AI interpretation request failed with code {ErrorCode} for {Method} {Path}",
                exception.Code,
                context.Request.Method,
                context.Request.Path);

            context.Response.Clear();
            context.Response.StatusCode = AiInterpretationStatusCode(exception.Code);
            await context.Response.WriteAsJsonAsync(ApiErrorResponse.Create(
                exception.Code,
                exception.SafeMessage,
                context.TraceIdentifier));
        }
        catch (LocalAnalystException exception) when (!context.Response.HasStarted)
        {
            logger.LogWarning(
                exception,
                "Local analyst request failed with code {ErrorCode} for {Method} {Path}",
                exception.Code,
                context.Request.Method,
                context.Request.Path);

            context.Response.Clear();
            context.Response.StatusCode = LocalAnalystStatusCode(exception.Code);
            await context.Response.WriteAsJsonAsync(ApiErrorResponse.Create(
                exception.Code,
                exception.SafeMessage,
                context.TraceIdentifier));
        }
        catch (TargetAnalysisException exception) when (!context.Response.HasStarted)
        {
            logger.LogWarning(
                exception,
                "Target analysis request failed with code {ErrorCode} for {Method} {Path}",
                exception.Code,
                context.Request.Method,
                context.Request.Path);

            context.Response.Clear();
            context.Response.StatusCode = TargetAnalysisStatusCode(exception.Code);
            await context.Response.WriteAsJsonAsync(ApiErrorResponse.Create(
                exception.Code,
                exception.SafeMessage,
                context.TraceIdentifier));
        }
        catch (FullAnalysisException exception) when (!context.Response.HasStarted)
        {
            logger.LogWarning(
                exception,
                "Full analysis request failed with code {ErrorCode} for {Method} {Path}",
                exception.Code,
                context.Request.Method,
                context.Request.Path);

            context.Response.Clear();
            context.Response.StatusCode = FullAnalysisStatusCode(exception.Code);
            await context.Response.WriteAsJsonAsync(ApiErrorResponse.Create(
                exception.Code,
                exception.SafeMessage,
                context.TraceIdentifier));
        }
        catch (BadHttpRequestException exception) when (!context.Response.HasStarted)
        {
            logger.LogWarning(
                exception,
                "Invalid HTTP request for {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(ApiErrorResponse.Create(
                MarketDataErrorCodes.InvalidRequest,
                "The request is missing a required value or contains an invalid value.",
                context.TraceIdentifier));
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            logger.LogError(exception, "Unhandled API failure for {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(ApiErrorResponse.Create(
                "INTERNAL_SERVER_ERROR",
                "An unexpected error occurred.",
                context.TraceIdentifier));
        }
    }

    private static int StatusCode(string code) => code switch
    {
        MarketDataErrorCodes.InvalidRequest
            or MarketDataErrorCodes.QueryLimitExceeded
            or MarketDataErrorCodes.QueryRangeTooLarge => StatusCodes.Status400BadRequest,
        MarketDataErrorCodes.ProviderSymbolNotFound => StatusCodes.Status404NotFound,
        MarketDataErrorCodes.ProviderRateLimited => StatusCodes.Status429TooManyRequests,
        MarketDataErrorCodes.ProviderTimeout => StatusCodes.Status504GatewayTimeout,
        MarketDataErrorCodes.ProviderDisabled
            or MarketDataErrorCodes.ProviderConfigurationInvalid
            or MarketDataErrorCodes.ProviderConnectionFailed
            or MarketDataErrorCodes.ProviderAuthenticationFailed
            or MarketDataErrorCodes.ProviderDataRequestFailed
            or MarketDataErrorCodes.ProviderUnavailable
            or MarketDataErrorCodes.DatabaseDisabled => StatusCodes.Status503ServiceUnavailable,
        _ => StatusCodes.Status500InternalServerError
    };

    private static int AnalysisStatusCode(string code) => code switch
    {
        TechnicalAnalysisErrorCodes.InvalidRequest => StatusCodes.Status400BadRequest,
        TechnicalAnalysisErrorCodes.NoData => StatusCodes.Status404NotFound,
        TechnicalAnalysisErrorCodes.Disabled => StatusCodes.Status503ServiceUnavailable,
        _ => StatusCodes.Status500InternalServerError
    };

    private static int NewsStatusCode(string code) => code switch
    {
        NewsErrorCodes.InvalidRequest => StatusCodes.Status400BadRequest,
        NewsErrorCodes.NotFound => StatusCodes.Status404NotFound,
        NewsErrorCodes.RateLimited => StatusCodes.Status429TooManyRequests,
        NewsErrorCodes.Timeout => StatusCodes.Status504GatewayTimeout,
        NewsErrorCodes.InvalidResponse => StatusCodes.Status502BadGateway,
        NewsErrorCodes.Disabled
            or NewsErrorCodes.DatabaseDisabled
            or NewsErrorCodes.AuthenticationFailed
            or NewsErrorCodes.ProviderUnavailable => StatusCodes.Status503ServiceUnavailable,
        _ => StatusCodes.Status500InternalServerError
    };

    private static int EconomicDataStatusCode(string code) => code switch
    {
        EconomicDataErrorCodes.InvalidRequest => StatusCodes.Status400BadRequest,
        EconomicDataErrorCodes.SeriesNotFound => StatusCodes.Status404NotFound,
        EconomicDataErrorCodes.RateLimited => StatusCodes.Status429TooManyRequests,
        EconomicDataErrorCodes.Timeout => StatusCodes.Status504GatewayTimeout,
        EconomicDataErrorCodes.InvalidResponse => StatusCodes.Status502BadGateway,
        EconomicDataErrorCodes.Disabled
            or EconomicDataErrorCodes.DatabaseDisabled
            or EconomicDataErrorCodes.AuthenticationFailed
            or EconomicDataErrorCodes.ProviderUnavailable => StatusCodes.Status503ServiceUnavailable,
        _ => StatusCodes.Status500InternalServerError
    };

    private static int AnalystStatusCode(string code) => code switch
    {
        AnalystErrorCodes.InvalidRequest => StatusCodes.Status400BadRequest,
        AnalystErrorCodes.NotFound => StatusCodes.Status404NotFound,
        AnalystErrorCodes.RateLimited => StatusCodes.Status429TooManyRequests,
        AnalystErrorCodes.Timeout => StatusCodes.Status504GatewayTimeout,
        AnalystErrorCodes.InvalidResponse => StatusCodes.Status502BadGateway,
        AnalystErrorCodes.Disabled
            or AnalystErrorCodes.DatabaseDisabled
            or AnalystErrorCodes.AuthenticationFailed
            or AnalystErrorCodes.ProviderUnavailable => StatusCodes.Status503ServiceUnavailable,
        _ => StatusCodes.Status500InternalServerError
    };

    private static int EvidenceStatusCode(string code) => code switch
    {
        EvidenceErrorCodes.InvalidRequest
            or EvidenceErrorCodes.InvalidRecord => StatusCodes.Status400BadRequest,
        EvidenceErrorCodes.NotFound => StatusCodes.Status404NotFound,
        EvidenceErrorCodes.DatabaseDisabled => StatusCodes.Status503ServiceUnavailable,
        _ => StatusCodes.Status500InternalServerError
    };

    private static int AiInterpretationStatusCode(string code) => code switch
    {
        AiInterpretationErrorCodes.InvalidRequest => StatusCodes.Status400BadRequest,
        AiInterpretationErrorCodes.NotFound => StatusCodes.Status404NotFound,
        AiInterpretationErrorCodes.RateLimited => StatusCodes.Status429TooManyRequests,
        AiInterpretationErrorCodes.Timeout => StatusCodes.Status504GatewayTimeout,
        AiInterpretationErrorCodes.InvalidResponse => StatusCodes.Status502BadGateway,
        AiInterpretationErrorCodes.NoEvidence => StatusCodes.Status422UnprocessableEntity,
        AiInterpretationErrorCodes.SpecialistDisabled
            or AiInterpretationErrorCodes.SpecialistNotConfigured
            or AiInterpretationErrorCodes.ProviderNotSupported
            or AiInterpretationErrorCodes.AuthenticationFailed
            or AiInterpretationErrorCodes.Unavailable
            or AiInterpretationErrorCodes.TokenLimit
            or AiInterpretationErrorCodes.DatabaseDisabled => StatusCodes.Status503ServiceUnavailable,
        _ => StatusCodes.Status500InternalServerError
    };

    private static int LocalAnalystStatusCode(string code) => code switch
    {
        LocalAnalystErrorCodes.InvalidRequest => StatusCodes.Status400BadRequest,
        LocalAnalystErrorCodes.SignalNotFound => StatusCodes.Status404NotFound,
        LocalAnalystErrorCodes.Disabled
            or LocalAnalystErrorCodes.DatabaseDisabled => StatusCodes.Status503ServiceUnavailable,
        _ => StatusCodes.Status500InternalServerError
    };

    private static int TargetAnalysisStatusCode(string code) => code switch
    {
        TargetAnalysisErrorCodes.InvalidRequest => StatusCodes.Status400BadRequest,
        TargetAnalysisErrorCodes.NotFound => StatusCodes.Status404NotFound,
        TargetAnalysisErrorCodes.InvalidState => StatusCodes.Status409Conflict,
        TargetAnalysisErrorCodes.RateLimited => StatusCodes.Status429TooManyRequests,
        TargetAnalysisErrorCodes.Timeout => StatusCodes.Status504GatewayTimeout,
        TargetAnalysisErrorCodes.InvalidResponse => StatusCodes.Status502BadGateway,
        TargetAnalysisErrorCodes.MissingMarketData
            or TargetAnalysisErrorCodes.NoEvidence => StatusCodes.Status422UnprocessableEntity,
        TargetAnalysisErrorCodes.Disabled
            or TargetAnalysisErrorCodes.DatabaseDisabled
            or TargetAnalysisErrorCodes.WorkspaceDisabled
            or TargetAnalysisErrorCodes.WorkspaceNotConfigured
            or TargetAnalysisErrorCodes.ProviderNotSupported
            or TargetAnalysisErrorCodes.AuthenticationFailed
            or TargetAnalysisErrorCodes.TokenLimit
            or TargetAnalysisErrorCodes.Unavailable => StatusCodes.Status503ServiceUnavailable,
        _ => StatusCodes.Status500InternalServerError
    };

    private static int FullAnalysisStatusCode(string code) => code switch
    {
        FullAnalysisErrorCodes.InvalidRequest => StatusCodes.Status400BadRequest,
        FullAnalysisErrorCodes.NotFound => StatusCodes.Status404NotFound,
        FullAnalysisErrorCodes.InvalidState => StatusCodes.Status409Conflict,
        FullAnalysisErrorCodes.RateLimited => StatusCodes.Status429TooManyRequests,
        FullAnalysisErrorCodes.Timeout => StatusCodes.Status504GatewayTimeout,
        FullAnalysisErrorCodes.InvalidResponse => StatusCodes.Status502BadGateway,
        FullAnalysisErrorCodes.MissingMarketData
            or FullAnalysisErrorCodes.NoEvidence => StatusCodes.Status422UnprocessableEntity,
        FullAnalysisErrorCodes.Disabled
            or FullAnalysisErrorCodes.DatabaseDisabled
            or FullAnalysisErrorCodes.WorkspaceDisabled
            or FullAnalysisErrorCodes.WorkspaceNotConfigured
            or FullAnalysisErrorCodes.ProviderNotSupported
            or FullAnalysisErrorCodes.AuthenticationFailed
            or FullAnalysisErrorCodes.TokenLimit
            or FullAnalysisErrorCodes.Unavailable => StatusCodes.Status503ServiceUnavailable,
        _ => StatusCodes.Status500InternalServerError
    };
}
