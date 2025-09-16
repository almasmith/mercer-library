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

public sealed class BooksCrudAndListTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public BooksCrudAndListTests(CustomWebApplicationFactory factory)
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
    public async Task Books_CRUD_And_List_Defaults_Work()
    {
        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var (email, password) = NewUserCreds();

        // Register and Login
        var registerResp = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password));
        registerResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var registerBody = await registerResp.Content.ReadFromJsonAsync<AuthResponse>(_json);
        registerBody.Should().NotBeNull();
        ApplyBearer(client, registerBody!.AccessToken);

        // 1) Create
        var createReq = new CreateBookRequest(
            Title: "Test Book",
            Author: "Author A",
            Genre: "SciFi",
            PublishedDate: new DateTimeOffset(2020, 1, 10, 0, 0, 0, TimeSpan.Zero),
            Rating: 5);

        var createResp = await client.PostAsJsonAsync("/api/books", createReq);
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResp.Content.ReadFromJsonAsync<BookDto>(_json);
        created.Should().NotBeNull();
        created!.Title.Should().Be("Test Book");
        created.Author.Should().Be("Author A");
        created.Genre.Should().Be("SciFi");
        created.Rating.Should().Be(5);
        created.PublishedDate.Should().Be(createReq.PublishedDate);

        // 2) Get
        var getResp = await client.GetAsync($"/api/books/{created.Id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var gotten = await getResp.Content.ReadFromJsonAsync<BookDto>(_json);
        gotten.Should().NotBeNull();
        gotten!.Id.Should().Be(created.Id);
        gotten.Title.Should().Be("Test Book");

        // 3) Update (and assert canonical route id is used)
        var updateReq = new UpdateBookRequest(
            Title: "Updated Title",
            Author: created.Author,
            Genre: created.Genre,
            PublishedDate: created.PublishedDate,
            Rating: 3);

        // Use different id in body by attempting to PUT to created.Id but body has no id; ensure returned dto id is canonical
        var putResp = await client.PutAsJsonAsync($"/api/books/{created.Id}", updateReq);
        putResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await putResp.Content.ReadFromJsonAsync<BookDto>(_json);
        updated.Should().NotBeNull();
        updated!.Id.Should().Be(created.Id);
        updated.Title.Should().Be("Updated Title");
        updated.Rating.Should().Be(3);

        // 4) List defaults: page 1, size 20, sort by PublishedDate desc by default
        var listResp = await client.GetAsync("/api/books");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await listResp.Content.ReadFromJsonAsync<PagedResult<BookDto>>(_json);
        list.Should().NotBeNull();
        list!.Page.Should().Be(1);
        list.PageSize.Should().Be(20);
        list.Items.Should().NotBeNull();
        list.Items.Count.Should().BeGreaterThan(0);
        var dates = list.Items.Select(b => b.PublishedDate).ToList();
        dates.Should().BeInDescendingOrder();

        // 5) Delete
        var delResp = await client.DeleteAsync($"/api/books/{created.Id}");
        delResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Ensure gone
        var getAfterDelete = await client.GetAsync($"/api/books/{created.Id}");
        getAfterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
