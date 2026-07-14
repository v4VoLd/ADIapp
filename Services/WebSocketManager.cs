using System;
using System.Threading.Tasks;
using PusherClient;

namespace ADIapp.Services;

/// <summary>
/// Manages the Pusher WebSocket connection and channel subscriptions.
/// Call InitializeAsync() once after a successful login.
/// Both Client and UserChannel are guaranteed non-null after initialization.
/// </summary>
public static class WebSocketManager
{
    private static Pusher? _client;
    private static Channel? _userChannel;

    /// <summary>Non-null after InitializeAsync() has been called.</summary>
    public static Pusher Client
        => _client ?? throw new InvalidOperationException("WebSocketManager is not initialized. Call InitializeAsync() after login.");

    /// <summary>Non-null after InitializeAsync() has been called.</summary>
    public static Channel UserChannel
        => _userChannel ?? throw new InvalidOperationException("WebSocketManager is not initialized. Call InitializeAsync() after login.");

    public static bool IsConnected
        => _client?.State == ConnectionState.Connected;

    public static async Task InitializeAsync(int userId)
    {
        // Prevent initializing twice
        if (_client != null && _client.State == ConnectionState.Connected)
            return;

        var authorizer = new HttpAuthorizer(AppConfig.BroadcastingAuthUrl);

        _client = new Pusher(AppConfig.PusherAppKey, new PusherOptions
        {
            Host      = AppConfig.WebSocketHost,
            Encrypted = false,
            Cluster   = null,
            Authorizer = authorizer
        });

        await _client.ConnectAsync();

        _userChannel = await _client.SubscribeAsync($"App.Models.User.{userId}");
    }
}
