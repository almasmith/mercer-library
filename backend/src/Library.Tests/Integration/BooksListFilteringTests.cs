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

public sealed class BooksListFilteringTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public BooksListFilteringTests(CustomWebApplicationFactory factory)
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
    public async Task GenreFilter_Partial_And_CaseInsensitive_Works()
    {
        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var (email, password) = NewUserCreds();

        // Register and authenticate
        var registerResp = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password));
        registerResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var registerBody = await registerResp.Content.ReadFromJsonAsync<AuthResponse>(_json);
        registerBody.Should().NotBeNull();
        ApplyBearer(client, registerBody!.AccessToken);

        // Create books with genre variants
        var now = DateTimeOffset.UtcNow;
        var b1Resp = await client.PostAsJsonAsync("/api/books", new CreateBookRequest("F1", "Auth", "Fantasy", now, 5));
        b1Resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var b1 = await b1Resp.Content.ReadFromJsonAsync<BookDto>(_json);
        b1.Should().NotBeNull();

        var b2Resp = await client.PostAsJsonAsync("/api/books", new CreateBookRequest("F2", "Auth", "FANTASY Epics", now, 4));
        b2Resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var b2 = await b2Resp.Content.ReadFromJsonAsync<BookDto>(_json);
        b2.Should().NotBeNull();

        var b3Resp = await client.PostAsJsonAsync("/api/books", new CreateBookRequest("M1", "Auth", "Mystery", now, 3));
        b3Resp.StatusCode.Should().Be(HttpStatusCode.Created);

        // fan -> both fantasy entries
        var listFanResp = await client.GetAsync("/api/books?genre=fan");
        listFanResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var listFan = await listFanResp.Content.ReadFromJsonAsync<PagedResult<BookDto>>(_json);
        listFan.Should().NotBeNull();
        listFan!.Items.Select(x => x.Id).Should().BeEquivalentTo(new[] { b1!.Id, b2!.Id }, o => o.WithoutStrictOrdering());
        listFan.TotalItems.Should().Be(2);
        listFan.Page.Should().Be(1);

        // FAN -> case-insensitive match, still both
        var listFANResp = await client.GetAsync("/api/books?genre=FAN");
        listFANResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var listFAN = await listFANResp.Content.ReadFromJsonAsync<PagedResult<BookDto>>(_json);
        listFAN.Should().NotBeNull();
        listFAN!.Items.Select(x => x.Id).Should().BeEquivalentTo(new[] { b1!.Id, b2!.Id }, o => o.WithoutStrictOrdering());
        listFAN.TotalItems.Should().Be(2);

        // Negative control: no matches
        var listNoneResp = await client.GetAsync("/api/books?genre=zzz");
        listNoneResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var listNone = await listNoneResp.Content.ReadFromJsonAsync<PagedResult<BookDto>>(_json);
        listNone.Should().NotBeNull();
        listNone!.Items.Should().BeEmpty();
        listNone.TotalItems.Should().Be(0);
    }
}


