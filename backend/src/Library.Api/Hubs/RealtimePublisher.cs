using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Library.Api.Hubs;

public sealed class RealtimePublisher : IRealtimePublisher
{
    private readonly IHubContext<LibraryHub> _hubContext;

    public RealtimePublisher(IHubContext<LibraryHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task BookCreated(Guid userId, object payload, CancellationToken ct)
    {
        return SendToUserGroup(userId, LibraryEvents.BookCreated, payload, ct);
    }

    public Task BookUpdated(Guid userId, object payload, CancellationToken ct)
    {
        return SendToUserGroup(userId, LibraryEvents.BookUpdated, payload, ct);
    }

    public Task BookDeleted(Guid userId, Guid bookId, CancellationToken ct)
    {
        return SendToUserGroup(userId, LibraryEvents.BookDeleted, new { id = bookId }, ct);
    }

    public Task BookFavorited(Guid userId, Guid bookId, CancellationToken ct)
    {
        return SendToUserGroup(userId, LibraryEvents.BookFavorited, new { id = bookId }, ct);
    }

    public Task BookUnfavorited(Guid userId, Guid bookId, CancellationToken ct)
    {
        return SendToUserGroup(userId, LibraryEvents.BookUnfavorited, new { id = bookId }, ct);
    }

    public Task BookRead(Guid userId, Guid bookId, CancellationToken ct)
    {
        return SendToUserGroup(userId, LibraryEvents.BookRead, new { id = bookId }, ct);
    }

    public Task StatsUpdated(Guid userId, CancellationToken ct)
    {
        return SendToUserGroup(userId, LibraryEvents.StatsUpdated, new { }, ct);
    }

    private Task SendToUserGroup(Guid userId, string eventName, object payload, CancellationToken ct)
    {
        var group = $"user:{userId}";
        return _hubContext.Clients.Group(group).SendAsync(eventName, payload, ct);
    }
}


