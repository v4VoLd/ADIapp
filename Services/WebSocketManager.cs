using System;
using System.Threading.Tasks;
using PusherClient;
using ADIapp.Models;
using ADIapp.Config;
using ADIapp.Helpers;

namespace ADIapp.Services;

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

    public static event Action<string, EcuIdentifyData>? EcuIdentified;

    public static async Task InitializeAsync(int userId)
    {
        // Prevent initializing twice
        if (_client != null && _client.State == ConnectionState.Connected)
            return;

        var authorizer = new HttpAuthorizer(AppConfig.BroadcastingAuthUrl)
        {
            AuthenticationHeader = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ApiService.AccessToken)
        };

        _client = new Pusher(AppConfig.PusherAppKey, new PusherOptions
        {
            Host      = AppConfig.WebSocketHost,
            Encrypted = false,
            Cluster   = null,
            Authorizer = authorizer
        });

        try
        {
            await _client.ConnectAsync();

            _userChannel = await _client.SubscribeAsync($"private-App.Models.User.{userId}");
            _userChannel.Bind("Illuminate\\Notifications\\Events\\BroadcastNotificationCreated", OnNotificationReceived);
            _userChannel.Bind("EcuIdentified", OnEcuIdentifiedEvent);
        }
        catch (Exception ex)
        {
            Logger.Error($"[WebSocket] Exception during initialization: {ex.Message}", ex);
            throw;
        }
    }

    private static void OnNotificationReceived(PusherEvent eventData)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(eventData.Data);
            var root = doc.RootElement;

            string id = "";
            if (root.TryGetProperty("id", out var idProp))
            {
                id = idProp.GetString() ?? "";
            }
            if (string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString();
            }

            string type = "";
            if (root.TryGetProperty("type", out var typeProp))
            {
                type = typeProp.GetString() ?? "";
            }

            string message = "";
            if (root.TryGetProperty("data", out var dataProp))
            {
                if (dataProp.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    if (dataProp.TryGetProperty("message", out var msgProp))
                    {
                        message = msgProp.GetString() ?? "";
                    }
                    else if (dataProp.TryGetProperty("content", out var contentProp))
                    {
                        message = contentProp.GetString() ?? "";
                    }
                    else if (dataProp.TryGetProperty("text", out var textProp))
                    {
                        message = textProp.GetString() ?? "";
                    }
                    else
                    {
                        message = dataProp.GetRawText();
                    }
                }
                else if (dataProp.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    message = dataProp.GetString() ?? "";
                }
            }

            if (string.IsNullOrEmpty(message))
            {
                if (root.TryGetProperty("message", out var rootMsg))
                {
                    message = rootMsg.GetString() ?? "";
                }
                else
                {
                    message = "New notification received.";
                }
            }

            NotificationService.AddNotification(id, message, type);
        }
        catch (Exception ex)
        {
            Logger.Error($"Error parsing notification payload: {ex.Message}", ex);
            NotificationService.AddNotification(Guid.NewGuid().ToString(), "New notification received.", "Info");
        }
    }

    private static void OnEcuIdentifiedEvent(PusherEvent eventData)
    {
        try
        {
            Logger.Info($"[WebSocket] Received EcuIdentified event: {eventData.Data}");
            using var doc = System.Text.Json.JsonDocument.Parse(eventData.Data);
            var root = doc.RootElement;

            string fileHash = "";
            if (root.TryGetProperty("hash", out var hashProp))
            {
                fileHash = hashProp.GetString() ?? "";
            }

            if (root.TryGetProperty("info", out var infoProp))
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var ecuData = System.Text.Json.JsonSerializer.Deserialize<EcuIdentifyData>(infoProp.GetRawText(), options);
                if (ecuData != null)
                {
                    ecuData.FileHash = fileHash;
                    EcuIdentified?.Invoke(fileHash, ecuData);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"[WebSocket] Error parsing EcuIdentified event: {ex.Message}", ex);
        }
    }
}
