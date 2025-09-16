using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Library.Api.Data;
using Library.Api.Domain;
using Library.Api.Dtos.Analytics;
using Library.Api.Services.Stats;
using Microsoft.EntityFrameworkCore;

namespace Library.Api.Services.Analytics
{
    public sealed class AnalyticsService : IAnalyticsService
    {
        private readonly LibraryDbContext _db;
        private readonly IStatsVersionService _stats;

        public AnalyticsService(LibraryDbContext db)
        {
            _db = db;
            _stats = null!; // optional for tests; DI uses the other ctor
        }

        public AnalyticsService(LibraryDbContext db, IStatsVersionService stats)
        {
            _db = db;
            _stats = stats;
        }

        public async Task RecordReadAsync(Guid userId, Guid bookId, CancellationToken ct)
        {
            var book = await _db.Books
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == bookId && b.OwnerUserId == userId, ct);

            if (book == null)
            {
                throw new KeyNotFoundException("Book not found for this user.");
            }

            var read = new BookRead
            {
                Id = Guid.NewGuid(),
                BookId = bookId,
                UserId = userId,
                OccurredAt = DateTimeOffset.UtcNow
            };

            await _db.BookReads.AddAsync(read, ct);
            await _db.SaveChangesAsync(ct);

            if (_stats != null)
            {
                await _stats.BumpAsync(userId, ct);
            }
        }

        public async Task<IReadOnlyList<AvgRatingBucketDto>> GetAvgRatingByMonthAsync(
            Guid userId,
            DateTimeOffset? from,
            DateTimeOffset? to,
            CancellationToken ct)
        {
            var reads = _db.BookReads
                .AsNoTracking()
                .Where(r => r.UserId == userId);

            if (from.HasValue)
            {
                var fromValue = from.Value;
                reads = reads.Where(r => r.OccurredAt >= fromValue);
            }

            if (to.HasValue)
            {
                var toValue = to.Value;
                reads = reads.Where(r => r.OccurredAt <= toValue);
            }

            var joined = reads
                .Join(
                    _db.Books.AsNoTracking(),
                    r => r.BookId,
                    b => b.Id,
                    (r, b) => new { r.OccurredAt, b.Rating }
                );

            // Materialize then group in-memory for cross-provider compatibility (SQLite/SqlServer)
            var items = await joined.ToListAsync(ct);

            var buckets = items
                .GroupBy(x => new { x.OccurredAt.UtcDateTime.Year, x.OccurredAt.UtcDateTime.Month })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.Month)
                .Select(g => new AvgRatingBucketDto(
                    Bucket: string.Create(CultureInfo.InvariantCulture, $"{g.Key.Year:D4}-{g.Key.Month:D2}"),
                    Average: g.Average(v => (double)v.Rating)))
                .ToList();

            return buckets;
        }

        public async Task<IReadOnlyList<MostReadGenreDto>> GetMostReadGenresAsync(
            Guid userId,
            DateTimeOffset? from,
            DateTimeOffset? to,
            CancellationToken ct)
        {
            var reads = _db.BookReads
                .AsNoTracking()
                .Where(r => r.UserId == userId);

            if (from.HasValue)
            {
                var fromValue = from.Value;
                reads = reads.Where(r => r.OccurredAt >= fromValue);
            }

            if (to.HasValue)
            {
                var toValue = to.Value;
                reads = reads.Where(r => r.OccurredAt <= toValue);
            }

            var joined = reads
                .Join(
                    _db.Books.AsNoTracking(),
                    r => r.BookId,
                    b => b.Id,
                    (r, b) => new { r.OccurredAt, Genre = b.Genre }
                );

            var items = await joined.ToListAsync(ct);

            var results = items
                .Select(x => new
                {
                    Original = (x.Genre ?? string.Empty).Trim(),
                    Normalized = (x.Genre ?? string.Empty).Trim().ToLowerInvariant()
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Normalized))
                .GroupBy(x => x.Normalized)
                .Select(g => new
                {
                    Normalized = g.Key,
                    ReadCount = g.Count(),
                    // Prefer the first original-cased appearance in the group
                    Display = g.Select(v => v.Original).FirstOrDefault() ?? g.Key
                })
                .OrderByDescending(x => x.ReadCount)
                .ThenBy(x => x.Normalized, StringComparer.Ordinal)
                .Select(x => new MostReadGenreDto(x.Display, x.ReadCount))
                .ToList();

            return results;
        }
    }
}


