using System;

namespace Library.Api.Services.Books
{
    public sealed class BookListParameters
    {
        public string? Genre { get; init; }
        public int? MinRating { get; init; }
        public int? MaxRating { get; init; }
        public DateTimeOffset? PublishedFrom { get; init; }
        public DateTimeOffset? PublishedTo { get; init; }
        public string? Search { get; init; } // title/author contains
        public string? SortBy { get; init; } // title, author, genre, publishedDate, rating, createdAt
        public string? SortOrder { get; init; } // asc | desc
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20; // clamp to 100
    }
}


