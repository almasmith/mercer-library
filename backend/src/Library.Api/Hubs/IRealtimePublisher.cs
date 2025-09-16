using System;
using System.Threading;
using System.Threading.Tasks;

namespace Library.Api.Hubs;

public interface IRealtimePublisher
{
    Task BookCreated(Guid userId, object payload, CancellationToken ct);
    Task BookUpdated(Guid userId, object payload, CancellationToken ct);
    Task BookDeleted(Guid userId, Guid bookId, CancellationToken ct);
    Task BookFavorited(Guid userId, Guid bookId, CancellationToken ct);
    Task BookUnfavorited(Guid userId, Guid bookId, CancellationToken ct);
    Task BookRead(Guid userId, Guid bookId, CancellationToken ct);
    Task StatsUpdated(Guid userId, CancellationToken ct);
}


