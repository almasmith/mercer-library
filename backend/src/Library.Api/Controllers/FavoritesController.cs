using System.Net.Mime;
using AutoMapper;
using Library.Api.Dtos;
using Library.Api.Infrastructure;
using Library.Api.Services.Books;
using Library.Api.Services.Favorites;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Library.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class FavoritesController : ControllerBase
{
    private readonly IFavoritesService _favorites;
    private readonly IMapper _mapper;

    public FavoritesController(IFavoritesService favorites, IMapper mapper)
    {
        _favorites = favorites;
        _mapper = mapper;
    }

    [HttpPost("/api/books/{id:guid}/favorite")]
    [Produces(MediaTypeNames.Application.Json)]
    [SwaggerOperation(Summary = "Favorite a book", Description = "Favorites a book that you own. Idempotent: returns 204 even if already favorited.")]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Favorited")] 
    [SwaggerResponse(StatusCodes.Status404NotFound, "Book not found or not owned")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Favorite([FromRoute] Guid id, CancellationToken ct)
    {
        var userId = UserContext.GetUserId(HttpContext);
        var ok = await _favorites.FavoriteAsync(userId, id, ct);
        if (!ok)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("/api/books/{id:guid}/favorite")]
    [SwaggerOperation(Summary = "Unfavorite a book", Description = "Removes a book from favorites. Idempotent: returns 204 even if already not favorited.")]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Unfavorited")] 
    [SwaggerResponse(StatusCodes.Status404NotFound, "Book not found or not owned")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Unfavorite([FromRoute] Guid id, CancellationToken ct)
    {
        var userId = UserContext.GetUserId(HttpContext);
        var ok = await _favorites.UnfavoriteAsync(userId, id, ct);
        if (!ok)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpGet("/api/favorites")]
    [Produces(MediaTypeNames.Application.Json)]
    [SwaggerOperation(Summary = "List favorite books", Description = "Returns a paged list of favorited books honoring filters, sort, and paging.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Favorites listed", typeof(PagedResult<BookDto>))]
    [ProducesResponseType(typeof(PagedResult<BookDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> List([FromQuery] BookListParameters p, CancellationToken ct)
    {
        var userId = UserContext.GetUserId(HttpContext);
        var result = await _favorites.ListAsync(userId, p, ct);
        var itemsDto = _mapper.Map<List<BookDto>>(result.Items);
        return Ok(new PagedResult<BookDto>(itemsDto, result.Page, result.PageSize, result.TotalItems, result.TotalPages));
    }
}


