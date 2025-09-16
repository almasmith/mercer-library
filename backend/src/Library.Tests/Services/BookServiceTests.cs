using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Library.Api.Data;
using Library.Api.Domain;
using Library.Api.Services;
using Library.Api.Services.Books;
using Library.Tests.Infrastructure;

namespace Library.Tests.Services;

public sealed class BookServiceTests
{
    private static readonly DateTimeOffset BaseDate = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static (LibraryDbContext Db, BookService Service, Guid OwnerA, Guid OwnerB, Action Cleanup) CreateContextWithSeed()
    {
        var (db, conn) = SqliteInMemoryDb.CreateDbContext();
        var svc = new BookService(db);

        var ownerA = Guid.NewGuid();
        var ownerB = Guid.NewGuid();

        var seed = new List<Book>
        {
            // Owner A
            new Book { Id = Guid.NewGuid(), Title = "Dune", Author = "Frank Herbert", Genre = "SciFi", PublishedDate = BaseDate.AddDays(10), Rating = 5, OwnerUserId = ownerA, CreatedAt = BaseDate.AddHours(1), UpdatedAt = BaseDate.AddHours(1) },
            new Book { Id = Guid.NewGuid(), Title = "The Hobbit", Author = "J.R.R. Tolkien", Genre = "Fantasy", PublishedDate = BaseDate.AddDays(5), Rating = 4, OwnerUserId = ownerA, CreatedAt = BaseDate.AddHours(2), UpdatedAt = BaseDate.AddHours(2) },
            new Book { Id = Guid.NewGuid(), Title = "Clean Code", Author = "Robert C. Martin", Genre = "Software", PublishedDate = BaseDate.AddDays(1), Rating = 5, OwnerUserId = ownerA, CreatedAt = BaseDate.AddHours(3), UpdatedAt = BaseDate.AddHours(3) },
            new Book { Id = Guid.NewGuid(), Title = "Dune Messiah", Author = "Frank Herbert", Genre = "SciFi", PublishedDate = BaseDate.AddDays(20), Rating = 4, OwnerUserId = ownerA, CreatedAt = BaseDate.AddHours(4), UpdatedAt = BaseDate.AddHours(4) },
            new Book { Id = Guid.NewGuid(), Title = "The Pragmatic Programmer", Author = "Andrew Hunt", Genre = "Software", PublishedDate = BaseDate.AddDays(15), Rating = 3, OwnerUserId = ownerA, CreatedAt = BaseDate.AddHours(5), UpdatedAt = BaseDate.AddHours(5) },
            new Book { Id = Guid.NewGuid(), Title = "Neuromancer", Author = "William Gibson", Genre = "SciFi", PublishedDate = BaseDate.AddDays(12), Rating = 5, OwnerUserId = ownerA, CreatedAt = BaseDate.AddHours(6), UpdatedAt = BaseDate.AddHours(6) },

            // Owner B
            new Book { Id = Guid.NewGuid(), Title = "Random Book", Author = "Other", Genre = "SciFi", PublishedDate = BaseDate.AddDays(2), Rating = 2, OwnerUserId = ownerB, CreatedAt = BaseDate.AddHours(1), UpdatedAt = BaseDate.AddHours(1) },
            new Book { Id = Guid.NewGuid(), Title = "Another", Author = "Other", Genre = "Fantasy", PublishedDate = BaseDate.AddDays(3), Rating = 4, OwnerUserId = ownerB, CreatedAt = BaseDate.AddHours(2), UpdatedAt = BaseDate.AddHours(2) },
            new Book { Id = Guid.NewGuid(), Title = "C#", Author = "Other", Genre = "Software", PublishedDate = BaseDate.AddDays(4), Rating = 5, OwnerUserId = ownerB, CreatedAt = BaseDate.AddHours(3), UpdatedAt = BaseDate.AddHours(3) }
        };

        db.Books.AddRange(seed);
        db.SaveChanges();

        void Cleanup()
        {
            SqliteInMemoryDb.Dispose(db, conn);
        }

        return (db, svc, ownerA, ownerB, Cleanup);
    }

    [Fact]
    public async Task List_Filters_By_Genre_CaseInsensitive()
    {
        var (db, svc, ownerA, _, cleanup) = CreateContextWithSeed();
        try
        {
            var p = new BookListParameters { Genre = "scifi" };
            var result = await svc.ListAsync(ownerA, p, CancellationToken.None);

            result.Items.Should().NotBeEmpty();
            result.Items.Should().OnlyContain(b => b.OwnerUserId == ownerA && b.Genre == "SciFi");
        }
        finally { cleanup(); }
    }

