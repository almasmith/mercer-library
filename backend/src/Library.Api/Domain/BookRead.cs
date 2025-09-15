using System;

namespace Library.Api.Domain
{
    public sealed class BookRead
    {
        public Guid Id { get; set; }
        public Guid BookId { get; set; }
        public Guid UserId { get; set; }
        public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation properties (optional but useful for FK relationships)
        public ApplicationUser? User { get; set; }
        public Book? Book { get; set; }
    }
}




