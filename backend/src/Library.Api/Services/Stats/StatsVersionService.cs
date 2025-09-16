using System;
using System.Threading;
using System.Threading.Tasks;
using Library.Api.Data;
using Library.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Library.Api.Services.Stats
{
    public sealed class StatsVersionService : IStatsVersionService
    {
        private readonly LibraryDbContext _db;

        public StatsVersionService(LibraryDbContext db)
        {
            _db = db;
        }

        public async Task<long> GetVersionAsync(Guid userId, CancellationToken ct)
        {
            var entry = await _db.UserStatsVersions.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId, ct);
            return entry?.Version ?? 0L;
        }

        public async Task BumpAsync(Guid userId, CancellationToken ct)
        {
            // Concurrency-safe bump using RowVersion token and retry
            const int maxRetries = 3;
            for (var attempt = 0; attempt < maxRetries; attempt++)
            {
                var existing = await _db.UserStatsVersions.FirstOrDefaultAsync(x => x.UserId == userId, ct);
                if (existing is null)
                {
                    var now = DateTimeOffset.UtcNow;
                    var created = new UserStatsVersion
                    {
                        UserId = userId,
                        Version = 1,
                        UpdatedAt = now,
                        RowVersion = Guid.NewGuid().ToByteArray()
                    };
                    try
                    {
                        await _db.UserStatsVersions.AddAsync(created, ct);
                        await _db.SaveChangesAsync(ct);
                        return;
                    }
                    catch (DbUpdateException)
                    {
                        // Possible race: another insert won. Clear change tracker and retry update path.
                        _db.ChangeTracker.Clear();
                        continue;
                    }
                }
                else
                {
                    var now = DateTimeOffset.UtcNow;
                    existing.Version = existing.Version + 1;
                    existing.UpdatedAt = now;
                    // Update RowVersion to a new random value to act as concurrency token across providers
                    existing.RowVersion = Guid.NewGuid().ToByteArray();
                    try
                    {
                        await _db.SaveChangesAsync(ct);
                        return;
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        _db.ChangeTracker.Clear();
                        continue;
                    }
                }
            }

            // If we reach here, we failed after retries; last resort use raw increment in a transaction
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                var existing = await _db.UserStatsVersions.FirstOrDefaultAsync(x => x.UserId == userId, ct);
                if (existing is null)
                {
                    var now = DateTimeOffset.UtcNow;
                    await _db.UserStatsVersions.AddAsync(new UserStatsVersion
                    {
                        UserId = userId,
                        Version = 1,
                        UpdatedAt = now,
                        RowVersion = Guid.NewGuid().ToByteArray()
                    }, ct);
                }
                else
                {
                    existing.Version = existing.Version + 1;
                    existing.UpdatedAt = DateTimeOffset.UtcNow;
                    existing.RowVersion = Guid.NewGuid().ToByteArray();
                }
                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }
    }
}

 

