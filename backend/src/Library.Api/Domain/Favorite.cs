using System;

namespace Library.Api.Domain
{
    public sealed class Favorite
    {
        public Guid UserId { get; set; }
        public Guid BookId { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation properties (optional but useful for FK relationships)
        public ApplicationUser? User { get; set; }
        public Book? Book { get; set; }
    }
}



