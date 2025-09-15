using System;
using Microsoft.AspNetCore.Http;
using Library.Api.Middleware;

namespace Library.Api.Infrastructure
{
    internal static class CorrelationIdAccessor
    {
        public static string Get(HttpContext httpContext)
        {
            if (httpContext is null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (httpContext.Items.TryGetValue(CorrelationIdMiddleware.CorrelationIdItemKey, out var existingObj)
                && existingObj is string existingStr
                && !string.IsNullOrWhiteSpace(existingStr))
            {
                return existingStr;
            }

            // Fallback: read from header if middleware hasn't set Items yet
            if (httpContext.Request.Headers.TryGetValue(CorrelationIdMiddleware.CorrelationIdHeaderName, out var headerValue)
                && !string.IsNullOrWhiteSpace(headerValue))
            {
                return headerValue.ToString();
            }

            // As a last resort, generate a new one (keeps method total function). Also store so others can see it.
            var correlationId = Guid.NewGuid().ToString("N");
            httpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey] = correlationId;
            httpContext.Response.Headers[CorrelationIdMiddleware.CorrelationIdHeaderName] = correlationId;
            return correlationId;
        }
    }
}


