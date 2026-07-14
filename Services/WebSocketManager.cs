using System.Threading.Tasks;
using PusherClient;

namespace ADIapp.Services;

/// <summary>
/// Manages the Pusher WebSocket connection and channel subscriptions.
/// Call InitializeAsync() once after a successful login.
/// </summary>
public static class WebSocketManager
{
    public static Pusher Client { get; private set; }
    public static Channel UserChannel { get; private set; }

    public static async Task InitializeAsync(int userId)
    {
        // Prevent initializing twice
        if (Client != null && Client.State == ConnectionState.Connected)
            return;

        var authorizer = new HttpAuthorizer(AppConfig.BroadcastingAuthUrl);

        Client = new Pusher(AppConfig.PusherAppKey, new PusherOptions
        {
            Host = AppConfig.WebSocketHost,
            Encrypted = false,
            Cluster = null,
            Authorizer = authorizer
        });

        await Client.ConnectAsync();

        UserChannel = await Client.SubscribeAsync($"App.Models.User.{userId}");
    }
}
