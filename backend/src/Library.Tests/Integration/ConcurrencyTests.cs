using System;
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

public sealed class ConcurrencyTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public ConcurrencyTests(CustomWebApplicationFactory factory)
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
    public async Task Put_Allows_Without_IfMatch_LastWriteWins_And_Stale_IfMatch_Yields_412()
    {
        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var (email, password) = NewUserCreds();

        // Register and Login
        var registerResp = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password));
        registerResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var registerBody = await registerResp.Content.ReadFromJsonAsync<AuthResponse>(_json);
        registerBody.Should().NotBeNull();
        ApplyBearer(client, registerBody!.AccessToken);

        // Create a book
        var createReq = new CreateBookRequest(
            Title: "Concurrency Book",
            Author: "Author X",
            Genre: "Tech",
            PublishedDate: new DateTimeOffset(2021, 5, 20, 0, 0, 0, TimeSpan.Zero),
            Rating: 4);
        var createResp = await client.PostAsJsonAsync("/api/books", createReq);
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResp.Content.ReadFromJsonAsync<BookDto>(_json);
        created.Should().NotBeNull();

        // GET to capture current ETag
        var getResp = await client.GetAsync($"/api/books/{created!.Id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        getResp.Headers.ETag.Should().NotBeNull();
        var etag = getResp.Headers.ETag!.Tag;
        etag.Should().NotBeNullOrEmpty();

        // 1) PUT without If-Match should succeed (last-write-wins allowed)
        var updateNoIfMatch = new UpdateBookRequest(
            Title: "Updated Without IfMatch",
            Author: created.Author,
            Genre: created.Genre,
            PublishedDate: created.PublishedDate,
            Rating: 3);
        var putNoIfMatch = await client.PutAsJsonAsync($"/api/books/{created.Id}", updateNoIfMatch);
        putNoIfMatch.StatusCode.Should().Be(HttpStatusCode.OK);

        // 2) PUT with stale If-Match should yield 412
        var updateWithStale = new UpdateBookRequest(
            Title: "Should Fail",
            Author: created.Author,
            Genre: created.Genre,
            PublishedDate: created.PublishedDate,
            Rating: 5);
        var staleReq = new HttpRequestMessage(HttpMethod.Put, $"/api/books/{created.Id}")
        {
            Content = JsonContent.Create(updateWithStale, options: _json)
        };
        staleReq.Headers.TryAddWithoutValidation("If-Match", etag);
        var staleResp = await client.SendAsync(staleReq);
        staleResp.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
    }
}


