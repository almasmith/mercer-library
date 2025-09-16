using System;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Library.Api.Dtos;
using Library.Api.Dtos.Auth;
using Library.Api.Hubs;
using Library.Tests.Infrastructure;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace Library.Tests.Integration;

public sealed class RealtimeSmokeTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public RealtimeSmokeTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static (string Email, string Password) NewUserCreds()
    {
        return ($"user_{Guid.NewGuid():N}@example.com", "Passw0rd!");
    }

    [Fact]
    public async Task BookCreated_event_is_received()
    {
        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var (email, password) = NewUserCreds();

        var registerResp = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password));
        registerResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var registerBody = await registerResp.Content.ReadFromJsonAsync<AuthResponse>(_json);
        registerBody.Should().NotBeNull();
        var token = registerBody!.AccessToken;

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var baseAddress = client.BaseAddress!.ToString().TrimEnd('/');
        var tcs = new TaskCompletionSource<BookDto>(TaskCreationOptions.RunContinuationsAsynchronously);

        var connection = new HubConnectionBuilder()
            .WithUrl($"{baseAddress}/hubs/library?access_token={token}", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            })
            .WithAutomaticReconnect()
            .Build();

        try
        {
            connection.On<BookDto>(LibraryEvents.BookCreated, payload => tcs.TrySetResult(payload));

            await connection.StartAsync();

            var createReq = new CreateBookRequest(
                Title: "Realtime Test",
                Author: "Agent",
                Genre: "Test",
                PublishedDate: new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.Zero),
                Rating: 4);

            var createResp = await client.PostAsJsonAsync("/api/books", createReq);
            createResp.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await createResp.Content.ReadFromJsonAsync<BookDto>(_json);
            created.Should().NotBeNull();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var evt = await tcs.Task.WaitAsync(cts.Token);

            evt.Should().NotBeNull();
            evt.Id.Should().Be(created!.Id);
            evt.Title.Should().Be("Realtime Test");
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }
}


