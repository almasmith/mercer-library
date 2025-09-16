namespace Library.Api.Dtos;

public sealed record CreateBookRequest(
    string Title,
    string Author,
    string Genre,
    DateTimeOffset PublishedDate,
    int Rating);
