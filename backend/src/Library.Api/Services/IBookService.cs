using System;
using System.Threading;
using System.Threading.Tasks;
using Library.Api.Domain;
using Library.Api.Dtos;
using Library.Api.Services.Books;

namespace Library.Api.Services
{
    public interface IBookService
    {
        Task<Book> CreateAsync(Guid ownerUserId, Book book, CancellationToken ct);
        Task<Book?> GetByIdAsync(Guid ownerUserId, Guid id, CancellationToken ct);
        Task<PagedResult<Book>> ListAsync(Guid ownerUserId, BookListParameters p, CancellationToken ct);
        Task<Book?> UpdateAsync(Guid ownerUserId, Guid id, Action<Book> applyUpdates, CancellationToken ct);
        Task<bool> DeleteAsync(Guid ownerUserId, Guid id, CancellationToken ct);
    }
}


