using System.Text.Json.Serialization;
using System.Threading.Channels;
using LiveAuction.Api.Realtime;
using LiveAuction.Application.Notifications;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace LiveAuction.IntegrationTests.Infrastructure;

// A real SignalR client talking WebSockets to the in-memory test server, the same way the browser
// connects (no negotiate request, token in the query string).
internal sealed class HubClient : IAsyncDisposable
{
    private static readonly TimeSpan MessageTimeout = TimeSpan.FromSeconds(10);

    private readonly HubConnection _connection;
    private readonly Channel<AuctionUpdate> _updates = Channel.CreateUnbounded<AuctionUpdate>();
    private readonly Channel<OutbidNotice> _outbidNotices = Channel.CreateUnbounded<OutbidNotice>();

    private HubClient(HubConnection connection)
    {
        _connection = connection;
        _connection.On<AuctionUpdate>(nameof(IAuctionClient.AuctionUpdated), update => _updates.Writer.TryWrite(update));
        _connection.On<OutbidNotice>(nameof(IAuctionClient.Outbid), notice => _outbidNotices.Writer.TryWrite(notice));
    }

    public static async Task<HubClient> ConnectAsync(AuctionApiFactory factory, AuctionClient user)
    {
        var server = factory.Server;
        var hubUri = new Uri(server.BaseAddress, $"{AuctionHub.Route}?access_token={user.AccessToken}");
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUri, options =>
            {
                options.Transports = HttpTransportType.WebSockets;
                options.SkipNegotiation = true;
                options.WebSocketFactory = async (context, cancellationToken) =>
                    await server.CreateWebSocketClient().ConnectAsync(context.Uri, cancellationToken);
            })
            .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .Build();

        var client = new HubClient(connection);
        await connection.StartAsync(TestContext.Current.CancellationToken);
        return client;
    }

    public Task<AuctionUpdate> WatchAsync(Guid auctionId) =>
        _connection.InvokeAsync<AuctionUpdate>(AuctionHub.WatchMethod, auctionId, TestContext.Current.CancellationToken);

    public async Task<AuctionUpdate> NextUpdateAsync()
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(MessageTimeout);
        return await _updates.Reader.ReadAsync(timeout.Token);
    }

    public async Task<OutbidNotice> NextOutbidNoticeAsync()
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(MessageTimeout);
        return await _outbidNotices.Reader.ReadAsync(timeout.Token);
    }

    public ValueTask DisposeAsync() => _connection.DisposeAsync();
}
