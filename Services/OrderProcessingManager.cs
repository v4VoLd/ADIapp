using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using ADIapp.Models;
using ADIapp.Config;

namespace ADIapp.Services;

public static class OrderProcessingManager
{
    private static string? _activeOrderFileHash;
    private static int? _activeOrderId;
    private static bool _isProcessing;
    private static string _statusText = string.Empty;
    private static bool _isDownloading;
    private static System.Threading.CancellationTokenSource? _safetyPollCts;

    public static string? ActiveOrderFileHash => _activeOrderFileHash;
    public static int? ActiveOrderId => _activeOrderId;
    public static bool IsProcessing => _isProcessing;
    public static string StatusText => _statusText;

    public static event Action? StateChanged;

    public static void Initialize()
    {
        WebSocketManager.OrderUpdated -= OnWebSocketOrderUpdated;
        WebSocketManager.OrderUpdated += OnWebSocketOrderUpdated;
    }

    public static void StartTrackingOrder(string fileHash, int? orderId = null)
    {
        _activeOrderFileHash = fileHash;
        _activeOrderId = orderId;
        _isProcessing = true;
        _statusText = LanguageService.Get("Tune_WaitingTuning");
        StateChanged?.Invoke();

        // 1. Immediate check in case the backend completed synchronously
        _ = CheckAndHandleOrderCompletionAsync();

        // 2. Safety net: periodic background fallback check every 6s in case WebSocket drops
        StartSafetyFallbackPolling();
    }

    public static void StopTracking()
    {
        _safetyPollCts?.Cancel();
        _safetyPollCts = null;
        _activeOrderFileHash = null;
        _activeOrderId = null;
        _isProcessing = false;
        _statusText = string.Empty;
        StateChanged?.Invoke();
    }

    private static void StartSafetyFallbackPolling()
    {
        _safetyPollCts?.Cancel();
        _safetyPollCts = new System.Threading.CancellationTokenSource();
        var token = _safetyPollCts.Token;

        _ = Task.Run(async () =>
        {
            for (int i = 0; i < 15; i++) // Poll up to ~90 seconds
            {
                try
                {
                    await Task.Delay(6000, token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                if (token.IsCancellationRequested || !_isProcessing) return;

                bool handled = await CheckAndHandleOrderCompletionAsync();
                if (handled) return;
            }
        }, token);
    }

    private static void OnWebSocketOrderUpdated()
    {
        if ((!_activeOrderId.HasValue && string.IsNullOrEmpty(_activeOrderFileHash)) || !_isProcessing)
            return;

        // WebSocket event received: trigger instant status resolution (0ms delay, event-driven)
        Dispatcher.UIThread.Post(async () =>
        {
            await CheckAndHandleOrderCompletionAsync();
        });
    }

    private static async Task<bool> CheckAndHandleOrderCompletionAsync()
    {
        if (_isDownloading || (!_activeOrderId.HasValue && string.IsNullOrEmpty(_activeOrderFileHash))) 
            return false;

        try
        {
            var history = await ApiService.GetOrderHistoryAsync();
            if (history?.Orders != null && history.Orders.Count > 0)
            {
                // Priority 1: Match by exact Order ID. Priority 2: Fallback to fileHash
                var matchingOrder = history.Orders.FirstOrDefault(o =>
                    (_activeOrderId.HasValue && o.Id == _activeOrderId.Value) ||
                    (!string.IsNullOrEmpty(_activeOrderFileHash) && 
                        ((o.FileReceived?.Contains(_activeOrderFileHash, StringComparison.OrdinalIgnoreCase) == true) ||
                         (o.FileSent?.Contains(_activeOrderFileHash, StringComparison.OrdinalIgnoreCase) == true) ||
                         (o.DownloadUrl?.Contains(_activeOrderFileHash, StringComparison.OrdinalIgnoreCase) == true)))
                );

                if (matchingOrder != null)
                {
                    if (matchingOrder.IsCompleted)
                    {
                        StopTracking();
                        if (matchingOrder.IsDownloadExpired)
                        {
                            NotificationService.AddNotification($"order_expired_{matchingOrder.Id}", LanguageService.Get("Tune_LinkExpired"), "warning");
                            return true;
                        }

                        await PromptAndDownloadFileAsync(matchingOrder);
                        return true;
                    }
                    else if (matchingOrder.IsCanceled)
                    {
                        StopTracking();
                        NotificationService.AddNotification($"order_canceled_{matchingOrder.Id}", LanguageService.Get("Tune_OrderCanceled"), "error");
                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Helpers.Logger.Error($"Error checking order completion: {ex.Message}", ex);
        }

        return false;
    }

    public static async Task PromptAndDownloadFileAsync(OrderHistoryItemDto order)
    {
        if (_isDownloading) return;
        _isDownloading = true;

        try
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var lifetime = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
                var window = lifetime?.MainWindow;
                if (window == null) return;

                string downloadUrl = order.DownloadUrl ?? $"{AppConfig.BaseUrl}/order/download/{order.Id}";
                string fileName = GenerateSuggestedFileName(order);

                var saveFile = await window.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
                {
                    Title = LanguageService.Get("Tune_SavePickerTitle"),
                    SuggestedFileName = fileName,
                    DefaultExtension = "bin",
                    FileTypeChoices = new[]
                    {
                        new Avalonia.Platform.Storage.FilePickerFileType("Binary File (*.bin)")
                        {
                            Patterns = new[] { "*.bin" }
                        },
                        new Avalonia.Platform.Storage.FilePickerFileType("All Files (*.*)")
                        {
                            Patterns = new[] { "*.*" }
                        }
                    }
                });

                if (saveFile != null)
                {
                    using var stream = await saveFile.OpenWriteAsync();
                    var progress = new System.Progress<double>(p => { });

                    var (success, msg) = await ApiService.DownloadFileToStreamAsync(downloadUrl, stream, progress);
                    if (success)
                    {
                        NotificationService.AddNotification(
                            $"order_download_{order.Id}",
                            string.Format(LanguageService.Get("Tune_SavedSuccess"), fileName),
                            "info"
                        );
                        _ = ApiService.FetchProfileAsync();
                    }
                }
            });
        }
        catch (Exception ex)
        {
            Helpers.Logger.Error($"Error saving file: {ex.Message}", ex);
        }
        finally
        {
            _isDownloading = false;
        }
    }

    public static string GenerateSuggestedFileName(OrderHistoryItemDto order)
    {
        // 1. Determine order/file base name
        string? baseCandidate = !string.IsNullOrWhiteSpace(order.FileReceived)
            ? order.FileReceived
            : (!string.IsNullOrWhiteSpace(order.Title) ? order.Title : (!string.IsNullOrWhiteSpace(order.FileSent) ? order.FileSent : $"Order_{order.Id}"));

        string extension = ".bin";
        string baseName = "ADI-Preformance";

            // 2. Extract services done
        if (order.Services != null && order.Services.Count > 0)
        {
            var serviceNames = order.Services
                .Where(s => !string.IsNullOrWhiteSpace(s.Name))
                .Select(s =>
                {
                    var parts = s.Name.Split(new[] { ' ', '-', '/', '\\', '+', '•', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    return string.Join("_", parts);
                })
                .Where(s => !string.IsNullOrWhiteSpace(s));

            string joinedServices = string.Join("_", serviceNames);
            if (!string.IsNullOrWhiteSpace(joinedServices))
            {
                return $"{baseName}_{joinedServices}{extension}";
            }
        }

        return $"{baseName}{extension}";
    }
}


