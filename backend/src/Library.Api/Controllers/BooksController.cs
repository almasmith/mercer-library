using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Library.Api.Domain;
using Library.Api.Dtos;
using Library.Api.Data;
using Library.Api.Infrastructure;
using Library.Api.Services;
using Library.Api.Services.Books;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;

namespace Library.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class BooksController : ControllerBase
{
    private readonly IBookService _bookService;
    private readonly IMapper _mapper;
    private readonly ILogger<BooksController> _logger;
    private readonly LibraryDbContext _db;

    public BooksController(IBookService bookService, IMapper mapper, ILogger<BooksController> logger, LibraryDbContext db)
    {
        _bookService = bookService;
        _mapper = mapper;
        _logger = logger;
        _db = db;
    }

    [HttpGet]
    [Produces(MediaTypeNames.Application.Json)]
    [SwaggerOperation(Summary = "List books", Description = "Lists books for the authenticated user with filtering, sorting, and pagination.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Books listed", typeof(PagedResult<BookDto>))]
    public async Task<IActionResult> List([FromQuery] BookListParameters parameters, CancellationToken ct)
    {
        var ownerUserId = UserContext.GetUserId(HttpContext);
        var result = await _bookService.ListAsync(ownerUserId, parameters, ct);

        var dtoItems = _mapper.Map<List<BookDto>>(result.Items);
        var dto = new PagedResult<BookDto>(dtoItems, result.Page, result.PageSize, result.TotalItems, result.TotalPages);
        return Ok(dto);
    }

    [HttpGet("{id:guid}")]
    [Produces(MediaTypeNames.Application.Json)]
    [SwaggerOperation(Summary = "Get a book", Description = "Gets a book by id for the authenticated user.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Book found", typeof(BookDto))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Book not found")]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var ownerUserId = UserContext.GetUserId(HttpContext);
        var book = await _bookService.GetByIdAsync(ownerUserId, id, ct);
        if (book is null)
        {
            return NotFound();
        }

        var etag = ETagHelper.ToStrongEtag(book.RowVersion);

        if (ETagHelper.IfNoneMatchSatisfied(Request, etag))
        {
            Response.Headers[Microsoft.Net.Http.Headers.HeaderNames.ETag] = etag;
            return StatusCode(StatusCodes.Status304NotModified);
        }

        Response.Headers[Microsoft.Net.Http.Headers.HeaderNames.ETag] = etag;
        var dto = _mapper.Map<BookDto>(book);
        return Ok(dto);
    }

    [HttpPost]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(BookDto), StatusCodes.Status201Created)]
    [SwaggerOperation(Summary = "Create a book", Description = "Creates a new book for the authenticated user.")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Validation errors", typeof(ValidationProblemDetails))]
    public async Task<IActionResult> Create([FromBody] CreateBookRequest request, CancellationToken ct)
    {
        var ownerUserId = UserContext.GetUserId(HttpContext);
        var book = _mapper.Map<Book>(request);
        book.OwnerUserId = ownerUserId;

        var created = await _bookService.CreateAsync(ownerUserId, book, ct);
        var dto = _mapper.Map<BookDto>(created);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [SwaggerOperation(Summary = "Update a book", Description = "Updates an existing book by id for the authenticated user.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Book updated", typeof(BookDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Validation errors", typeof(ValidationProblemDetails))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Book not found")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateBookRequest request, CancellationToken ct)
    {
        var ownerUserId = UserContext.GetUserId(HttpContext);

        var updated = await _bookService.UpdateAsync(ownerUserId, id, b =>
        {
            b.Title = request.Title;
            b.Author = request.Author;
            b.Genre = request.Genre;
            b.PublishedDate = request.PublishedDate;
            b.Rating = request.Rating;
            b.UpdatedAt = DateTimeOffset.UtcNow;
        }, ct);

        if (updated is null)
        {
            return NotFound();
        }

        var dto = _mapper.Map<BookDto>(updated);
        return Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    [SwaggerOperation(Summary = "Delete a book", Description = "Deletes a book by id for the authenticated user.")]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Book deleted")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Book not found")]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
    {
        var ownerUserId = UserContext.GetUserId(HttpContext);
        var deleted = await _bookService.DeleteAsync(ownerUserId, id, ct);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpGet("stats")]
    [Produces(MediaTypeNames.Application.Json)]
    [SwaggerOperation(Summary = "Book stats by genre", Description = "Returns genre counts for the authenticated user. Excludes empty/null genres. Case-insensitive grouping with whitespace trimming.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Stats calculated", typeof(IEnumerable<BookGenreCountDto>))]
    public async Task<IActionResult> Stats(CancellationToken ct)
    {
        var ownerUserId = UserContext.GetUserId(HttpContext);

        var genres = await _db.Books
            .AsNoTracking()
            .Where(b => b.OwnerUserId == ownerUserId)
            .Select(b => b.Genre)
            .ToListAsync(ct);

        var results = genres
            .Select(g => (g ?? string.Empty).Trim())
            .Where(s => s.Length > 0)
            .GroupBy(s => s, StringComparer.OrdinalIgnoreCase)
            .Select(g => new
            {
                Canonical = g.OrderBy(s => s, StringComparer.Ordinal).First(),
                Count = g.Count(),
                NormalizedLower = g.Key.ToLowerInvariant()
            })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.NormalizedLower, StringComparer.Ordinal)
            .Select(x => new BookGenreCountDto(x.Canonical, x.Count))
            .ToList();

        return Ok(results);
    }
}


