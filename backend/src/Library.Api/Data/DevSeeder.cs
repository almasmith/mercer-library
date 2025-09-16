using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Library.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Library.Api.Data
{
    public static class DevSeeder
    {
        public static async Task SeedAsync(IServiceProvider services, CancellationToken ct)
        {
            var env = services.GetRequiredService<IHostEnvironment>();
            if (!env.IsDevelopment())
            {
                return;
            }

            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DevSeeder");

            await using var scope = services.CreateAsyncScope();
            var scopedProvider = scope.ServiceProvider;

            var db = scopedProvider.GetRequiredService<LibraryDbContext>();
            var userManager = scopedProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Ensure database is up to date for development
            await db.Database.MigrateAsync(ct);

            // 1) Ensure dev user exists
            const string devEmail = "test@example.com";
            const string devPassword = "Passw0rd!";

            var devUser = await userManager.FindByEmailAsync(devEmail);
            if (devUser is null)
            {
                devUser = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    Email = devEmail,
                    UserName = devEmail,
                    EmailConfirmed = true,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                var createResult = await userManager.CreateAsync(devUser, devPassword);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => $"{e.Code}:{e.Description}"));
                    logger.LogError("Failed creating dev user: {Errors}", errors);
                    throw new InvalidOperationException($"Failed creating dev user: {errors}");
                }
                logger.LogInformation("Created dev user {Email}", devEmail);
            }
            else
            {
                logger.LogInformation("Dev user {Email} already exists", devEmail);
            }

            // 2) Seed 5–10 books for the dev user (idempotent by Title + OwnerUserId)
            var now = DateTimeOffset.UtcNow;
            var sampleBooks = new List<(string Title, string Author, string Genre, DateTimeOffset Published, int Rating)>
            {
                ("The Silent Library", "A. Winters", "Mystery", now.AddYears(-12), 4),
                ("Echoes of Tomorrow", "L. Harper", "Sci-Fi", now.AddYears(-3), 5),
                ("Roots and Rivers", "M. Delgado", "Historical", now.AddYears(-28), 4),
                ("Saffron Skies", "N. Kapoor", "Romance", now.AddYears(-6), 3),
                ("Shards of Code", "J. Fowler", "Technology", now.AddYears(-1), 5),
                ("Whispers in Pine", "R. Tanaka", "Thriller", now.AddYears(-9), 4),
                ("Cinder & Snow", "E. Park", "Fantasy", now.AddYears(-16), 5),
                ("The Last Harvest", "T. O’Neill", "Drama", now.AddYears(-7), 4),

                // Additional Mystery (4 more)
                ("Midnight Margins", "C. Ellery", "Mystery", now.AddYears(-10).AddMonths(-2), 4),
                ("Case of the Vanished Key", "P. Moran", "Mystery", now.AddYears(-8), 5),
                ("Shadows at Dusk", "I. Romero", "Mystery", now.AddYears(-6), 3),
                ("A Quiet Alibi", "V. Chen", "Mystery", now.AddYears(-4).AddMonths(-5), 4),

                // Additional Sci-Fi (3 more)
                ("Orbit of Glass", "N. Flores", "Sci-Fi", now.AddYears(-5), 4),
                ("Quantum Drift", "R. Ames", "Sci-Fi", now.AddYears(-2), 5),
                ("Starlight Protocol", "J. Kwan", "Sci-Fi", now.AddYears(-1).AddMonths(-7), 4),

                // Additional Historical (1 more)
                ("Embers of Empire", "G. Kovacs", "Historical", now.AddYears(-31), 4),

                // Additional Romance (2 more)
                ("Lavender Letters", "S. Marino", "Romance", now.AddYears(-11), 3),
                ("Autumn Promises", "H. Nguyen", "Romance", now.AddYears(-3).AddMonths(-9), 4),

                // Additional Technology (5 more)
                ("Refactoring the Future", "T. Patel", "Technology", now.AddMonths(-18), 5),
                ("Distributed Dreams", "E. Santos", "Technology", now.AddMonths(-9), 4),
                ("The Bug Hunter's Diary", "K. Rivera", "Technology", now.AddMonths(-6), 4),
                ("Edge of Reliability", "B. Ortega", "Technology", now.AddMonths(-3), 5),
                ("Patterns in Motion", "D. Yamada", "Technology", now.AddMonths(-1), 5),

                // Additional Thriller (2 more)
                ("The Glass Cipher", "L. Serrano", "Thriller", now.AddYears(-13), 4),
                ("Under False Lights", "M. Kowalski", "Thriller", now.AddYears(-2).AddMonths(-2), 3),

                // Additional Fantasy (4 more)
                ("Ash and Azure", "R. Vance", "Fantasy", now.AddYears(-18), 5),
                ("Crown of Salt", "Y. Adler", "Fantasy", now.AddYears(-12), 4),
                ("Warden of Hollow Ways", "P. D’Souza", "Fantasy", now.AddYears(-9), 5),
                ("The Last Witch's Oath", "K. Beaumont", "Fantasy", now.AddYears(-5).AddMonths(-4), 4),

                // Additional Drama (1 more)
                ("Between Quiet Hours", "A. Rahman", "Drama", now.AddYears(-1).AddMonths(-11), 4)
            };

            var existingTitles = await db.Books
                .Where(b => b.OwnerUserId == devUser.Id)
                .Select(b => b.Title)
                .ToListAsync(ct);

            var booksToAdd = sampleBooks
                .Where(sb => !existingTitles.Contains(sb.Title))
                .Select(sb => new Book
                {
                    Id = Guid.NewGuid(),
                    Title = sb.Title,
                    Author = sb.Author,
                    Genre = sb.Genre,
                    PublishedDate = sb.Published,
                    Rating = sb.Rating,
                    OwnerUserId = devUser.Id,
                    CreatedAt = now,
                    UpdatedAt = now
                })
                .ToList();

            if (booksToAdd.Count > 0)
            {
                await db.Books.AddRangeAsync(booksToAdd, ct);
                await db.SaveChangesAsync(ct);
                logger.LogInformation("Inserted {Count} books for dev user", booksToAdd.Count);
            }
            else
            {
                logger.LogInformation("No new books to insert for dev user");
            }

            // Refresh the list of books from database
            var allBooks = await db.Books.Where(b => b.OwnerUserId == devUser.Id).ToListAsync(ct);

            // 3) Add 2–3 favorites (idempotent via composite PK)
            var favoriteCandidates = allBooks.Take(3).ToList();
            foreach (var favBook in favoriteCandidates)
            {
                var exists = await db.Favorites.AnyAsync(f => f.UserId == devUser.Id && f.BookId == favBook.Id, ct);
                if (!exists)
                {
                    await db.Favorites.AddAsync(new Favorite
                    {
                        UserId = devUser.Id,
                        BookId = favBook.Id,
                        CreatedAt = now
                    }, ct);
                }
            }
            await db.SaveChangesAsync(ct);

            // 4) Insert BookRead events across varied dates/genres (idempotent by exact tuple)
            if (allBooks.Count > 0)
            {
                var readsPlan = BuildReadsPlan(devUser.Id, allBooks);
                foreach (var planned in readsPlan)
                {
                    var exists = await db.BookReads.AnyAsync(br => br.UserId == planned.UserId && br.BookId == planned.BookId && br.OccurredAt == planned.OccurredAt, ct);
                    if (!exists)
                    {
                        await db.BookReads.AddAsync(new BookRead
                        {
                            Id = Guid.NewGuid(),
                            UserId = planned.UserId,
                            BookId = planned.BookId,
                            OccurredAt = planned.OccurredAt
                        }, ct);
                    }
                }
                await db.SaveChangesAsync(ct);
            }

            logger.LogInformation("Development seeding completed");
        }

        private static IEnumerable<(Guid UserId, Guid BookId, DateTimeOffset OccurredAt)> BuildReadsPlan(Guid userId, List<Book> books)
        {
            var now = DateTimeOffset.UtcNow;
            var offsets = new[] { -2, -7, -14, -21, -30, -45, -60, -75, -90 };

            var picks = books
                .OrderBy(b => b.Genre)
                .ThenBy(b => b.Title)
                .Take(5)
                .ToList();

            var plan = new List<(Guid, Guid, DateTimeOffset)>();
            for (var i = 0; i < picks.Count; i++)
            {
                var book = picks[i];
                // two reads per selected book spaced across time
                var dayOffsetA = offsets[(i * 2) % offsets.Length];
                var dayOffsetB = offsets[(i * 2 + 1) % offsets.Length];
                var dateA = new DateTimeOffset(now.AddDays(dayOffsetA).Date, TimeSpan.Zero).AddHours(10);
                var dateB = new DateTimeOffset(now.AddDays(dayOffsetB).Date, TimeSpan.Zero).AddHours(20);
                plan.Add((userId, book.Id, dateA));
                plan.Add((userId, book.Id, dateB));
            }

            return plan;
        }
    }
}


