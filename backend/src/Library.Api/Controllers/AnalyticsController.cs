using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Library.Api.Dtos.Analytics;
using Library.Api.Infrastructure;
using Library.Api.Services.Analytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Library.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analytics;

    public AnalyticsController(IAnalyticsService analytics)
    {
        _analytics = analytics;
    }

    // B31.2: POST /api/books/{id}/read -> 204
    [HttpPost("/api/books/{id:guid}/read")]
    [SwaggerOperation(Summary = "Record a book read event", Description = "Records a read event for the specified book owned by the authenticated user.")]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Read recorded")] 
    [SwaggerResponse(StatusCodes.Status404NotFound, "Book not found or not owned")]
    public async Task<IActionResult> RecordRead([FromRoute] Guid id, CancellationToken ct)
    {
        var userId = UserContext.GetUserId(HttpContext);
        await _analytics.RecordReadAsync(userId, id, ct);
        return NoContent();
    }

    // B31.3: GET /api/analytics/avg-rating?bucket=month&from=&to=
    [HttpGet("avg-rating")]
    [Produces(MediaTypeNames.Application.Json)]
    [SwaggerOperation(Summary = "Average rating by month", Description = "Returns average book rating grouped by month within the optional date range.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Averages calculated", typeof(IReadOnlyList<AvgRatingBucketDto>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Validation errors", typeof(ValidationProblemDetails))]
    public async Task<IActionResult> GetAvgRatingByMonth(
        [FromQuery(Name = "bucket")] string? bucket,
        [FromQuery(Name = "from")] DateTimeOffset? from,
        [FromQuery(Name = "to")] DateTimeOffset? to,
        CancellationToken ct)
    {
        // Only supported bucket is "month"
        if (!string.Equals(bucket, "month", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("bucket", "Only 'month' bucket is supported.");
            return ValidationProblem(ModelState);
        }

        if (from.HasValue && to.HasValue && from.Value > to.Value)
        {
            ModelState.AddModelError("from", "From must be less than or equal to To.");
            return ValidationProblem(ModelState);
        }

        var userId = UserContext.GetUserId(HttpContext);
        var data = await _analytics.GetAvgRatingByMonthAsync(userId, from, to, ct);
        return Ok(data);
    }

    // B31.4: GET /api/analytics/most-read-genres?from=&to=
    [HttpGet("most-read-genres")]
    [Produces(MediaTypeNames.Application.Json)]
    [SwaggerOperation(Summary = "Most read genres", Description = "Returns most read genres sorted by read count descending, then genre ascending (case-insensitive).")]
    [SwaggerResponse(StatusCodes.Status200OK, "Genres calculated", typeof(IReadOnlyList<MostReadGenreDto>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Validation errors", typeof(ValidationProblemDetails))]
    public async Task<IActionResult> GetMostReadGenres(
        [FromQuery(Name = "from")] DateTimeOffset? from,
        [FromQuery(Name = "to")] DateTimeOffset? to,
        CancellationToken ct)
    {
        if (from.HasValue && to.HasValue && from.Value > to.Value)
        {
            ModelState.AddModelError("from", "From must be less than or equal to To.");
            return ValidationProblem(ModelState);
        }

        var userId = UserContext.GetUserId(HttpContext);
        var data = await _analytics.GetMostReadGenresAsync(userId, from, to, ct);
        return Ok(data);
    }
}


