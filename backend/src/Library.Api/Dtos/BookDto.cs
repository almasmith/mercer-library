namespace Library.Api.Dtos;

public sealed record BookDto(
    Guid Id,
    string Title,
    string Author,
    string Genre,
    DateTimeOffset PublishedDate,
    int Rating,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
