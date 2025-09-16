using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Library.Api.Data;
using Library.Api.Domain;
using Library.Api.Services.Books;
using Library.Api.Services.Favorites;
using Library.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Library.Api.Hubs;

namespace Library.Tests.Services;

public sealed class FavoritesServiceTests
{
    private sealed class NoopPublisher : IRealtimePublisher
    {
        public Task BookCreated(Guid userId, object payload, CancellationToken ct) => Task.CompletedTask;
        public Task BookUpdated(Guid userId, object payload, CancellationToken ct) => Task.CompletedTask;
        public Task BookDeleted(Guid userId, Guid bookId, CancellationToken ct) => Task.CompletedTask;
        public Task BookFavorited(Guid userId, Guid bookId, CancellationToken ct) => Task.CompletedTask;
        public Task BookUnfavorited(Guid userId, Guid bookId, CancellationToken ct) => Task.CompletedTask;
        public Task BookRead(Guid userId, Guid bookId, CancellationToken ct) => Task.CompletedTask;
        public Task StatsUpdated(Guid userId, CancellationToken ct) => Task.CompletedTask;
    }

    private static readonly DateTimeOffset BaseDate = new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static (LibraryDbContext Db, FavoritesService Service, Guid OwnerA, Guid OwnerB, List<Book> OwnerABooks, Action Cleanup) CreateContextWithSeed()
    {
        var (db, conn) = SqliteInMemoryDb.CreateDbContext();
        var svc = new FavoritesService(db, new NoopPublisher());

        var ownerA = Guid.NewGuid();
        var ownerB = Guid.NewGuid();

        // Seed users to satisfy FK for Favorites(UserId)
        db.Users.AddRange(
            new ApplicationUser
            {
                Id = ownerA,
                UserName = "ownera",
                NormalizedUserName = "OWNERA",
                Email = "a@example.com",
                NormalizedEmail = "A@EXAMPLE.COM",
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
            },
            new ApplicationUser
            {
                Id = ownerB,
                UserName = "ownerb",
                NormalizedUserName = "OWNERB",
                Email = "b@example.com",
                NormalizedEmail = "B@EXAMPLE.COM",
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
            }
        );
        db.SaveChanges();

        var seed = new List<Book>
        {
            // Owner A (6 books)
            new Book { Id = Guid.NewGuid(), Title = "Alpha", Author = "Author A", Genre = "SciFi", PublishedDate = BaseDate.AddDays(10), Rating = 5, OwnerUserId = ownerA, CreatedAt = BaseDate.AddHours(1), UpdatedAt = BaseDate.AddHours(1) },
            new Book { Id = Guid.NewGuid(), Title = "Beta", Author = "Author B", Genre = "Fantasy", PublishedDate = BaseDate.AddDays(5), Rating = 4, OwnerUserId = ownerA, CreatedAt = BaseDate.AddHours(2), UpdatedAt = BaseDate.AddHours(2) },
            new Book { Id = Guid.NewGuid(), Title = "Gamma", Author = "Author C", Genre = "Software", PublishedDate = BaseDate.AddDays(1), Rating = 5, OwnerUserId = ownerA, CreatedAt = BaseDate.AddHours(3), UpdatedAt = BaseDate.AddHours(3) },
            new Book { Id = Guid.NewGuid(), Title = "Delta", Author = "Author D", Genre = "SciFi", PublishedDate = BaseDate.AddDays(20), Rating = 4, OwnerUserId = ownerA, CreatedAt = BaseDate.AddHours(4), UpdatedAt = BaseDate.AddHours(4) },
            new Book { Id = Guid.NewGuid(), Title = "Epsilon", Author = "Author E", Genre = "Software", PublishedDate = BaseDate.AddDays(15), Rating = 3, OwnerUserId = ownerA, CreatedAt = BaseDate.AddHours(5), UpdatedAt = BaseDate.AddHours(5) },
            new Book { Id = Guid.NewGuid(), Title = "Zeta", Author = "Author F", Genre = "SciFi", PublishedDate = BaseDate.AddDays(12), Rating = 5, OwnerUserId = ownerA, CreatedAt = BaseDate.AddHours(6), UpdatedAt = BaseDate.AddHours(6) },

            // Owner B (3 books)
            new Book { Id = Guid.NewGuid(), Title = "Theta", Author = "Other 1", Genre = "SciFi", PublishedDate = BaseDate.AddDays(2), Rating = 2, OwnerUserId = ownerB, CreatedAt = BaseDate.AddHours(1), UpdatedAt = BaseDate.AddHours(1) },
            new Book { Id = Guid.NewGuid(), Title = "Iota", Author = "Other 2", Genre = "Fantasy", PublishedDate = BaseDate.AddDays(3), Rating = 4, OwnerUserId = ownerB, CreatedAt = BaseDate.AddHours(2), UpdatedAt = BaseDate.AddHours(2) },
            new Book { Id = Guid.NewGuid(), Title = "Kappa", Author = "Other 3", Genre = "Software", PublishedDate = BaseDate.AddDays(4), Rating = 5, OwnerUserId = ownerB, CreatedAt = BaseDate.AddHours(3), UpdatedAt = BaseDate.AddHours(3) }
        };

        db.Books.AddRange(seed);
        db.SaveChanges();

        void Cleanup()
        {
            SqliteInMemoryDb.Dispose(db, conn);
        }

        var ownerABooks = seed.Where(b => b.OwnerUserId == ownerA).ToList();
        return (db, svc, ownerA, ownerB, ownerABooks, Cleanup);
    }

