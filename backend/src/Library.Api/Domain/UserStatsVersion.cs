using System;

namespace Library.Api.Domain
{
    public sealed class UserStatsVersion
    {
        public Guid UserId { get; set; }
        public long Version { get; set; } = 0L;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}

 

