using System;

namespace Library.Api.Domain
{
    public sealed class Book
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public DateTimeOffset PublishedDate { get; set; }
        public int Rating { get; set; }
        public Guid OwnerUserId { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}


