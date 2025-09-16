using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Library.Api.Data;
using Library.Api.Domain;
using Library.Api.Dtos.Analytics;
using Library.Api.Services.Analytics;
using Library.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Library.Tests.Services;

public sealed class AnalyticsServiceTests
{
    private static ApplicationUser CreateUser(Guid id, string userName, string email)
    {
        return new ApplicationUser
        {
            Id = id,
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
        };
    }

    [Fact]
    public async Task RecordReadAsync_Verifies_Ownership()
    {
        var (db, conn) = SqliteInMemoryDb.CreateDbContext();
        try
        {
            var ownerA = Guid.NewGuid();
            var ownerB = Guid.NewGuid();

            db.Users.AddRange(
                CreateUser(ownerA, "ownera", "a@example.com"),
                CreateUser(ownerB, "ownerb", "b@example.com")
            );
            await db.SaveChangesAsync();

            var bookOwned = new Book
            {
                Id = Guid.NewGuid(),
                Title = "Owned",
                Author = "Author",
                Genre = "Genre",
                PublishedDate = DateTimeOffset.UtcNow,
                Rating = 4,
                OwnerUserId = ownerA,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            var bookNotOwned = new Book
            {
                Id = Guid.NewGuid(),
                Title = "NotOwned",
                Author = "Author",
                Genre = "Genre",
                PublishedDate = DateTimeOffset.UtcNow,
                Rating = 3,
                OwnerUserId = ownerB,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            db.Books.AddRange(bookOwned, bookNotOwned);
            await db.SaveChangesAsync();

            var svc = new AnalyticsService(db);
            var ct = CancellationToken.None;

            // Succeeds for owned book
            await svc.RecordReadAsync(ownerA, bookOwned.Id, ct);
            (await db.BookReads.CountAsync()).Should().Be(1);
            var created = await db.BookReads.AsQueryable().SingleAsync();
            created.UserId.Should().Be(ownerA);
            created.BookId.Should().Be(bookOwned.Id);

            // Throws for book not owned by the user
            var act = async () => await svc.RecordReadAsync(ownerA, bookNotOwned.Id, ct);
            await act.Should().ThrowAsync<KeyNotFoundException>();

            // No extra reads were created on failure
            (await db.BookReads.CountAsync()).Should().Be(1);
        }
        finally
        {
            SqliteInMemoryDb.Dispose(db, conn);
        }
    }

    [Fact]
    public async Task GetAvgRatingByMonthAsync_Buckets_By_YearMonth_And_Computes_Averages()
    {
        var (db, conn) = SqliteInMemoryDb.CreateDbContext();
        try
        {
            var owner = Guid.NewGuid();
            db.Users.Add(CreateUser(owner, "owner", "owner@example.com"));
            await db.SaveChangesAsync();

            // Books with distinct ratings
            var bookHi = new Book { Id = Guid.NewGuid(), Title = "Hi", Author = "A", Genre = "G", PublishedDate = new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.Zero), Rating = 5, OwnerUserId = owner, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
            var bookLow = new Book { Id = Guid.NewGuid(), Title = "Low", Author = "A", Genre = "G", PublishedDate = new DateTimeOffset(2022, 1, 2, 0, 0, 0, TimeSpan.Zero), Rating = 1, OwnerUserId = owner, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
            var bookMid = new Book { Id = Guid.NewGuid(), Title = "Mid", Author = "A", Genre = "G", PublishedDate = new DateTimeOffset(2022, 3, 1, 0, 0, 0, TimeSpan.Zero), Rating = 4, OwnerUserId = owner, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
            db.Books.AddRange(bookHi, bookLow, bookMid);
            await db.SaveChangesAsync();

            // Reads across months
            db.BookReads.AddRange(
                new BookRead { Id = Guid.NewGuid(), BookId = bookHi.Id, UserId = owner, OccurredAt = new DateTimeOffset(2022, 1, 15, 0, 0, 0, TimeSpan.Zero) }, // 5
                new BookRead { Id = Guid.NewGuid(), BookId = bookLow.Id, UserId = owner, OccurredAt = new DateTimeOffset(2022, 1, 20, 0, 0, 0, TimeSpan.Zero) }, // 1 => Jan avg = 3.0

                new BookRead { Id = Guid.NewGuid(), BookId = bookLow.Id, UserId = owner, OccurredAt = new DateTimeOffset(2022, 3, 5, 0, 0, 0, TimeSpan.Zero) },  // 1
                new BookRead { Id = Guid.NewGuid(), BookId = bookMid.Id, UserId = owner, OccurredAt = new DateTimeOffset(2022, 3, 10, 0, 0, 0, TimeSpan.Zero) }, // 4
                new BookRead { Id = Guid.NewGuid(), BookId = bookHi.Id, UserId = owner, OccurredAt = new DateTimeOffset(2022, 3, 12, 0, 0, 0, TimeSpan.Zero) }   // 5 => Mar avg = (1+4+5)/3
            );
            await db.SaveChangesAsync();

            var svc = new AnalyticsService(db);
            var result = await svc.GetAvgRatingByMonthAsync(owner, from: null, to: null, ct: CancellationToken.None);

            result.Should().NotBeNull();
            result.Select(x => x.Bucket).Should().Equal(new[] { "2022-01", "2022-03" });

            var jan = result.First(x => x.Bucket == "2022-01");
            jan.Average.Should().BeApproximately(3.0, 1e-9);

            var mar = result.First(x => x.Bucket == "2022-03");
            mar.Average.Should().BeApproximately((1 + 4 + 5) / 3.0, 1e-9);
        }
        finally
        {
            SqliteInMemoryDb.Dispose(db, conn);
        }
    }

    [Fact]
    public async Task GetMostReadGenresAsync_Normalizes_Excludes_Empty_And_Sorts()
    {
        var (db, conn) = SqliteInMemoryDb.CreateDbContext();
        try
        {
            var owner = Guid.NewGuid();
            db.Users.Add(CreateUser(owner, "owner", "owner@example.com"));
            await db.SaveChangesAsync();

            // Books for various genre representations (case/whitespace differences and empties)
            var sci1 = new Book { Id = Guid.NewGuid(), Title = "S1", Author = "A", Genre = "SciFi", PublishedDate = DateTimeOffset.UtcNow, Rating = 5, OwnerUserId = owner, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
            var sci2 = new Book { Id = Guid.NewGuid(), Title = "S2", Author = "A", Genre = " scifi ", PublishedDate = DateTimeOffset.UtcNow, Rating = 4, OwnerUserId = owner, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };

            var fan1 = new Book { Id = Guid.NewGuid(), Title = "F1", Author = "A", Genre = "FANTASY", PublishedDate = DateTimeOffset.UtcNow, Rating = 3, OwnerUserId = owner, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
            var fan2 = new Book { Id = Guid.NewGuid(), Title = "F2", Author = "A", Genre = "fantasy", PublishedDate = DateTimeOffset.UtcNow, Rating = 5, OwnerUserId = owner, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };

            var mys1 = new Book { Id = Guid.NewGuid(), Title = "M1", Author = "A", Genre = "Mystery", PublishedDate = DateTimeOffset.UtcNow, Rating = 4, OwnerUserId = owner, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
            var mys2 = new Book { Id = Guid.NewGuid(), Title = "M2", Author = "A", Genre = " mystery  ", PublishedDate = DateTimeOffset.UtcNow, Rating = 4, OwnerUserId = owner, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };

            var empty = new Book { Id = Guid.NewGuid(), Title = "E", Author = "A", Genre = "   ", PublishedDate = DateTimeOffset.UtcNow, Rating = 2, OwnerUserId = owner, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
            var soft = new Book { Id = Guid.NewGuid(), Title = "SW", Author = "A", Genre = "Software", PublishedDate = DateTimeOffset.UtcNow, Rating = 5, OwnerUserId = owner, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };

            db.Books.AddRange(sci1, sci2, fan1, fan2, mys1, mys2, empty, soft);
            await db.SaveChangesAsync();

            // Reads: scifi(3), fantasy(2), mystery(2), software(1), empty(1 but excluded)
            var ownerId = owner;
            db.BookReads.AddRange(
                new BookRead { Id = Guid.NewGuid(), BookId = sci1.Id, UserId = ownerId, OccurredAt = DateTimeOffset.UtcNow },
                new BookRead { Id = Guid.NewGuid(), BookId = sci2.Id, UserId = ownerId, OccurredAt = DateTimeOffset.UtcNow },
                new BookRead { Id = Guid.NewGuid(), BookId = sci1.Id, UserId = ownerId, OccurredAt = DateTimeOffset.UtcNow },

                new BookRead { Id = Guid.NewGuid(), BookId = fan1.Id, UserId = ownerId, OccurredAt = DateTimeOffset.UtcNow },
                new BookRead { Id = Guid.NewGuid(), BookId = fan2.Id, UserId = ownerId, OccurredAt = DateTimeOffset.UtcNow },

                new BookRead { Id = Guid.NewGuid(), BookId = mys1.Id, UserId = ownerId, OccurredAt = DateTimeOffset.UtcNow },
                new BookRead { Id = Guid.NewGuid(), BookId = mys2.Id, UserId = ownerId, OccurredAt = DateTimeOffset.UtcNow },

                new BookRead { Id = Guid.NewGuid(), BookId = soft.Id, UserId = ownerId, OccurredAt = DateTimeOffset.UtcNow },

                new BookRead { Id = Guid.NewGuid(), BookId = empty.Id, UserId = ownerId, OccurredAt = DateTimeOffset.UtcNow }
            );
            await db.SaveChangesAsync();

            var svc = new AnalyticsService(db);
            var result = await svc.GetMostReadGenresAsync(owner, from: null, to: null, ct: CancellationToken.None);

            // Excludes empty/whitespace, normalizes case and whitespace, sorts by count desc then genre asc (normalized)
            var simplified = result.Select(x => (Genre: x.Genre.Trim().ToLowerInvariant(), x.ReadCount)).ToList();
            simplified.Should().Equal(new List<(string Genre, int ReadCount)>
            {
                ("scifi", 3),
                ("fantasy", 2),
                ("mystery", 2),
                ("software", 1)
            });

            // Ensure no empty/whitespace genres present
            simplified.Any(x => string.IsNullOrWhiteSpace(x.Genre)).Should().BeFalse();
        }
        finally
        {
            SqliteInMemoryDb.Dispose(db, conn);
        }
    }
}


