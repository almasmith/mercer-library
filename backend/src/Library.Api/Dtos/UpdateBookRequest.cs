namespace Library.Api.Dtos;

public sealed record UpdateBookRequest(
    string Title,
    string Author,
    string Genre,
    DateTimeOffset PublishedDate,
    int Rating);
