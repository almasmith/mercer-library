using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Library.Api.Middleware
{
    public sealed class ExceptionHandlingMiddleware
    {
        private const string CorrelationIdHeaderName = "X-Correlation-ID";
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                var correlationId = GetOrCreateCorrelationId(context);
                var (statusCode, title) = MapExceptionToStatusAndTitle(ex);

                var problem = new ProblemDetails
                {
                    Type = GetProblemType(statusCode),
                    Title = title,
                    Status = statusCode,
                    Detail = GetSafeDetailMessage(ex, statusCode),
                    Instance = $"{context.Request.Path}?cid={correlationId}"
                };

                LogException(ex, statusCode, correlationId, context.Request.Path);

                context.Response.Clear();
                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "application/problem+json";

                var json = JsonSerializer.Serialize(problem, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                });
                await context.Response.WriteAsync(json);
            }
        }

        private static string GetOrCreateCorrelationId(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var existing) && !string.IsNullOrWhiteSpace(existing))
            {
                var cid = existing.ToString();
                context.Response.Headers[CorrelationIdHeaderName] = cid;
                return cid;
            }

            var correlationId = Guid.NewGuid().ToString("N");
            context.Response.Headers[CorrelationIdHeaderName] = correlationId;
            return correlationId;
        }

        private static (int statusCode, string title) MapExceptionToStatusAndTitle(Exception ex)
        {
            return ex switch
            {
                KeyNotFoundException => (StatusCodes.Status404NotFound, ReasonPhrases.GetReasonPhrase(StatusCodes.Status404NotFound)),
                ArgumentException => (StatusCodes.Status400BadRequest, ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest)),
                InvalidOperationException => (StatusCodes.Status400BadRequest, ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest)),
                _ => (StatusCodes.Status500InternalServerError, ReasonPhrases.GetReasonPhrase(StatusCodes.Status500InternalServerError))
            };
        }

        private static string GetProblemType(int statusCode)
        {
            return $"https://httpstatuses.com/{statusCode}";
        }

        private static string GetSafeDetailMessage(Exception ex, int statusCode)
        {
            if (statusCode >= 500)
            {
                return "An unexpected error occurred.";
            }

            return string.IsNullOrWhiteSpace(ex.Message) ? ReasonPhrases.GetReasonPhrase(statusCode) : ex.Message;
        }

        private void LogException(Exception ex, int statusCode, string correlationId, PathString path)
        {
            var message = "Request {Path} failed with status {StatusCode}. CorrelationId={CorrelationId}";
            if (statusCode >= 500)
            {
                _logger.LogError(ex, message, path.ToString(), statusCode, correlationId);
            }
            else
            {
                _logger.LogWarning(ex, message, path.ToString(), statusCode, correlationId);
            }
        }
    }
}