    [Fact]
    public async Task FavoriteAsync_Is_Idempotent_And_Returns_False_For_Wrong_Owner()
    {
        var (db, svc, ownerA, ownerB, ownerABooks, cleanup) = CreateContextWithSeed();
        try
        {
            var book = ownerABooks.First();
            var ct = CancellationToken.None;

            // Verify seed exists and mirrors service query
            (await db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == book.Id && b.OwnerUserId == ownerA, ct)).Should().NotBeNull();

            var first = await svc.FavoriteAsync(ownerA, book.Id, ct);
            first.Should().BeTrue();
            (await svc.IsFavoritedAsync(ownerA, book.Id, ct)).Should().BeTrue();

            var second = await svc.FavoriteAsync(ownerA, book.Id, ct);
            second.Should().BeTrue();
            (await svc.IsFavoritedAsync(ownerA, book.Id, ct)).Should().BeTrue();

            var wrongOwner = await svc.FavoriteAsync(ownerB, book.Id, ct);
            wrongOwner.Should().BeFalse(); // controller would translate to 404
        }
        finally { cleanup(); }
    }

    [Fact]
    public async Task UnfavoriteAsync_Is_Idempotent_And_Returns_False_For_Wrong_Owner()
    {
        var (db, svc, ownerA, ownerB, ownerABooks, cleanup) = CreateContextWithSeed();
        try
        {
            var book = ownerABooks.First();
            var ct = CancellationToken.None;

            // Verify seed exists and mirrors service query
            (await db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == book.Id && b.OwnerUserId == ownerA, ct)).Should().NotBeNull();

            // Unfavorite before favorited should still return true (idempotent), but service first checks ownership via book
            var unfavBefore = await svc.UnfavoriteAsync(ownerA, book.Id, ct);
            unfavBefore.Should().BeTrue();
            (await svc.IsFavoritedAsync(ownerA, book.Id, ct)).Should().BeFalse();

            var fav = await svc.FavoriteAsync(ownerA, book.Id, ct);
            fav.Should().BeTrue();
            (await svc.IsFavoritedAsync(ownerA, book.Id, ct)).Should().BeTrue();

            var unfav = await svc.UnfavoriteAsync(ownerA, book.Id, ct);
            unfav.Should().BeTrue();
            (await svc.IsFavoritedAsync(ownerA, book.Id, ct)).Should().BeFalse();

            // Repeated unfavorite is still true (idempotent)
            var unfavAgain = await svc.UnfavoriteAsync(ownerA, book.Id, ct);
            unfavAgain.Should().BeTrue();

            // Wrong owner path -> service returns false (controller translates to 404)
            var wrongOwner = await svc.UnfavoriteAsync(ownerB, book.Id, ct);
            wrongOwner.Should().BeFalse();
        }
        finally { cleanup(); }
    }

    [Fact]
    public async Task ListAsync_Returns_Only_Favorited_Books_For_Owner_With_Filters_Sort_And_Paging()
    {
        var (db, svc, ownerA, _, ownerABooks, cleanup) = CreateContextWithSeed();
        try
        {
            var ct = CancellationToken.None;

            // Favorite 5 of the 6 OwnerA books: Alpha, Beta, Gamma, Delta, Zeta
            var toFavorite = ownerABooks.Where(b => new[] { "Alpha", "Beta", "Gamma", "Delta", "Zeta" }.Contains(b.Title)).ToList();
            foreach (var b in toFavorite)
            {
                // Verify seed exists and ownership matches
                (await db.Books.AsNoTracking().FirstOrDefaultAsync(x => x.Id == b.Id && x.OwnerUserId == ownerA, ct)).Should().NotBeNull();

                var ok = await svc.FavoriteAsync(ownerA, b.Id, ct);
                ok.Should().BeTrue();
            }

            // Filter by Genre = SciFi (Alpha, Delta, Zeta)
            var pGenre = new BookListParameters { Genre = "scifi", SortBy = "title", SortOrder = "asc" };
            var byGenre = await svc.ListAsync(ownerA, pGenre, ct);
            byGenre.Items.Should().OnlyContain(b => b.OwnerUserId == ownerA && b.Genre == "SciFi");
            byGenre.Items.Select(b => b.Title).Should().Equal(new[] { "Alpha", "Delta", "Zeta" });

            // Filter by Rating range 4..5 -> all favorited except Epsilon (not favorited) and ratings < 4
            var pRating = new BookListParameters { MinRating = 4, MaxRating = 5, SortBy = "rating", SortOrder = "desc" };
            var byRating = await svc.ListAsync(ownerA, pRating, ct);
            byRating.Items.Should().OnlyContain(b => b.OwnerUserId == ownerA && b.Rating >= 4 && b.Rating <= 5);
            byRating.Items.Select(b => b.Rating).Should().BeInDescendingOrder();

            // Search by title contains 'a' (case-insensitive) among favorited, sorted by title asc, page 1 size 2
            var pSearch = new BookListParameters { Search = "a", SortBy = "title", SortOrder = "asc", Page = 1, PageSize = 2 };
            var bySearchPage1 = await svc.ListAsync(ownerA, pSearch, ct);
            var expectedAll = toFavorite.Where(b => (b.Title.ToLower().Contains("a") || b.Author.ToLower().Contains("a")))
                .OrderBy(b => b.Title).ToList();
            bySearchPage1.TotalItems.Should().Be(expectedAll.Count);
            bySearchPage1.TotalPages.Should().Be((int)Math.Ceiling(expectedAll.Count / 2.0));
            bySearchPage1.Items.Select(b => b.Title).Should().Equal(expectedAll.Take(2).Select(b => b.Title));

            // Page 2 slice
            var pSearchPage2 = new BookListParameters { Search = "a", SortBy = "title", SortOrder = "asc", Page = 2, PageSize = 2 };
            var bySearchPage2 = await svc.ListAsync(ownerA, pSearchPage2, ct);
            bySearchPage2.Items.Select(b => b.Title).Should().Equal(expectedAll.Skip(2).Take(2).Select(b => b.Title));
        }
        finally { cleanup(); }
    }
}
