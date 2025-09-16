using System;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Library.Api.Dtos;
using Library.Api.Dtos.Auth;
using Library.Tests.Infrastructure;

namespace Library.Tests.Integration;

public sealed class AuthorizationBoundaryTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public AuthorizationBoundaryTests(CustomWebApplicationFactory factory)
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
    public async Task User_A_Cannot_Access_User_Bs_Book_By_Id_Update_Delete_Favorite_Unfavorite_Read()
    {
        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Create two distinct users A and B
        var (aEmail, aPassword) = NewUserCreds();
        var (bEmail, bPassword) = NewUserCreds();

        var registerA = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(aEmail, aPassword));
        registerA.StatusCode.Should().Be(HttpStatusCode.OK);
        var authA = await registerA.Content.ReadFromJsonAsync<AuthResponse>(_json);
        authA.Should().NotBeNull();

        var registerB = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(bEmail, bPassword));
        registerB.StatusCode.Should().Be(HttpStatusCode.OK);
        var authB = await registerB.Content.ReadFromJsonAsync<AuthResponse>(_json);
        authB.Should().NotBeNull();

        // User B creates a book
        ApplyBearer(client, authB!.AccessToken);
        var createReq = new CreateBookRequest("B's Book", "B Author", "Mystery", new DateTimeOffset(2021, 5, 20, 0, 0, 0, TimeSpan.Zero), 4);
        var createResp = await client.PostAsJsonAsync("/api/books", createReq);
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var bBook = await createResp.Content.ReadFromJsonAsync<BookDto>(_json);
        bBook.Should().NotBeNull();

        // Switch to User A and attempt to access User B's book across endpoints
        ApplyBearer(client, authA!.AccessToken);

        // GET
        var getResp = await client.GetAsync($"/api/books/{bBook!.Id}");
        new[] { HttpStatusCode.NotFound, HttpStatusCode.Forbidden }.Should().Contain(getResp.StatusCode);

        // PUT
        var updateReq = new UpdateBookRequest("Hacked Title", bBook.Author, bBook.Genre, bBook.PublishedDate, 1);
        var putResp = await client.PutAsJsonAsync($"/api/books/{bBook.Id}", updateReq);
        new[] { HttpStatusCode.NotFound, HttpStatusCode.Forbidden }.Should().Contain(putResp.StatusCode);

        // DELETE
        var delResp = await client.DeleteAsync($"/api/books/{bBook.Id}");
        new[] { HttpStatusCode.NotFound, HttpStatusCode.Forbidden }.Should().Contain(delResp.StatusCode);

        // POST favorite
        var favResp = await client.PostAsync($"/api/books/{bBook.Id}/favorite", null);
        new[] { HttpStatusCode.NotFound, HttpStatusCode.Forbidden }.Should().Contain(favResp.StatusCode);

        // DELETE favorite (unfavorite)
        var unfavResp = await client.DeleteAsync($"/api/books/{bBook.Id}/favorite");
        new[] { HttpStatusCode.NotFound, HttpStatusCode.Forbidden }.Should().Contain(unfavResp.StatusCode);

        // POST read
        var readResp = await client.PostAsync($"/api/books/{bBook.Id}/read", null);
        new[] { HttpStatusCode.NotFound, HttpStatusCode.Forbidden }.Should().Contain(readResp.StatusCode);
    }
}


