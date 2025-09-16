using System;
using AutoMapper;
using FluentAssertions;
using Library.Api.Domain;
using Library.Api.Dtos;
using Library.Api.Mappings;

namespace Library.Tests.Mappings;

public sealed class BookProfileTests
{
    private static IMapper CreateMapper()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<BookProfile>();
        });

        // Assert configuration is valid here so failures are clear
        configuration.AssertConfigurationIsValid();
        return configuration.CreateMapper();
    }

    [Fact]
    public void Configuration_Is_Valid()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<BookProfile>());
        Action act = () => config.AssertConfigurationIsValid();
        act.Should().NotThrow();
    }

    [Fact]
    public void Maps_Book_To_BookDto()
    {
        var mapper = CreateMapper();
        var now = DateTimeOffset.UtcNow;
        var book = new Book
        {
            Id = Guid.NewGuid(),
            Title = "The Pragmatic Programmer",
            Author = "Andrew Hunt",
            Genre = "Software",
            PublishedDate = new DateTimeOffset(1999, 10, 30, 0, 0, 0, TimeSpan.Zero),
            Rating = 5,
            OwnerUserId = Guid.NewGuid(),
            CreatedAt = now.AddDays(-1),
            UpdatedAt = now,
            RowVersion = new byte[] { 1, 2, 3 }
        };

        var dto = mapper.Map<BookDto>(book);

        dto.Id.Should().Be(book.Id);
        dto.Title.Should().Be(book.Title);
        dto.Author.Should().Be(book.Author);
        dto.Genre.Should().Be(book.Genre);
        dto.PublishedDate.Should().Be(book.PublishedDate);
        dto.Rating.Should().Be(book.Rating);
        dto.CreatedAt.Should().Be(book.CreatedAt);
        dto.UpdatedAt.Should().Be(book.UpdatedAt);
    }

    [Fact]
    public void Maps_CreateBookRequest_To_Book_Ignores_ServerFields_And_Sets_Timestamps()
    {
        var mapper = CreateMapper();
        var request = new CreateBookRequest(
            Title: "Clean Code",
            Author: "Robert C. Martin",
            Genre: "Software",
            PublishedDate: new DateTimeOffset(2008, 8, 1, 0, 0, 0, TimeSpan.Zero),
            Rating: 5);

        var mapped = mapper.Map<Book>(request);

        mapped.Id.Should().Be(Guid.Empty);
        mapped.OwnerUserId.Should().Be(Guid.Empty);
        mapped.RowVersion.Should().BeEmpty();

        mapped.Title.Should().Be(request.Title);
        mapped.Author.Should().Be(request.Author);
        mapped.Genre.Should().Be(request.Genre);
        mapped.PublishedDate.Should().Be(request.PublishedDate);
        mapped.Rating.Should().Be(request.Rating);

        var within = TimeSpan.FromSeconds(5);
        mapped.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, within);
        mapped.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, within);
    }

    [Fact]
    public void Maps_UpdateBookRequest_Onto_Existing_Book_Ignores_ServerFields_And_Sets_UpdatedAt()
    {
        var mapper = CreateMapper();
        var existing = new Book
        {
            Id = Guid.NewGuid(),
            OwnerUserId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-10),
            UpdatedAt = DateTimeOffset.UtcNow.AddDays(-10),
            RowVersion = new byte[] { 9, 9, 9 },
            Title = "Old Title",
            Author = "Old Author",
            Genre = "Old Genre",
            PublishedDate = new DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero),
            Rating = 1
        };

        var request = new UpdateBookRequest(
            Title: "Refactoring",
            Author: "Martin Fowler",
            Genre: "Software",
            PublishedDate: new DateTimeOffset(1999, 7, 8, 0, 0, 0, TimeSpan.Zero),
            Rating: 4);

        var mapped = mapper.Map(request, existing);

        // Server-controlled fields unchanged
        mapped.Id.Should().Be(existing.Id);
        mapped.OwnerUserId.Should().Be(existing.OwnerUserId);
        mapped.RowVersion.Should().Equal(existing.RowVersion);
        mapped.CreatedAt.Should().Be(existing.CreatedAt);

        // Updatable fields updated
        mapped.Title.Should().Be(request.Title);
        mapped.Author.Should().Be(request.Author);
        mapped.Genre.Should().Be(request.Genre);
        mapped.PublishedDate.Should().Be(request.PublishedDate);
        mapped.Rating.Should().Be(request.Rating);

        // UpdatedAt set to now by mapping
        var within = TimeSpan.FromSeconds(5);
        mapped.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, within);
    }
}
