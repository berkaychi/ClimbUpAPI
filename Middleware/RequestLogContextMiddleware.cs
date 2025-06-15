using Serilog.Context;
using System.Security.Claims;

namespace ClimbUpAPI.Middleware
{
    public class RequestLogContextMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestLogContextMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            LogContext.PushProperty("RequestId", context.TraceIdentifier);
            LogContext.PushProperty("IPAddress", context.Connection.RemoteIpAddress?.ToString());

            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                LogContext.PushProperty("UserId", userId);
            }

            var userName = context.User.Identity?.Name;
            if (!string.IsNullOrEmpty(userName))
            {
                LogContext.PushProperty("UserName", userName);
            }

            string correlationId;
            if (context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationIdValues))
            {
                correlationId = correlationIdValues.FirstOrDefault() ?? Guid.NewGuid().ToString();
            }
            else
            {
                correlationId = Guid.NewGuid().ToString();
            }
            LogContext.PushProperty("CorrelationId", correlationId);

            try
            {
                await _next(context);
            }
            finally
            {

            }
        }
    }
}