using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Library.Api.Dtos;
using Library.Api.Dtos.Analytics;
using Library.Api.Dtos.Auth;
using Library.Tests.Infrastructure;

namespace Library.Tests.Integration;

public sealed class ReadsAndAnalyticsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public ReadsAndAnalyticsTests(CustomWebApplicationFactory factory)
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
    public async Task Post_Read_Then_Analytics_Endpoints_Reflect_Data_And_FromTo_Filtering()
    {
        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var (email, password) = NewUserCreds();

        // Register and login
        var registerResp = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password));
        registerResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var registerBody = await registerResp.Content.ReadFromJsonAsync<AuthResponse>(_json);
        registerBody.Should().NotBeNull();
        ApplyBearer(client, registerBody!.AccessToken);

        // Create two books with different ratings and genres
        var b1Req = new CreateBookRequest("Readable One", "A. Author", "SciFi", new DateTimeOffset(2021, 6, 1, 0, 0, 0, TimeSpan.Zero), 5);
        var b2Req = new CreateBookRequest("Readable Two", "B. Author", "Fantasy", new DateTimeOffset(2022, 1, 15, 0, 0, 0, TimeSpan.Zero), 3);

        var b1Resp = await client.PostAsJsonAsync("/api/books", b1Req);
        b1Resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var b1 = await b1Resp.Content.ReadFromJsonAsync<BookDto>(_json);
        b1.Should().NotBeNull();

        var b2Resp = await client.PostAsJsonAsync("/api/books", b2Req);
        b2Resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var b2 = await b2Resp.Content.ReadFromJsonAsync<BookDto>(_json);
        b2.Should().NotBeNull();

        // Record reads (204)
        var read1 = await client.PostAsync($"/api/books/{b1!.Id}/read", null);
        read1.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var read2 = await client.PostAsync($"/api/books/{b2!.Id}/read", null);
        read2.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Avg rating by month should reflect both reads in the current month bucket (no from/to)
        var now = DateTimeOffset.UtcNow;
        var avgResp = await client.GetAsync($"/api/analytics/avg-rating?bucket=month");
        if (avgResp.StatusCode != HttpStatusCode.OK)
        {
            var body = await avgResp.Content.ReadAsStringAsync();
            throw new Xunit.Sdk.XunitException($"avg-rating unexpected status {(int)avgResp.StatusCode}: {avgResp.StatusCode}. Body: {body}");
        }
        var avg = await avgResp.Content.ReadFromJsonAsync<AvgRatingBucketDto[]>(_json);
        avg.Should().NotBeNull();
        avg!.Should().NotBeEmpty();
        var thisMonthBucket = $"{now.UtcDateTime.Year:D4}-{now.UtcDateTime.Month:D2}";
        avg!.Any(b => b.Bucket == thisMonthBucket).Should().BeTrue();
        var bucketValue = avg!.Single(b => b.Bucket == thisMonthBucket).Average;
        bucketValue.Should().BeApproximately((b1Req.Rating + b2Req.Rating) / 2.0, 0.001);

        // Most read genres should include SciFi and Fantasy (no from/to)
        var genresResp = await client.GetAsync($"/api/analytics/most-read-genres");
        if (genresResp.StatusCode != HttpStatusCode.OK)
        {
            var body = await genresResp.Content.ReadAsStringAsync();
            throw new Xunit.Sdk.XunitException($"most-read-genres unexpected status {(int)genresResp.StatusCode}: {genresResp.StatusCode}. Body: {body}");
        }
        var genres = await genresResp.Content.ReadFromJsonAsync<MostReadGenreDto[]>(_json);
        genres.Should().NotBeNull();
        genres!.Any(g => g.Genre.Equals("SciFi", StringComparison.OrdinalIgnoreCase) && g.ReadCount >= 1).Should().BeTrue();
        genres!.Any(g => g.Genre.Equals("Fantasy", StringComparison.OrdinalIgnoreCase) && g.ReadCount >= 1).Should().BeTrue();

        // From filtering: pick a future 'from' that should exclude current reads
        var futureFrom = now.AddMonths(1).UtcDateTime;
        var futureFromIso = futureFrom.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'");
        var avgFutureResp = await client.GetAsync($"/api/analytics/avg-rating?bucket=month&from={Uri.EscapeDataString(futureFromIso)}");
        if (avgFutureResp.StatusCode != HttpStatusCode.OK)
        {
            var body = await avgFutureResp.Content.ReadAsStringAsync();
            throw new Xunit.Sdk.XunitException($"avg-rating future-from unexpected status {(int)avgFutureResp.StatusCode}: {avgFutureResp.StatusCode}. Body: {body}");
        }
        var avgFuture = await avgFutureResp.Content.ReadFromJsonAsync<AvgRatingBucketDto[]>(_json);
        (avgFuture == null || avgFuture.Length == 0).Should().BeTrue();

        var genresFutureResp = await client.GetAsync($"/api/analytics/most-read-genres?from={Uri.EscapeDataString(futureFromIso)}");
        if (genresFutureResp.StatusCode != HttpStatusCode.OK)
        {
            var body = await genresFutureResp.Content.ReadAsStringAsync();
            throw new Xunit.Sdk.XunitException($"most-read-genres future-from unexpected status {(int)genresFutureResp.StatusCode}: {genresFutureResp.StatusCode}. Body: {body}");
        }
        var genresFuture = await genresFutureResp.Content.ReadFromJsonAsync<MostReadGenreDto[]>(_json);
        (genresFuture == null || genresFuture.Length == 0).Should().BeTrue();
    }
}
