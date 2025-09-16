using System;
using System.Threading;
using System.Threading.Tasks;

namespace Library.Api.Services.Stats
{
    public interface IStatsVersionService
    {
        Task<long> GetVersionAsync(Guid userId, CancellationToken ct);
        Task BumpAsync(Guid userId, CancellationToken ct);
    }
}

 

