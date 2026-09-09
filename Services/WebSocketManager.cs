using System;
using System.Threading;
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
    public static event Action? TicketUpdated;
    public static event Action? OrderUpdated;

    private static int _currentUserId;
    private static bool _isConnecting;
    private static CancellationTokenSource? _reconnectCts;

    public static async Task InitializeAsync(int userId)
    {
        _currentUserId = userId;

        // Cancel previous reconnect task if any
        _reconnectCts?.Cancel();
        _reconnectCts = new CancellationTokenSource();

        await StartConnectionLoopAsync(_reconnectCts.Token);
    }

    public static async Task DisconnectAsync()
    {
        _reconnectCts?.Cancel();
        _reconnectCts = null;

        if (_client != null)
        {
            try
            {
                await _client.DisconnectAsync();
            }
            catch { }
        }
    }

    private static async Task StartConnectionLoopAsync(CancellationToken token)
    {
        if (_isConnecting) return;
        _isConnecting = true;

        try
        {
            int retryDelaySeconds = 3;
            const int maxRetryDelaySeconds = 15;

            while (!token.IsCancellationRequested)
            {
                if (_client != null && _client.State == ConnectionState.Connected)
                {
                    break;
                }

                Logger.Info("[WebSocket] Attempting to connect...");

                try
                {
                    var authorizer = new HttpAuthorizer(AppConfig.BroadcastingAuthUrl)
                    {
                        AuthenticationHeader = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ApiService.AccessToken)
                    };

                    var host = AppConfig.WebSocketHost;

                    if (_client != null)
                    {
                        try { await _client.DisconnectAsync(); } catch { }
                    }

                    _client = new Pusher(AppConfig.PusherAppKey, new PusherOptions
                    {
                        Host       = host,
                        Encrypted  = AppConfig.WebsocketEncrypted,
                        Cluster    = null,
                        Authorizer = authorizer
                    });

                    _client.ConnectionStateChanged += (sender, state) =>
                    {
                        Logger.Info($"[WebSocket] Connection state changed to: {state}");
                        if (state == ConnectionState.Disconnected && _reconnectCts != null && !_reconnectCts.IsCancellationRequested)
                        {
                            Logger.Warn("[WebSocket] Disconnected from server. Scheduling reconnect...");
                            _ = Task.Run(() => StartConnectionLoopAsync(_reconnectCts.Token));
                        }
                    };

                    _client.Error += (sender, error) =>
                    {
                        Logger.Warn($"[WebSocket] Error event received: {error.Message}");
                    };

                    await _client.ConnectAsync();

                    _userChannel = await _client.SubscribeAsync($"private-App.Models.User.{_currentUserId}");
                    _userChannel.Bind("Illuminate\\Notifications\\Events\\BroadcastNotificationCreated", OnNotificationReceived);
                    _userChannel.Bind("EcuIdentified", OnEcuIdentifiedEvent);
                    _userChannel.Bind("TicketUpdated", OnTicketUpdatedEvent);
                    _userChannel.Bind("OrderUpdated", OnOrderUpdatedEvent);
                    _userChannel.Bind("OrderStatusUpdated", OnOrderUpdatedEvent);
                    _userChannel.Bind("UserProfileUpdated", OnUserProfileUpdatedEvent);

                    Logger.Info($"[WebSocket] Successfully connected and subscribed to user {_currentUserId} channel.");
                    OrderUpdated?.Invoke();
                    TicketUpdated?.Invoke();
                    break; // Connected successfully
                }
                catch (Exception ex)
                {
                    Logger.Warn($"[WebSocket] Connection attempt failed: {ex.Message}. Retrying in {retryDelaySeconds}s...");
                    await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds), token);
                    retryDelaySeconds = Math.Min(retryDelaySeconds * 2, maxRetryDelaySeconds);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on logout / intentional disconnect
        }
        catch (Exception ex)
        {
            Logger.Error($"[WebSocket] Fatal error in connection loop: {ex.Message}", ex);
        }
        finally
        {
            _isConnecting = false;
        }
    }

    private static void OnTicketUpdatedEvent(PusherEvent eventData)
    {
        try
        {
            Logger.Info($"[WebSocket] Received TicketUpdated event: {eventData.Data}");
            TicketUpdated?.Invoke();
        }
        catch (Exception ex)
        {
            Logger.Error($"[WebSocket] Error handling TicketUpdated event: {ex.Message}", ex);
        }
    }

    private static void OnOrderUpdatedEvent(PusherEvent eventData)
    {
        try
        {
            Logger.Info($"[WebSocket] Received OrderUpdated event: {eventData.Data}");
            OrderUpdated?.Invoke();
            _ = ApiService.FetchProfileAsync();
        }
        catch (Exception ex)
        {
            Logger.Error($"[WebSocket] Error handling OrderUpdated event: {ex.Message}", ex);
        }
    }

    private static void OnUserProfileUpdatedEvent(PusherEvent eventData)
    {
        try
        {
            Logger.Info($"[WebSocket] Received UserProfileUpdated event: {eventData.Data}");
            _ = ApiService.FetchProfileAsync();
        }
        catch (Exception ex)
        {
            Logger.Error($"[WebSocket] Error handling UserProfileUpdated event: {ex.Message}", ex);
        }
    }

    private static void OnNotificationReceived(PusherEvent eventData)
    {
        try
        {
            OrderUpdated?.Invoke();
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
            int? ticketId = null;
            string? ticketNumber = null;

            if (root.TryGetProperty("ticket_id", out var tIdProp) && tIdProp.TryGetInt32(out var tIdVal))
            {
                ticketId = tIdVal;
            }
            if (root.TryGetProperty("ticket_number", out var tNumProp))
            {
                ticketNumber = tNumProp.GetString();
            }

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

                    if (!ticketId.HasValue && dataProp.TryGetProperty("ticket_id", out var dTicketId) && dTicketId.TryGetInt32(out var dtVal))
                    {
                        ticketId = dtVal;
                    }
                    if (string.IsNullOrEmpty(ticketNumber) && dataProp.TryGetProperty("ticket_number", out var dTicketNum))
                    {
                        ticketNumber = dTicketNum.GetString();
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
                    message = LanguageService.Get("Tune_NewNotification");
                }
            }

            NotificationService.AddNotification(id, message, type, ticketId, ticketNumber);
        }
        catch (Exception ex)
        {
            Logger.Error($"Error parsing notification payload: {ex.Message}", ex);
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
