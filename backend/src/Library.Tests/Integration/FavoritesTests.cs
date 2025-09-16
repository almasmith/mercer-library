using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Library.Api.Dtos;
using Library.Api.Dtos.Auth;
using Library.Tests.Infrastructure;

namespace Library.Tests.Integration;

public sealed class FavoritesTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public FavoritesTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static (string Email, string Password) NewUserCreds()
    {
        return ($"user_{Guid.NewGuid():N}@example.com", "Passw0rd!");
    }

    private static void ApplyBearer(System.Net.Http.HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    [Fact]
    public async Task Favorite_Unfavorite_Are_Idempotent_And_List_Honors_Filters_Sort_Paging()
    {
        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var (email, password) = NewUserCreds();

        // Register and authenticate
        var registerResp = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password));
        registerResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var registerBody = await registerResp.Content.ReadFromJsonAsync<AuthResponse>(_json);
        registerBody.Should().NotBeNull();
        ApplyBearer(client, registerBody!.AccessToken);

        // Create books
        var b1Req = new CreateBookRequest("Alpha", "Author X", "SciFi", new DateTimeOffset(2020, 1, 10, 0, 0, 0, TimeSpan.Zero), 5);
        var b2Req = new CreateBookRequest("Beta", "Author Y", "Fantasy", new DateTimeOffset(2019, 5, 1, 0, 0, 0, TimeSpan.Zero), 4);
        var b3Req = new CreateBookRequest("Gamma", "Author Z", "SciFi", new DateTimeOffset(2021, 3, 15, 0, 0, 0, TimeSpan.Zero), 2);

        var b1Resp = await client.PostAsJsonAsync("/api/books", b1Req);
        b1Resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var b1 = await b1Resp.Content.ReadFromJsonAsync<BookDto>(_json);
        b1.Should().NotBeNull();

        var b2Resp = await client.PostAsJsonAsync("/api/books", b2Req);
        b2Resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var b2 = await b2Resp.Content.ReadFromJsonAsync<BookDto>(_json);
        b2.Should().NotBeNull();

        var b3Resp = await client.PostAsJsonAsync("/api/books", b3Req);
        b3Resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var b3 = await b3Resp.Content.ReadFromJsonAsync<BookDto>(_json);
        b3.Should().NotBeNull();

        // Idempotent favorite
        var fav1 = await client.PostAsync($"/api/books/{b1!.Id}/favorite", null);
        fav1.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var fav1Repeat = await client.PostAsync($"/api/books/{b1.Id}/favorite", null);
        fav1Repeat.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var fav2 = await client.PostAsync($"/api/books/{b2!.Id}/favorite", null);
        fav2.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Idempotent unfavorite
        var unfav3 = await client.DeleteAsync($"/api/books/{b3!.Id}/favorite");
        unfav3.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var unfav3Repeat = await client.DeleteAsync($"/api/books/{b3.Id}/favorite");
        unfav3Repeat.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // List favorites default
        var listDefaultResp = await client.GetAsync("/api/favorites");
        listDefaultResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var listDefault = await listDefaultResp.Content.ReadFromJsonAsync<PagedResult<BookDto>>(_json);
        listDefault.Should().NotBeNull();
        listDefault!.Items.Select(x => x.Id).Should().BeEquivalentTo(new[] { b1.Id, b2.Id }, o => o.WithoutStrictOrdering());

        // Filters: genre = scifi -> only b1
        var listGenreResp = await client.GetAsync("/api/favorites?genre=SciFi");
        listGenreResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var listGenre = await listGenreResp.Content.ReadFromJsonAsync<PagedResult<BookDto>>(_json);
        listGenre.Should().NotBeNull();
        listGenre!.Items.Should().OnlyContain(x => (x.Genre ?? string.Empty).Trim().Equals("scifi", StringComparison.OrdinalIgnoreCase));

        // Sort by title asc
        var listSortResp = await client.GetAsync("/api/favorites?sortBy=title&sortOrder=asc");
        listSortResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var listSort = await listSortResp.Content.ReadFromJsonAsync<PagedResult<BookDto>>(_json);
        listSort.Should().NotBeNull();
        var titles = listSort!.Items.Select(i => i.Title).ToList();
        titles.Should().BeInAscendingOrder();

        // Paging: pageSize=1 -> 1 item; page=2 -> next item
        var page1Resp = await client.GetAsync("/api/favorites?pageSize=1&page=1&sortBy=title&sortOrder=asc");
        page1Resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var page1 = await page1Resp.Content.ReadFromJsonAsync<PagedResult<BookDto>>(_json);
        page1.Should().NotBeNull();
        page1!.Items.Should().HaveCount(1);

        var page2Resp = await client.GetAsync("/api/favorites?pageSize=1&page=2&sortBy=title&sortOrder=asc");
        page2Resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var page2 = await page2Resp.Content.ReadFromJsonAsync<PagedResult<BookDto>>(_json);
        page2.Should().NotBeNull();
        page2!.Items.Should().HaveCount(1);
        // Combined two pages should equal the set of favorites
        var combined = page1.Items.Concat(page2.Items).Select(x => x.Id).ToList();
        combined.Should().BeEquivalentTo(new[] { b1.Id, b2.Id });
    }
}
