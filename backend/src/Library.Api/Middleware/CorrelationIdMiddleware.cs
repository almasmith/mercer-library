using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Library.Api.Middleware
{
    public sealed class CorrelationIdMiddleware
    {
        public const string CorrelationIdHeaderName = "X-Correlation-ID";
        public const string CorrelationIdItemKey = "CorrelationId";

        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;

        public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var correlationId = GetOrAddCorrelationId(context);

            // Echo correlation id on every response
            context.Response.Headers[CorrelationIdHeaderName] = correlationId;

            using (_logger.BeginScope(new Dictionary<string, object> { { "CorrelationId", correlationId } }))
            {
                await _next(context);
            }
        }

        private static string GetOrAddCorrelationId(HttpContext context)
        {
            if (context.Items.TryGetValue(CorrelationIdItemKey, out var existingObj) && existingObj is string existingStr && !string.IsNullOrWhiteSpace(existingStr))
            {
                return existingStr;
            }

            string correlationId;
            if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var headerValue) && !string.IsNullOrWhiteSpace(headerValue))
            {
                correlationId = headerValue.ToString();
            }
            else
            {
                correlationId = Guid.NewGuid().ToString("N");
            }

            context.Items[CorrelationIdItemKey] = correlationId;

            return correlationId;
        }
    }
}