    [Fact]
    public async Task List_Filters_By_Rating_Range()
    {
        var (db, svc, ownerA, _, cleanup) = CreateContextWithSeed();
        try
        {
            var p = new BookListParameters { MinRating = 4, MaxRating = 5 };
            var result = await svc.ListAsync(ownerA, p, CancellationToken.None);

            result.Items.Should().NotBeEmpty();
            result.Items.Should().OnlyContain(b => b.OwnerUserId == ownerA && b.Rating >= 4 && b.Rating <= 5);
        }
        finally { cleanup(); }
    }

    [Fact]
    public async Task List_Filters_By_Published_Date_Range_Inclusive()
    {
        var (db, svc, ownerA, _, cleanup) = CreateContextWithSeed();
        try
        {
            var from = BaseDate.AddDays(10);
            var to = BaseDate.AddDays(12);
            var p = new BookListParameters { PublishedFrom = from, PublishedTo = to };
            var result = await svc.ListAsync(ownerA, p, CancellationToken.None);

            result.Items.Should().OnlyContain(b => b.OwnerUserId == ownerA && b.PublishedDate >= from && b.PublishedDate <= to);
        }
        finally { cleanup(); }
    }

    [Fact]
    public async Task List_Filters_By_Search_On_Title_Or_Author_CaseInsensitive()
    {
        var (db, svc, ownerA, _, cleanup) = CreateContextWithSeed();
        try
        {
            var p = new BookListParameters { Search = "dune" };
            var result = await svc.ListAsync(ownerA, p, CancellationToken.None);

            result.Items.Should().NotBeEmpty();
            result.Items.Should().OnlyContain(b => b.OwnerUserId == ownerA && (b.Title.ToLower().Contains("dune") || b.Author.ToLower().Contains("dune")));
        }
        finally { cleanup(); }
    }

    [Fact]
    public async Task List_Sorts_By_Title_Ascending()
    {
        var (db, svc, ownerA, _, cleanup) = CreateContextWithSeed();
        try
        {
            var p = new BookListParameters { SortBy = "title", SortOrder = "asc" };
            var result = await svc.ListAsync(ownerA, p, CancellationToken.None);

            var titles = result.Items.Select(b => b.Title).ToList();
            titles.Should().BeInAscendingOrder();
        }
        finally { cleanup(); }
    }

    [Fact]
    public async Task List_Sorts_By_Rating_Descending()
    {
        var (db, svc, ownerA, _, cleanup) = CreateContextWithSeed();
        try
        {
            var p = new BookListParameters { SortBy = "rating", SortOrder = "desc" };
            var result = await svc.ListAsync(ownerA, p, CancellationToken.None);

            var ratings = result.Items.Select(b => b.Rating).ToList();
            ratings.Should().BeInDescendingOrder();
        }
        finally { cleanup(); }
    }

    [Fact]
    public async Task List_Defaults_To_PublishedDate_Descending_On_Sqlite()
    {
        var (db, svc, ownerA, _, cleanup) = CreateContextWithSeed();
        try
        {
            var p = new BookListParameters();
            var result = await svc.ListAsync(ownerA, p, CancellationToken.None);

            var dates = result.Items.Select(b => b.PublishedDate).ToList();
            dates.Should().BeInDescendingOrder();
        }
        finally { cleanup(); }
    }

    [Fact]
    public async Task List_Sorts_By_CreatedAt_Ascending_On_Sqlite()
    {
        var (db, svc, ownerA, _, cleanup) = CreateContextWithSeed();
        try
        {
            var p = new BookListParameters { SortBy = "createdAt", SortOrder = "asc" };
            var result = await svc.ListAsync(ownerA, p, CancellationToken.None);

            var dates = result.Items.Select(b => b.CreatedAt).ToList();
            dates.Should().BeInAscendingOrder();
        }
        finally { cleanup(); }
    }

