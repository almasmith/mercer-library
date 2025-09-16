using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Library.Api.Dtos.Analytics;

namespace Library.Api.Services.Analytics
{
    public interface IAnalyticsService
    {
        Task RecordReadAsync(Guid userId, Guid bookId, CancellationToken ct);
        Task<IReadOnlyList<AvgRatingBucketDto>> GetAvgRatingByMonthAsync(Guid userId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct);
        Task<IReadOnlyList<MostReadGenreDto>> GetMostReadGenresAsync(Guid userId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct);
    }
}


