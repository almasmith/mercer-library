using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Library.Api.Data;
using Library.Api.Domain;
using Library.Api.Dtos;
using Library.Api.Services.Books;
using Microsoft.EntityFrameworkCore;

namespace Library.Api.Services
{
    public sealed class BookService : IBookService
    {
        private readonly LibraryDbContext _db;

        public BookService(LibraryDbContext db)
        {
            _db = db;
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

            // RowVersion is a concurrency token handled in other tasks; keep as-is here

            await _db.Books.AddAsync(book, ct);
            await _db.SaveChangesAsync(ct);
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

            // SQLite does not support ORDER BY DateTimeOffset. Fallback to in-memory ordering for date sorts on SQLite.
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

        public async Task<Book?> UpdateAsync(Guid ownerUserId, Guid id, Action<Book> applyUpdates, CancellationToken ct)
        {
            var book = await _db.Books.FirstOrDefaultAsync(b => b.OwnerUserId == ownerUserId && b.Id == id, ct);
            if (book == null)
            {
                return null;
            }

            applyUpdates(book);
            book.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(ct);
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
            return true;
        }
    }
}


