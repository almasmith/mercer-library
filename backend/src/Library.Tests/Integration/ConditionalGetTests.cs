using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Library.Api.Dtos;
using Library.Api.Dtos.Auth;
using Library.Tests.Infrastructure;

namespace Library.Tests.Integration;

public sealed class ConditionalGetTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public ConditionalGetTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static (string Email, string Password) NewUserCreds()
    {
        return ($"user_{Guid.NewGuid():N}@example.com", "Passw0rd!");
    }

    private static void ApplyBearer(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    [Fact]
    public async Task Book_GetById_Emits_ETag_IfNoneMatch_Yields_304_And_Changes_After_Update()
    {
        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var (email, password) = NewUserCreds();

        // Register and login
        var registerResp = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password));
        registerResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var registerBody = await registerResp.Content.ReadFromJsonAsync<AuthResponse>(_json);
        registerBody.Should().NotBeNull();
        ApplyBearer(client, registerBody!.AccessToken);

        // Create a book
        var createReq = new CreateBookRequest("ETag Book", "Author", "SciFi", new DateTimeOffset(2022, 5, 1, 0, 0, 0, TimeSpan.Zero), 4);
        var createResp = await client.PostAsJsonAsync("/api/books", createReq);
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResp.Content.ReadFromJsonAsync<BookDto>(_json);
        created.Should().NotBeNull();

        // Initial GET should have ETag
        var getResp = await client.GetAsync($"/api/books/{created!.Id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        getResp.Headers.ETag.Should().NotBeNull();
        var etag = getResp.Headers.ETag!.Tag;
        etag.Should().NotBeNullOrWhiteSpace();

        // GET with If-None-Match should yield 304
        var condReq = new HttpRequestMessage(HttpMethod.Get, $"/api/books/{created.Id}");
        condReq.Headers.TryAddWithoutValidation("If-None-Match", etag);
        var condResp = await client.SendAsync(condReq);
        condResp.StatusCode.Should().Be(HttpStatusCode.NotModified);
        condResp.Headers.ETag.Should().NotBeNull();
        condResp.Headers.ETag!.Tag.Should().Be(etag);

        // Update the book to change RowVersion/ETag
        var updateReq = new UpdateBookRequest("New Title", created.Author, created.Genre, created.PublishedDate, created.Rating);
        var putResp = await client.PutAsJsonAsync($"/api/books/{created.Id}", updateReq);
        putResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // GET with the old ETag should now return 200 with a new ETag
        var staleReq = new HttpRequestMessage(HttpMethod.Get, $"/api/books/{created.Id}");
        staleReq.Headers.TryAddWithoutValidation("If-None-Match", etag);
        var staleResp = await client.SendAsync(staleReq);
        staleResp.StatusCode.Should().Be(HttpStatusCode.OK);
        staleResp.Headers.ETag.Should().NotBeNull();
        staleResp.Headers.ETag!.Tag.Should().NotBe(etag);
    }

    [Fact]
    public async Task Stats_ETag_IfNoneMatch_304_And_Bumps_On_Create_Favorite_Read()
    {
        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var (email, password) = NewUserCreds();

        // Register and login
        var registerResp = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password));
        registerResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var registerBody = await registerResp.Content.ReadFromJsonAsync<AuthResponse>(_json);
        registerBody.Should().NotBeNull();
        ApplyBearer(client, registerBody!.AccessToken);

        // First request gets ETag
        var stats1 = await client.GetAsync("/api/books/stats");
        stats1.StatusCode.Should().Be(HttpStatusCode.OK);
        stats1.Headers.ETag.Should().NotBeNull();
        var etag1 = stats1.Headers.ETag!.Tag;

        // Repeat with If-None-Match -> 304
        var statsCond = new HttpRequestMessage(HttpMethod.Get, "/api/books/stats");
        statsCond.Headers.TryAddWithoutValidation("If-None-Match", etag1!);
        var stats304 = await client.SendAsync(statsCond);
        stats304.StatusCode.Should().Be(HttpStatusCode.NotModified);
        stats304.Headers.ETag.Should().NotBeNull();
        stats304.Headers.ETag!.Tag.Should().Be(etag1);

        // Create a book (mutates stats version)
        var bReq = new CreateBookRequest("S", "A", "SciFi", DateTimeOffset.UtcNow, 5);
        var bResp = await client.PostAsJsonAsync("/api/books", bReq);
        bResp.StatusCode.Should().Be(HttpStatusCode.Created);

        // Stats with old ETag should now be 200 and new ETag
        var statsAfterCreate = new HttpRequestMessage(HttpMethod.Get, "/api/books/stats");
        statsAfterCreate.Headers.TryAddWithoutValidation("If-None-Match", etag1!);
        var stats2 = await client.SendAsync(statsAfterCreate);
        stats2.StatusCode.Should().Be(HttpStatusCode.OK);
        stats2.Headers.ETag.Should().NotBeNull();
        var etag2 = stats2.Headers.ETag!.Tag;
        etag2.Should().NotBe(etag1);

        // Favorite the book (further mutation)
        var book = await bResp.Content.ReadFromJsonAsync<BookDto>(_json);
        var favResp = await client.PostAsync($"/api/books/{book!.Id}/favorite", null);
        favResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var statsAfterFav = new HttpRequestMessage(HttpMethod.Get, "/api/books/stats");
        statsAfterFav.Headers.TryAddWithoutValidation("If-None-Match", etag2!);
        var stats3 = await client.SendAsync(statsAfterFav);
        stats3.StatusCode.Should().Be(HttpStatusCode.OK);
        var etag3 = stats3.Headers.ETag!.Tag;
        etag3.Should().NotBe(etag2);

        // Record a read (another mutation)
        var readResp = await client.PostAsync($"/api/books/{book!.Id}/read", null);
        readResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var statsAfterRead = new HttpRequestMessage(HttpMethod.Get, "/api/books/stats");
        statsAfterRead.Headers.TryAddWithoutValidation("If-None-Match", etag3!);
        var stats4 = await client.SendAsync(statsAfterRead);
        stats4.StatusCode.Should().Be(HttpStatusCode.OK);
        var etag4 = stats4.Headers.ETag!.Tag;
        etag4.Should().NotBe(etag3);
    }
}
