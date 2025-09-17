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

namespace Library.Api.Services
{
    public sealed class BookService : IBookService
    {
        private readonly LibraryDbContext _db;
        private readonly Library.Api.Services.Stats.IStatsVersionService _stats;
        private readonly IRealtimePublisher _publisher;

        public BookService(LibraryDbContext db)
        {
            _db = db;
            _stats = null!; // will be set via DI constructor overload
            _publisher = null!; // optional for tests; DI uses the 3-arg ctor
        }

        public BookService(LibraryDbContext db, Library.Api.Services.Stats.IStatsVersionService stats)
        {
            _db = db;
            _stats = stats;
            _publisher = null!; // optional for tests; DI uses the 3-arg ctor
        }

        public BookService(LibraryDbContext db, Library.Api.Services.Stats.IStatsVersionService stats, IRealtimePublisher publisher)
        {
            _db = db;
            _stats = stats;
            _publisher = publisher;
        }

        public async Task<Book> CreateAsync(Guid ownerUserId, Book book, CancellationToken ct)
        {
            var utcNow = DateTimeOffset.UtcNow;

            if (book.Id == Guid.Empty)
            {
                book.Id = Guid.NewGuid();
            }

            book.OwnerUserId = ownerUserId;
            book.CreatedAt = utcNow;
            book.UpdatedAt = utcNow;
            // Initialize RowVersion to a random value to serve as an ETag across providers
            book.RowVersion = Guid.NewGuid().ToByteArray();

            await _db.Books.AddAsync(book, ct);
            await _db.SaveChangesAsync(ct);
            // Bump stats for the owner
            if (_stats != null)
            {
                await _stats.BumpAsync(ownerUserId, ct);
            }

            // Publish events: created + statsUpdated
            if (_publisher != null)
            {
                var payload = new BookDto(
                    book.Id,
                    book.Title,
                    book.Author,
                    book.Genre ?? string.Empty,
                    book.PublishedDate,
                    book.Rating,
                    book.CreatedAt,
                    book.UpdatedAt);
                await _publisher.BookCreated(ownerUserId, payload, ct);
                await _publisher.StatsUpdated(ownerUserId, ct);
            }
            return book;
        }

        public async Task<Book?> GetByIdAsync(Guid ownerUserId, Guid id, CancellationToken ct)
        {
            return await _db.Books
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.OwnerUserId == ownerUserId && b.Id == id, ct);
        }

        public async Task<PagedResult<Book>> ListAsync(Guid ownerUserId, BookListParameters p, CancellationToken ct)
        {
            var query = _db.Books
                .AsNoTracking()
                .Where(b => b.OwnerUserId == ownerUserId);

            // Filters
            var usingSqlite = _db.Database.IsSqlite();
            if (!string.IsNullOrWhiteSpace(p.Genre))
            {
                var g = p.Genre.Trim();
                if (g.Length > 0)
                {
                    query = query.Where(b => b.Genre != null && b.Genre.ToLower().Contains(g.ToLower()));
                }
            }

            if (p.MinRating.HasValue)
            {
                query = query.Where(b => b.Rating >= p.MinRating.Value);
            }

            if (p.MaxRating.HasValue)
            {
                query = query.Where(b => b.Rating <= p.MaxRating.Value);
            }

            if (p.PublishedFrom.HasValue && !usingSqlite)
            {
                query = query.Where(b => b.PublishedDate >= p.PublishedFrom.Value);
            }

            if (p.PublishedTo.HasValue && !usingSqlite)
            {
                query = query.Where(b => b.PublishedDate <= p.PublishedTo.Value);
            }

            if (!string.IsNullOrWhiteSpace(p.Search))
            {
                var search = p.Search.Trim();
                query = query.Where(b =>
                    b.Title.ToLower().Contains(search.ToLower()) ||
                    b.Author.ToLower().Contains(search.ToLower()));
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

            // SQLite does not support ORDER BY or filtering with DateTimeOffset. Fallback to in-memory for date sorts/filters on SQLite.
            var isDateSort = string.IsNullOrEmpty(sortBy) || sortBy == "createdat" || sortBy == "publisheddate";
            var hasDateFilter = p.PublishedFrom.HasValue || p.PublishedTo.HasValue;
            if ((isDateSort || hasDateFilter) && usingSqlite)
            {
                var allItems = await query.ToListAsync(ct);
                // Apply date filters client-side when using SQLite
                if (p.PublishedFrom.HasValue)
                {
                    allItems = allItems.Where(b => b.PublishedDate >= p.PublishedFrom.Value).ToList();
                }
                if (p.PublishedTo.HasValue)
                {
                    allItems = allItems.Where(b => b.PublishedDate <= p.PublishedTo.Value).ToList();
                }

                var totalItems = (long)allItems.Count;
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var itemsQueryable = allItems.AsQueryable();
                itemsQueryable = descending ? itemsQueryable.OrderByDescending(orderBy) : itemsQueryable.OrderBy(orderBy);
                var items = itemsQueryable
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

        public async Task<Book?> UpdateAsync(Guid ownerUserId, Guid id, Action<Book> applyUpdates, CancellationToken ct)
        {
            var book = await _db.Books.FirstOrDefaultAsync(b => b.OwnerUserId == ownerUserId && b.Id == id, ct);
            if (book == null)
            {
                return null;
            }

            applyUpdates(book);
            book.UpdatedAt = DateTimeOffset.UtcNow;
            // Bump RowVersion to indicate a new representation (strong ETag)
            book.RowVersion = Guid.NewGuid().ToByteArray();

            await _db.SaveChangesAsync(ct);
            if (_stats != null)
            {
                await _stats.BumpAsync(ownerUserId, ct);
            }
            if (_publisher != null)
            {
                var payload = new BookDto(
                    book.Id,
                    book.Title,
                    book.Author,
                    book.Genre ?? string.Empty,
                    book.PublishedDate,
                    book.Rating,
                    book.CreatedAt,
                    book.UpdatedAt);
                await _publisher.BookUpdated(ownerUserId, payload, ct);
                await _publisher.StatsUpdated(ownerUserId, ct);
            }
            return book;
        }

        public async Task<bool> DeleteAsync(Guid ownerUserId, Guid id, CancellationToken ct)
        {
            var book = await _db.Books.FirstOrDefaultAsync(b => b.OwnerUserId == ownerUserId && b.Id == id, ct);
            if (book == null)
            {
                return false;
            }

            _db.Books.Remove(book);
            await _db.SaveChangesAsync(ct);
            if (_stats != null)
            {
                await _stats.BumpAsync(ownerUserId, ct);
            }
            if (_publisher != null)
            {
                await _publisher.BookDeleted(ownerUserId, id, ct);
                await _publisher.StatsUpdated(ownerUserId, ct);
            }
            return true;
        }
    }
}