    [Fact]
    public async Task List_Paging_Returns_Correct_Slice_And_Totals()
    {
        var (db, svc, ownerA, _, cleanup) = CreateContextWithSeed();
        try
        {
            var p = new BookListParameters { SortBy = "title", SortOrder = "asc", Page = 2, PageSize = 2 };
            var result = await svc.ListAsync(ownerA, p, CancellationToken.None);

            result.Page.Should().Be(2);
            result.PageSize.Should().Be(2);
            result.TotalItems.Should().Be(6);
            result.TotalPages.Should().Be((int)Math.Ceiling(6 / 2.0));

            var expectedAll = db.Books.AsQueryable().Where(b => b.OwnerUserId == ownerA).OrderBy(b => b.Title).Select(b => b.Title).ToList();
            var expectedPage = expectedAll.Skip(2).Take(2).ToList();
            result.Items.Select(b => b.Title).Should().Equal(expectedPage);
        }
        finally { cleanup(); }
    }

    [Fact]
    public async Task CreateAsync_Sets_Timestamps_And_Owner_And_Persists()
    {
        var (db, svc, ownerA, _, cleanup) = CreateContextWithSeed();
        try
        {
            var nowBefore = DateTimeOffset.UtcNow;
            var book = new Book
            {
                Title = "Domain-Driven Design",
                Author = "Eric Evans",
                Genre = "Software",
                PublishedDate = BaseDate.AddDays(30),
                Rating = 5
            };

            var created = await svc.CreateAsync(ownerA, book, CancellationToken.None);
            var nowAfter = DateTimeOffset.UtcNow;

            created.OwnerUserId.Should().Be(ownerA);
            created.CreatedAt.Should().BeOnOrAfter(nowBefore).And.BeOnOrBefore(nowAfter);
            created.UpdatedAt.Should().BeOnOrAfter(nowBefore).And.BeOnOrBefore(nowAfter);
            created.UpdatedAt.Should().BeCloseTo(created.CreatedAt, TimeSpan.FromSeconds(1));

            var fromDb = db.Books.Single(b => b.Id == created.Id);
            fromDb.Should().NotBeNull();
            fromDb.OwnerUserId.Should().Be(ownerA);
        }
        finally { cleanup(); }
    }

    [Fact]
    public async Task UpdateAsync_Updates_Fields_And_UpdatedAt_For_Correct_Owner()
    {
        var (db, svc, ownerA, ownerB, cleanup) = CreateContextWithSeed();
        try
        {
            var target = db.Books.First(b => b.OwnerUserId == ownerA);
            var originalUpdated = target.UpdatedAt;

            var updated = await svc.UpdateAsync(ownerA, target.Id, b =>
            {
                b.Title = "Updated Title";
                b.Rating = 2;
            }, CancellationToken.None);

            updated.Should().NotBeNull();
            updated!.Title.Should().Be("Updated Title");
            updated.Rating.Should().Be(2);
            updated.UpdatedAt.Should().BeAfter(originalUpdated);
        }
        finally { cleanup(); }
    }

    [Fact]
    public async Task UpdateAsync_Returns_Null_When_Wrong_Owner()
    {
        var (db, svc, ownerA, ownerB, cleanup) = CreateContextWithSeed();
        try
        {
            var target = db.Books.First(b => b.OwnerUserId == ownerA);
            var originalTitle = target.Title;

            var updated = await svc.UpdateAsync(ownerB, target.Id, b => b.Title = "Hacked", CancellationToken.None);
            updated.Should().BeNull();

            var fromDb = db.Books.Single(b => b.Id == target.Id);
            fromDb.Title.Should().Be(originalTitle);
        }
        finally { cleanup(); }
    }

    [Fact]
    public async Task DeleteAsync_Removes_For_Correct_Owner_And_Scoped_By_Owner()
    {
        var (db, svc, ownerA, ownerB, cleanup) = CreateContextWithSeed();
        try
        {
            var target = db.Books.First(b => b.OwnerUserId == ownerA);

            var deleted = await svc.DeleteAsync(ownerA, target.Id, CancellationToken.None);
            deleted.Should().BeTrue();
            db.Books.Any(b => b.Id == target.Id).Should().BeFalse();

            // Wrong owner cannot delete
            var other = db.Books.First(b => b.OwnerUserId == ownerB);
            var deletedOther = await svc.DeleteAsync(ownerA, other.Id, CancellationToken.None);
            deletedOther.Should().BeFalse();
            db.Books.Any(b => b.Id == other.Id).Should().BeTrue();
        }
        finally { cleanup(); }
    }
}


