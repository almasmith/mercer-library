using System;
using System.Collections.Generic;
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

public sealed class StatsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public StatsTests(CustomWebApplicationFactory factory)
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
    public async Task Stats_Normalizes_And_Sorts_Per_Spec()
    {
        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var (email, password) = NewUserCreds();

        // Register and get token
        var registerResp = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password));
        registerResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var registerBody = await registerResp.Content.ReadFromJsonAsync<AuthResponse>(_json);
        registerBody.Should().NotBeNull();
        ApplyBearer(client, registerBody!.AccessToken);

        // Create books with genre variations (scifi x3, fantasy x2, mystery x2, software x1)
        var now = DateTimeOffset.UtcNow;
        var creates = new List<CreateBookRequest>
        {
            new("S1", "A", "SciFi", now, 5),
            new("S2", "A", " scifi ", now, 4),
            new("S3", "A", "SCIFI", now, 3),

            new("F1", "A", "FANTASY", now, 4),
            new("F2", "A", "fantasy", now, 5),

            new("M1", "A", "Mystery", now, 4),
            new("M2", "A", " mystery  ", now, 4),

            new("SW", "A", "Software", now, 5)
        };

        foreach (var req in creates)
        {
            var createResp = await client.PostAsJsonAsync("/api/books", req);
            createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        // Fetch stats
        var statsResp = await client.GetAsync("/api/books/stats");
        statsResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var stats = await statsResp.Content.ReadFromJsonAsync<List<BookGenreCountDto>>(_json);
        stats.Should().NotBeNull();

        // Normalize for comparison (trim + lower)
        var simplified = stats!
            .Select(x => (Genre: x.Genre.Trim().ToLowerInvariant(), x.Count))
            .ToList();

        // Expect order: scifi (3), fantasy (2), mystery (2), software (1)
        simplified.Should().Equal(new List<(string Genre, int Count)>
        {
            ("scifi", 3),
            ("fantasy", 2),
            ("mystery", 2),
            ("software", 1)
        });

        // No empty/whitespace genres present
        simplified.Any(x => string.IsNullOrWhiteSpace(x.Genre)).Should().BeFalse();
    }

    [Fact]
    public async Task Stats_Are_Scoped_To_User()
    {
        // User A
        using var clientA = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var (emailA, passwordA) = NewUserCreds();
        var regRespA = await clientA.PostAsJsonAsync("/api/auth/register", new RegisterRequest(emailA, passwordA));
        regRespA.StatusCode.Should().Be(HttpStatusCode.OK);
        var bodyA = await regRespA.Content.ReadFromJsonAsync<AuthResponse>(_json);
        ApplyBearer(clientA, bodyA!.AccessToken);

        // User B
        using var clientB = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var (emailB, passwordB) = NewUserCreds();
        var regRespB = await clientB.PostAsJsonAsync("/api/auth/register", new RegisterRequest(emailB, passwordB));
        regRespB.StatusCode.Should().Be(HttpStatusCode.OK);
        var bodyB = await regRespB.Content.ReadFromJsonAsync<AuthResponse>(_json);
        ApplyBearer(clientB, bodyB!.AccessToken);

        var now = DateTimeOffset.UtcNow;

        // Create books for A: two scifi, one fantasy
        foreach (var req in new[]
        {
            new CreateBookRequest("A1", "A", "SciFi", now, 5),
            new CreateBookRequest("A2", "A", " scifi ", now, 4),
            new CreateBookRequest("A3", "A", "fantasy", now, 4),
        })
        {
            var resp = await clientA.PostAsJsonAsync("/api/books", req);
            resp.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        // Create books for B: one horror only
        var respB1 = await clientB.PostAsJsonAsync("/api/books", new CreateBookRequest("B1", "B", "Horror", now, 3));
        respB1.StatusCode.Should().Be(HttpStatusCode.Created);

        // Stats for A
        var statsRespA = await clientA.GetAsync("/api/books/stats");
        statsRespA.StatusCode.Should().Be(HttpStatusCode.OK);
        var statsA = await statsRespA.Content.ReadFromJsonAsync<List<BookGenreCountDto>>(_json);
        statsA.Should().NotBeNull();
        var simplifiedA = statsA!.Select(x => (x.Genre.Trim().ToLowerInvariant(), x.Count)).ToList();
        simplifiedA.Should().Contain(("scifi", 2));
        simplifiedA.Should().Contain(("fantasy", 1));
        simplifiedA.Any(x => x.Item1 == "horror").Should().BeFalse();

        // Stats for B
        var statsRespB = await clientB.GetAsync("/api/books/stats");
        statsRespB.StatusCode.Should().Be(HttpStatusCode.OK);
        var statsB = await statsRespB.Content.ReadFromJsonAsync<List<BookGenreCountDto>>(_json);
        statsB.Should().NotBeNull();
        var simplifiedB = statsB!.Select(x => (x.Genre.Trim().ToLowerInvariant(), x.Count)).ToList();
        simplifiedB.Should().Equal(new List<(string, int)> { ("horror", 1) });
    }
}
