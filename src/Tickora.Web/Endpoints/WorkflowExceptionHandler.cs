using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Diagnostics;
using Tickora.Application;

namespace Tickora.Web.Endpoints;

public sealed class WorkflowExceptionHandler(ILogger<WorkflowExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, code) = exception switch
        {
            WorkflowException workflow => (workflow.Status, workflow.Message, workflow.Code),
            AntiforgeryValidationException => (400, "Invalid antiforgery token.", "antiforgery"),
            BadHttpRequestException bad => (bad.StatusCode, "Invalid request parameters or body.", "invalid_request"),
            _ => (500, "The request could not be completed.", "request_failed")
        };
        if (status >= 500) logger.LogError(exception, "Unhandled request failure {TraceIdentifier}", context.TraceIdentifier);
        await Results.Problem(statusCode: status, title: title, extensions: new Dictionary<string, object?>
        { ["code"] = code, ["traceId"] = context.TraceIdentifier }).ExecuteAsync(context);
        return true;
    }
}
