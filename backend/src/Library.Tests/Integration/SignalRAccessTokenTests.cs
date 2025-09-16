using System;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Library.Api.Dtos.Auth;
using Library.Tests.Infrastructure;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace Library.Tests.Integration;

public sealed class SignalRAccessTokenTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public SignalRAccessTokenTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static (string Email, string Password) NewUserCreds()
    {
        return ($"user_{Guid.NewGuid():N}@example.com", "Passw0rd!");
    }

    [Fact]
    public async Task Handshake_With_Valid_Access_Token_Succeeds()
    {
        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var (email, password) = NewUserCreds();

        var registerResp = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password));
        registerResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var registerBody = await registerResp.Content.ReadFromJsonAsync<AuthResponse>(_json);
        registerBody.Should().NotBeNull();
        var token = registerBody!.AccessToken;

        var baseAddress = client.BaseAddress!.ToString().TrimEnd('/');

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
            await connection.StartAsync();

            connection.State.Should().Be(HubConnectionState.Connected);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task Handshake_With_Invalid_Or_Missing_Token_Fails()
    {
        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var baseAddress = client.BaseAddress!.ToString().TrimEnd('/');

        // Invalid token
        var invalidConnection = new HubConnectionBuilder()
            .WithUrl($"{baseAddress}/hubs/library?access_token=invalid", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            })
            .Build();

        Func<Task> invalidAct = () => invalidConnection.StartAsync();
        await invalidAct.Should().ThrowAsync<Exception>();

        await invalidConnection.DisposeAsync();

        // Missing token
        var missingTokenConnection = new HubConnectionBuilder()
            .WithUrl($"{baseAddress}/hubs/library", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            })
            .Build();

        Func<Task> missingAct = () => missingTokenConnection.StartAsync();
        await missingAct.Should().ThrowAsync<Exception>();

        await missingTokenConnection.DisposeAsync();
    }
}


