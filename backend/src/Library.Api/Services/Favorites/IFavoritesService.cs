using System;
using System.Threading;
using System.Threading.Tasks;
using Library.Api.Domain;
using Library.Api.Dtos;
using Library.Api.Services.Books;

namespace Library.Api.Services.Favorites
{
    public interface IFavoritesService
    {
        Task<bool> FavoriteAsync(Guid userId, Guid bookId, CancellationToken ct);
        Task<bool> UnfavoriteAsync(Guid userId, Guid bookId, CancellationToken ct);
        Task<PagedResult<Book>> ListAsync(Guid userId, BookListParameters p, CancellationToken ct);
        Task<bool> IsFavoritedAsync(Guid userId, Guid bookId, CancellationToken ct);
    }
}


