using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Library.Api.Data;
using Library.Api.Domain;
using Library.Api.Dtos;
using Library.Api.Services.Books;
using Library.Api.Hubs;
using Microsoft.EntityFrameworkCore;

namespace Library.Api.Services.Favorites
{
    public sealed class FavoritesService : IFavoritesService
    {
        private readonly LibraryDbContext _db;
        private readonly IRealtimePublisher _publisher;

        public FavoritesService(LibraryDbContext db)
        {
            _db = db;
            _publisher = null!; // optional for tests; DI uses the 2-arg ctor
        }

        public FavoritesService(LibraryDbContext db, IRealtimePublisher publisher)
        {
            _db = db;
            _publisher = publisher;
        }

        public async Task<bool> FavoriteAsync(Guid userId, Guid bookId, CancellationToken ct)
        {
            // Verify ownership
            var book = await _db.Books
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == bookId && b.OwnerUserId == userId, ct);
            if (book == null)
            {
                return false; // controller will translate to 404
            }

            // Idempotency pre-check to avoid EF tracking conflicts within the same DbContext
            var alreadyFavorited = await _db.Favorites
                .AsNoTracking()
                .AnyAsync(f => f.UserId == userId && f.BookId == bookId, ct);
            if (alreadyFavorited)
            {
                if (_publisher != null)
                {
                    await _publisher.BookFavorited(userId, bookId, ct);
                }
                return true;
            }

            // Try to insert; treat duplicates as success for idempotency
            var favorite = new Favorite
            {
                UserId = userId,
                BookId = bookId,
                CreatedAt = DateTimeOffset.UtcNow
            };

            try
            {
                _db.Favorites.Add(favorite);
                await _db.SaveChangesAsync(ct);
                if (_publisher != null)
                {
                    await _publisher.BookFavorited(userId, bookId, ct);
                }
                return true;
            }
            catch (DbUpdateException)
            {
                // Likely PK conflict due to existing favorite. Ensure it exists and return true.
                var exists = await _db.Favorites.AnyAsync(f => f.UserId == userId && f.BookId == bookId, ct);
                if (exists && _publisher != null)
                {
                    // Idempotent case: still emit event to reflect current state if needed by UI
                    await _publisher.BookFavorited(userId, bookId, ct);
                }
                return exists;
            }
        }

        public async Task<bool> UnfavoriteAsync(Guid userId, Guid bookId, CancellationToken ct)
        {
            // Verify ownership
            var book = await _db.Books
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == bookId && b.OwnerUserId == userId, ct);
            if (book == null)
            {
                return false; // controller will translate to 404
            }

            // Delete if exists; idempotent
            var deleted = await _db.Favorites
                .Where(f => f.UserId == userId && f.BookId == bookId)
                .ExecuteDeleteAsync(ct);

            // If not found, still success (idempotent)
            if (_publisher != null)
            {
                await _publisher.BookUnfavorited(userId, bookId, ct);
            }
            return true;
        }

        public async Task<PagedResult<Book>> ListAsync(Guid userId, BookListParameters p, CancellationToken ct)
        {
            // Join favorites with books, ensuring ownership
            var query = _db.Favorites
                .AsNoTracking()
                .Where(f => f.UserId == userId)
                .Join(
                    _db.Books.AsNoTracking().Where(b => b.OwnerUserId == userId),
                    f => f.BookId,
                    b => b.Id,
                    (f, b) => b);

            // Filters (mirror BookService)
            if (!string.IsNullOrWhiteSpace(p.Genre))
            {
                var genre = p.Genre.Trim();
                query = query.Where(b => b.Genre != null && b.Genre.ToLower() == genre.ToLower());
            }

            if (p.MinRating.HasValue)
            {
                query = query.Where(b => b.Rating >= p.MinRating.Value);
            }

            if (p.MaxRating.HasValue)
            {
                query = query.Where(b => b.Rating <= p.MaxRating.Value);
            }

            if (p.PublishedFrom.HasValue)
            {
                query = query.Where(b => b.PublishedDate >= p.PublishedFrom.Value);
            }

            if (p.PublishedTo.HasValue)
            {
                query = query.Where(b => b.PublishedDate <= p.PublishedTo.Value);
            }

            if (!string.IsNullOrWhiteSpace(p.Search))
            {
                var search = p.Search.Trim();
                query = query.Where(b => b.Title.ToLower().Contains(search.ToLower()) || b.Author.ToLower().Contains(search.ToLower()));
            }

            // Sorting
            var sortBy = (p.SortBy ?? string.Empty).Trim().ToLowerInvariant();
            var sortOrder = (p.SortOrder ?? "desc").Trim().ToLowerInvariant();
            var descending = sortOrder != "asc";

            Expression<Func<Book, object>> orderBy = sortBy switch
            {
                "title" => b => b.Title,
                "author" => b => b.Author,
                "genre" => b => b.Genre,
                "rating" => b => b.Rating,
                "createdat" => b => b.CreatedAt,
                // default
                _ => b => b.PublishedDate
            };

            // Paging inputs
            var page = p.Page < 1 ? 1 : p.Page;
            var pageSize = p.PageSize < 1 ? 1 : p.PageSize > 100 ? 100 : p.PageSize;

            // SQLite workaround for DateTimeOffset ordering, same as BookService
            var isDateSort = string.IsNullOrEmpty(sortBy) || sortBy == "createdat" || sortBy == "publisheddate";
            if (isDateSort && _db.Database.IsSqlite())
            {
                var totalItems = await query.LongCountAsync(ct);
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var allItems = await query.ToListAsync(ct);
                Func<Book, DateTimeOffset> dateSelector = sortBy == "createdat" ? b => b.CreatedAt : b => b.PublishedDate;
                var ordered = descending ? allItems.OrderByDescending(dateSelector) : allItems.OrderBy(dateSelector);
                var items = ordered
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return new PagedResult<Book>(items, page, pageSize, totalItems, totalPages);
            }
            else
            {
                query = descending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);

                var totalItems = await query.LongCountAsync(ct);
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var items = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(ct);

                return new PagedResult<Book>(items, page, pageSize, totalItems, totalPages);
            }
        }

        public async Task<bool> IsFavoritedAsync(Guid userId, Guid bookId, CancellationToken ct)
        {
            return await _db.Favorites.AsNoTracking().AnyAsync(f => f.UserId == userId && f.BookId == bookId, ct);
        }
    }
}


